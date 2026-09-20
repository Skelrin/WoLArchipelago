using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace WoLArchipelago
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.skelrin.wolarchipelago";
        public const string PluginName = "WoL Archipelago";
        public const string PluginVersion = "0.4.0";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static APManager AP { get; private set; }

        private Harmony harmony;
        private static readonly Queue<Action> mainThreadActions = new Queue<Action>();
        private static readonly object queueLock = new object();

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Services.StorageService.InitProfile();
            AP = new APManager();

            gameObject.AddComponent<ArchipelagoUI>();
            gameObject.AddComponent<DebugController>();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            harmony?.UnpatchSelf();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Patches.ItemStoreItemPatches.ClearSceneAssignments();
            Patches.SkillStoreItemPatches.ClearSceneAssignments();
            Patches.OutfitStoreItemPatches.ClearSceneAssignments();
        }

        private void Start()
        {
            try
            {
                harmony = new Harmony(PluginGUID);
                harmony.PatchAll();
                Log.LogInfo($"{PluginName} v{PluginVersion} initialized successfully!");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply Harmony patches: {ex}");
            }
        }

        private void Update()
        {
            lock (queueLock)
            {
                while (mainThreadActions.Count > 0)
                {
                    try
                    {
                        mainThreadActions.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Main thread execution error: {ex}");
                    }
                }
            }

            AP?.ProcessIncomingItems();
        }

        public static void ExecuteOnMainThread(Action action)
        {
            lock (queueLock)
            {
                mainThreadActions.Enqueue(action);
            }
        }

        public static void StartAPRun()
        {
            ExecuteOnMainThread(() =>
            {
                GameController.LoadLevel("PlayerRoom");
                ArchipelagoUI.Instance?.Hide();
            });
        }
    }
}