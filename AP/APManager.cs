using System;
using System.Collections.Generic;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;

namespace WoLArchipelago
{
    public class APManager
    {
        public ArchipelagoSession session { get; private set; }
        private string lastHost;
        private int lastPort;
        private string lastSlot;
        private string lastPassword;
        private readonly object lockObject = new object();
        private readonly Services.StorageService storageService = new Services.StorageService();
        private readonly HashSet<long> checkedLocations = new HashSet<long>();
        public Dictionary<long, ScoutedItemInfo> ScoutedLocations { get; private set; } = new Dictionary<long, ScoutedItemInfo>();
        private Queue<long> offlineCheckQueue = new Queue<long>();
        private int itemsReceivedIndex = 0;
        private readonly Queue<long> itemsToProcess = new Queue<long>();
        public static string CurrentAPSavePrefix => Services.StorageService.CurrentAPSavePrefix;
        public bool IsConnected { get; private set; }
        public string StatusMessage { get; private set; } = "Disconnected";
        public int StartingArcanaMode { get; private set; }
        public int ElementLicensesMode { get; private set; }
        public static int ChaosFragmentsRequired { get; private set; }
        public string StartingElement { get; private set; }
        private bool pendingGoalCompletion = false;

        public APManager()
        {
            Services.StorageService.InitProfile();
            offlineCheckQueue = storageService.LoadPendingChecks(checkedLocations);
            itemsReceivedIndex = storageService.LoadItemIndex();
            pendingGoalCompletion = storageService.LoadGoalCompletion();
        }

