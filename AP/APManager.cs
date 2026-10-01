using System;
using System.Collections.Generic;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using WoLArchipelago.Services;

namespace WoLArchipelago
{
    /// <summary>
    /// Manages Archipelago connection lifecycle, item queueing, location checks, and offline synchronization.
    /// </summary>
    public class APManager
    {
        private readonly object _lockObject = new();
        private readonly StorageService _storageService = new();
        private readonly HashSet<long> _checkedLocations = [];
        private readonly Queue<long> _itemsToProcess = new();

        private Queue<long> _offlineCheckQueue = new();
        private int _itemsReceivedIndex;
        private bool _pendingGoalCompletion;

        // Saved parameters for auto-reconnection
        private string _lastHost;
        private int _lastPort;
        private string _lastSlot;
        private string _lastPassword;

        // Session & Connection State
        public ArchipelagoSession Session { get; private set; }
        public Dictionary<long, ScoutedItemInfo> ScoutedLocations { get; private set; }
        public bool IsConnected { get; private set; }
        public string StatusMessage { get; private set; }

        // Game Settings from Slot Data
        public static int StartingArcanaMode { get; private set; }
        public static int ElementLicensesMode { get; private set; }
        public static int ChaosFragmentsRequired { get; private set; }
        public static string StartingElement { get; private set; }

        public static string CurrentAPSavePrefix
        {
            get { return StorageService.CurrentAPSavePrefix; }
        }

        public APManager()
        {
            ScoutedLocations = [];
            StatusMessage = "Disconnected";

            StorageService.InitProfile();
            ItemHandler.CachedItems.Clear();

            _offlineCheckQueue = _storageService.LoadPendingChecks(_checkedLocations);
            _itemsReceivedIndex = _storageService.LoadItemIndex();
            _pendingGoalCompletion = DataManager.IsGoalCompleted;
        }

