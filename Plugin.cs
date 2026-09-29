using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using WoLArchipelago.Services;

namespace WoLArchipelago
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.skelrin.wolarchipelago";
        public const string PluginName = "WoL Archipelago";
        public const string PluginVersion = "1.0.0";

        private static readonly Queue<Action> _mainThreadActions = new();
        private static readonly object _queueLock = new();

        private Harmony _harmony;

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static APManager AP { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            StorageService.InitProfile();
            AP = new APManager();

            gameObject.AddComponent<ArchipelagoUI>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
            }
        }

        private void Start()
        {
            try
            {
                _harmony = new Harmony(PluginGUID);
                _harmony.PatchAll();
                Log.LogInfo(string.Format("{0} v{1} initialized successfully!", PluginName, PluginVersion));
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("Failed to apply Harmony patches: {0}", ex));
            }
        }

        private void Update()
        {
            // Process actions enqueued for the main Unity thread preventing freezes in-game
            lock (_queueLock)
            {
                while (_mainThreadActions.Count > 0)
                {
                    try
                    {
                        Action action = _mainThreadActions.Dequeue();
                        if (action != null)
                        {
                            action();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(string.Format("Main thread execution error: {0}", ex));
                    }
                }
            }

            AP?.ProcessIncomingItems();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ShopService.ClearSceneAssignments();
        }

        public static void ExecuteOnMainThread(Action action)
        {
            if (action == null) return;

            lock (_queueLock)
            {
                _mainThreadActions.Enqueue(action);
            }
        }

        /// <summary>
        /// Teleport the player to the game default spawn point after connecting to the archipelago multiworld
        /// </summary>
        public static void StartAPRun()
        {
            ExecuteOnMainThread(delegate
            {
                GameController.LoadLevel("PlayerRoom");
                ArchipelagoUI.Instance?.Hide();
            });
        }
    }
}