        public void Connect(string host, int port, string slotName, string password = null, bool isReconnecting = false)
        {
            lastHost = host; lastPort = port; lastSlot = slotName; lastPassword = password;

            StatusMessage = "Connecting...";

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    DisconnectInternal();

                    session = ArchipelagoSessionFactory.CreateSession(host, port);
                    LoginResult result = session.TryConnectAndLogin(
                        "Wizard of Legend",
                        slotName,
                        ItemsHandlingFlags.AllItems,
                        password: password
                    );

                    if (result is LoginSuccessful loginSuccess)
                    {
                        IsConnected = true;
                        StatusMessage = "Online";
                        storageService.SetAPSaveProfile(session.RoomState.Seed, slotName);

                        ParseSlotData(loginSuccess.SlotData);

                        Plugin.ExecuteOnMainThread(() =>
                        {
                            ArchipelagoUI.Instance?.ToggleHasConnectedOnce();
                        });

                        lock (lockObject)
                        {
                            checkedLocations.Clear();
                            offlineCheckQueue.Clear();
                            itemsToProcess.Clear();
                            Services.ItemHandler.CachedItems.Clear();

                            itemsReceivedIndex = storageService.LoadItemIndex();
                            offlineCheckQueue = storageService.LoadPendingChecks(checkedLocations);

                            Services.ItemHandler.CachedItems.AddRange(session.Items.AllItemsReceived);
                            Services.StatsManager.LoadStats();
                        }

                        session.Locations.ScoutLocationsAsync(scoutedInfo =>
                        {
                            lock (lockObject)
                            {
                                ScoutedLocations = scoutedInfo;
                            }
                        }, new List<long>(session.Locations.AllLocations).ToArray());

                        if (!isReconnecting)
                        {
                            Plugin.StartAPRun();
                        }

                        session.Items.ItemReceived += OnItemReceived;
                        session.Socket.SocketClosed += OnSocketClosed;

                        foreach (long locationId in session.Locations.AllLocationsChecked)
                        {
                            checkedLocations.Add(locationId);
                        }

                        FlushOfflineQueue();
                    }
                    else if (result is LoginFailure failure)
                    {
                        IsConnected = false;
                        StatusMessage = $"Failed: {string.Join(", ", failure.Errors)}";
                        Plugin.Log.LogError($"[AP] {StatusMessage}");
                    }
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    StatusMessage = $"Error: {ex.Message}";
                    Plugin.Log.LogError($"[AP] Connection error: {ex}");
                }
            });
        }

        public string GetLocationDescription(long locationId)
        {
            lock (lockObject)
            {
                if (ScoutedLocations != null && ScoutedLocations.TryGetValue(locationId, out ScoutedItemInfo itemInfo))
                {
                    string playerName = itemInfo.Player?.Alias ?? itemInfo.Player?.Name ?? "Unknown Player";
                    string itemName = !string.IsNullOrEmpty(itemInfo.ItemDisplayName) ? itemInfo.ItemDisplayName : itemInfo.ItemName;

                    return $"{playerName} : {itemName}";
                }
            }
            return "Emplacement Archipelago";
        }

        public void Disconnect()
        {
            DisconnectInternal();
            StatusMessage = "Disconnected (Offline Mode)";
        }

        private void OnSocketClosed(string reason)
        {
            IsConnected = false;
            StatusMessage = "Connection Lost!";
            Plugin.Log.LogWarning($"[AP] Connection lost: {reason}. Attempting reconnect...");

            Plugin.ExecuteOnMainThread(() =>
            {
                Plugin.Instance.StartCoroutine(AutoReconnectCoroutine());
            });
        }

        private System.Collections.IEnumerator AutoReconnectCoroutine()
        {
            yield return new UnityEngine.WaitForSeconds(3f);

            if (!IsConnected)
            {
                Connect(lastHost, lastPort, lastSlot, lastPassword, isReconnecting: true);
            }
        }

        private void DisconnectInternal()
        {
            if (session != null)
            {
                session.Items.ItemReceived -= OnItemReceived;
                if (session.Socket != null)
                {
                    session.Socket.SocketClosed -= OnSocketClosed;
                }
                session = null;
            }
            IsConnected = false;
            StatusMessage = "Offline";
            Plugin.Log.LogWarning("[AP] Offline mode active.");
        }

        public void CompleteGoal()
        {
            lock (lockObject)
            {
                pendingGoalCompletion = true;
                storageService.SaveGoalCompletion(true);

                if (IsConnected && session != null)
                {
                    Plugin.Log.LogInfo("[AP] Goal completed!");
                    session.SetGoalAchieved();
                }
                else
                {
                    Plugin.Log.LogWarning("[AP] Goal completed offline. Saved for next connection.");
                }
            }
        }

        public HashSet<long> GetCheckedLocation()
        {
            return checkedLocations;
        }

        public void SendLocationCheck(long locationId)
        {
            lock (lockObject)
            {
                if (checkedLocations.Contains(locationId)) return;

                checkedLocations.Add(locationId);

                if (IsConnected && session != null)
                {
                    Plugin.Log.LogInfo($"[AP] Sending check: {locationId}");
                    session.Locations.CompleteLocationChecks(locationId);
                }
                else
                {
                    offlineCheckQueue.Enqueue(locationId);
                    storageService.SavePendingChecks(offlineCheckQueue);
                }
            }
        }

        private void FlushOfflineQueue()
        {
            lock (lockObject)
            {
                if (!IsConnected || session == null) return;

                if (offlineCheckQueue.Count > 0)
                {
                    List<long> toSend = new List<long>();
                    while (offlineCheckQueue.Count > 0)
                    {
                        toSend.Add(offlineCheckQueue.Dequeue());
                    }
                    session.Locations.CompleteLocationChecks(toSend.ToArray());
                    storageService.SavePendingChecks(offlineCheckQueue);
                }

                if (pendingGoalCompletion)
                {
                    session.SetGoalAchieved();
                }
            }
        }

        private void OnItemReceived(IReceivedItemsHelper helper)
        {
            lock (lockObject)
            {
                Services.ItemHandler.CachedItems.Clear();
                Services.ItemHandler.CachedItems.AddRange(helper.AllItemsReceived);

                while (itemsReceivedIndex < helper.AllItemsReceived.Count)
                {
                    ItemInfo item = helper.AllItemsReceived[itemsReceivedIndex];
                    itemsToProcess.Enqueue(item.ItemId);
                    itemsReceivedIndex++;
                    storageService.SaveItemIndex(itemsReceivedIndex);
                }
            }
        }

        public void ProcessIncomingItems()
        {
            lock (lockObject)
            {
                while (itemsToProcess.Count > 0)
                {
                    long itemId = itemsToProcess.Dequeue();
                    Services.ItemHandler.GrantPlayerAPItem(itemId);
                }
            }
        }

        private void ParseSlotData(Dictionary<string, object> slotData)
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

                Plugin.Log.LogInfo($"[AP] SlotData loaded: ArcanaMode={StartingArcanaMode}, LicensesMode={ElementLicensesMode}, ChaosRequired={ChaosFragmentsRequired}, StartingElement={StartingElement}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[AP] Error parsing SlotData: {ex}");
            }
        }
    }
}