        public void Connect(string host, int port, string slotName, string password = null, bool isReconnecting = false)
        {
            _lastHost = host;
            _lastPort = port;
            _lastSlot = slotName;
            _lastPassword = password;

            StatusMessage = "Connecting...";

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    DisconnectInternal();

                    string connectUri = PrepareConnectUri(host, port);
                    Session = ArchipelagoSessionFactory.CreateSession(connectUri);

                    LoginResult result = Session.TryConnectAndLogin(
                        "Wizard of Legend",
                        slotName,
                        ItemsHandlingFlags.AllItems,
                        password: password
                    );


                    if (result is LoginSuccessful loginSuccess)
                    {
                        HandleSuccessfulLogin(loginSuccess, slotName, isReconnecting);
                    }
                    else if (result is LoginFailure failure)
                    {
                        HandleLoginFailure(failure);
                    }
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    StatusMessage = string.Format("Error: {0}", ex.Message);
                    Plugin.Log.LogError(string.Format("[AP] Connection error: {0}", ex));
                }
            });
        }

        private string PrepareConnectUri(string host, int port)
        {
            string scheme = host.Contains("archipelago.gg") ? "wss" : "ws";
            return string.Format("{0}://{1}:{2}", scheme, host, port);
        }

        private void HandleSuccessfulLogin(LoginSuccessful loginSuccess, string slotName, bool isReconnecting)
        {
            IsConnected = true;
            StatusMessage = "Online";

            _storageService.SetAPSaveProfile(Session.RoomState.Seed, slotName);
            ParseSlotData(loginSuccess.SlotData);

            Plugin.ExecuteOnMainThread(delegate
            {
                GameDataManager.Load();
                if (ArchipelagoUI.Instance != null)
                {
                    ArchipelagoUI.Instance.ToggleHasConnectedOnce();
                }
            });

            lock (_lockObject)
            {
                _checkedLocations.Clear();
                _offlineCheckQueue.Clear();
                _itemsToProcess.Clear();
                ItemHandler.CachedItems.Clear();

                _itemsReceivedIndex = _storageService.LoadItemIndex();
                _offlineCheckQueue = _storageService.LoadPendingChecks(_checkedLocations);

                ItemHandler.CachedItems.AddRange(Session.Items.AllItemsReceived);

                // Enqueue items received while offline or unprocessed
                while (_itemsReceivedIndex < Session.Items.AllItemsReceived.Count)
                {
                    ItemInfo item = Session.Items.AllItemsReceived[_itemsReceivedIndex];
                    _itemsToProcess.Enqueue(item.ItemId);
                    _itemsReceivedIndex++;
                    _storageService.SaveItemIndex(_itemsReceivedIndex);
                }

                DataManager.LoadData();
            }

            // Fetch scouted information for all locations in the room
            Session.Locations.ScoutLocationsAsync(delegate(Dictionary<long, ScoutedItemInfo> scoutedInfo)
            {
                lock (_lockObject)
                {
                    ScoutedLocations = scoutedInfo;
                }
            }, new List<long>(Session.Locations.AllLocations).ToArray());

            Session.Items.ItemReceived += OnItemReceived;
            Session.Socket.SocketClosed += OnSocketClosed;

            foreach (long locationId in Session.Locations.AllLocationsChecked)
            {
                _checkedLocations.Add(locationId);
            }

            if (!isReconnecting)
            {
                Plugin.StartAPRun();
            }

            FlushOfflineQueue();
        }

        private void HandleLoginFailure(LoginFailure failure)
        {
            IsConnected = false;
            StatusMessage = string.Format("Failed: {0}", string.Join(", ", failure.Errors));
            Plugin.Log.LogError(string.Format("[AP] {0}", StatusMessage));
        }

        /// <summary>
        /// Retrieves player alias and item display name for a given scouted location, 
        /// used in-game for shop slots to let player prioritize which location to unlock first
        /// </summary>
        public Tuple<string, string> GetLocationInfo(long locationId)
        {
            lock (_lockObject)
            {
                ScoutedItemInfo itemInfo;
                if (ScoutedLocations != null && ScoutedLocations.TryGetValue(locationId, out itemInfo))
                {
                    string playerName = "Unknown Player";
                    if (itemInfo.Player != null)
                    {
                        playerName = itemInfo.Player.Alias ?? itemInfo.Player.Name ?? "Unknown Player";
                    }

                    string itemName = !string.IsNullOrEmpty(itemInfo.ItemDisplayName) ? itemInfo.ItemDisplayName : itemInfo.ItemName;

                    return new Tuple<string, string>(playerName, itemName);
                }
            }

            return new Tuple<string, string>("Unknown Player", "Emplacement Archipelago");
        }

        public void Disconnect()
        {
            DisconnectInternal();
            StatusMessage = "Disconnected (Offline Mode)";
        }

        private void DisconnectInternal()
        {
            if (Session != null)
            {
                Session.Items.ItemReceived -= OnItemReceived;
                if (Session.Socket != null)
                {
                    Session.Socket.SocketClosed -= OnSocketClosed;
                }
                Session = null;
            }

            IsConnected = false;
            StatusMessage = "Offline";
            Plugin.Log.LogWarning("[AP] Offline mode active.");
        }

        private void OnSocketClosed(string reason)
        {
            IsConnected = false;
            StatusMessage = "Connection Lost!";
            Plugin.Log.LogWarning(string.Format("[AP] Connection lost: {0}. Attempting reconnect...", reason));

            Plugin.ExecuteOnMainThread(delegate
            {
                Plugin.Instance.StartCoroutine(AutoReconnectCoroutine());
            });
        }

        private System.Collections.IEnumerator AutoReconnectCoroutine()
        {
            yield return new UnityEngine.WaitForSeconds(3f);

            if (!IsConnected)
            {
                Connect(_lastHost, _lastPort, _lastSlot, _lastPassword, isReconnecting: true);
            }
        }

        public void CompleteGoal()
        {
            lock (_lockObject)
            {
                _pendingGoalCompletion = true;
                DataManager.IsGoalCompleted = true;
                DataManager.SaveData();

                if (IsConnected && Session != null)
                {
                    Plugin.Log.LogInfo("[AP] Goal completed!");
                    Session.SetGoalAchieved();
                }
                else
                {
                    Plugin.Log.LogWarning("[AP] Goal completed offline. Saved for next connection.");
                }
            }
        }

        public HashSet<long> GetCheckedLocation()
        {
            return _checkedLocations;
        }

        /// <summary>
        /// Records a location check locally and sends it to Archipelago, or queues it if offline.
        /// </summary>
        public void SendLocationCheck(long locationId)
        {
            lock (_lockObject)
            {
                if (!_checkedLocations.Add(locationId)) return;

                if (IsConnected && Session != null)
                {
                    Plugin.Log.LogInfo(string.Format("[AP] Sending check: {0}", locationId));
                    Session.Locations.CompleteLocationChecks(locationId);
                }
                else
                {
                    _offlineCheckQueue.Enqueue(locationId);
                    _storageService.SavePendingChecks(_offlineCheckQueue);
                }
            }
        }

        /// <summary>
        /// Sends queued offline checks and goal completion status once reconnected.
        /// </summary>
        private void FlushOfflineQueue()
        {
            lock (_lockObject)
            {
                if (!IsConnected || Session == null) return;

                if (_offlineCheckQueue.Count > 0)
                {
                    long[] toSend = _offlineCheckQueue.ToArray();
                    _offlineCheckQueue.Clear();

                    Session.Locations.CompleteLocationChecks(toSend);
                    _storageService.SavePendingChecks(_offlineCheckQueue);
                }

                if (_pendingGoalCompletion)
                {
                    Session.SetGoalAchieved();
                }
            }
        }

        private void OnItemReceived(IReceivedItemsHelper helper)
        {
            lock (_lockObject)
            {
                ItemHandler.CachedItems.Clear();
                ItemHandler.CachedItems.AddRange(helper.AllItemsReceived);

                while (_itemsReceivedIndex < helper.AllItemsReceived.Count)
                {
                    ItemInfo item = helper.AllItemsReceived[_itemsReceivedIndex];
                    _itemsToProcess.Enqueue(item.ItemId);
                    _itemsReceivedIndex++;
                    _storageService.SaveItemIndex(_itemsReceivedIndex);
                }
            }
        }

        /// <summary>
        /// Processes and grant items pending in the incoming queue to the local player.
        /// </summary>
        public void ProcessIncomingItems()
        {
            lock (_lockObject)
            {
                while (_itemsToProcess.Count > 0)
                {
                    long itemId = _itemsToProcess.Dequeue();
                    ItemHandler.GrantPlayerAPItem(itemId);
                }
            }
        }

        private static void ParseSlotData(Dictionary<string, object> slotData)
        {
            if (slotData == null) return;

            try
            {
                if (slotData.TryGetValue("starting_arcana_mode", out object arcanaMode))
                    StartingArcanaMode = Convert.ToInt32(arcanaMode);

                if (slotData.TryGetValue("element_licenses_mode", out object licensesMode))
                    ElementLicensesMode = Convert.ToInt32(licensesMode);

                if (slotData.TryGetValue("chaos_fragments_required", out object chaosReq))
                    ChaosFragmentsRequired = Convert.ToInt32(chaosReq);

                if (slotData.TryGetValue("starting_element", out object startElem) && startElem != null)
                    StartingElement = startElem.ToString();

                Plugin.Log.LogInfo(string.Format("[AP] SlotData loaded: ArcanaMode={0}, LicensesMode={1}, ChaosRequired={2}, StartingElement={3}",
                    StartingArcanaMode, ElementLicensesMode, ChaosFragmentsRequired, StartingElement));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[AP] Error parsing SlotData: {0}", ex));
            }
        }
    }
}