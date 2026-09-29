using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace WoLArchipelago.Services
{
    /// <summary>
    /// Handles profile resolution and I/O for Archipelago offline sync queues.
    /// </summary>
    public class StorageService
    {
        private static readonly string LastProfileFilePath = Path.Combine(Paths.ConfigPath, "WoL_AP_LastProfile.txt");

        public static string CurrentAPSavePrefix { get; private set; }

        static StorageService()
        {
            CurrentAPSavePrefix = "AP_Default";
        }

        private string PendingChecksFilePath
        {
            get { return Path.Combine(GetProfileFolderPath(), "PendingChecks.txt"); }
        }

        private string ItemIndexFilePath
        {
            get { return Path.Combine(GetProfileFolderPath(), "ItemIndex.txt"); }
        }

        /// <summary>
        /// Reads the last active AP save profile prefix from global config.
        /// </summary>
        public static void InitProfile()
        {
            if (!File.Exists(LastProfileFilePath)) return;

            try
            {
                string savedPrefix = File.ReadAllText(LastProfileFilePath).Trim();
                if (!string.IsNullOrEmpty(savedPrefix))
                {
                    CurrentAPSavePrefix = savedPrefix;
                    Plugin.Log.LogInfo(string.Format("[AP Save] Loaded active AP Profile: {0}", CurrentAPSavePrefix));
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[AP Save] Error reading last profile: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Sets and persists a new active AP profile based on room seed and slot name.
        /// </summary>
        public void SetAPSaveProfile(string seed, string slotName)
        {
            string safeSlot = string.Join("_", slotName.Split(Path.GetInvalidFileNameChars()));
            CurrentAPSavePrefix = string.Format("AP_{0}_{1}", seed, safeSlot);

            try
            {
                File.WriteAllText(LastProfileFilePath, CurrentAPSavePrefix);
                Plugin.Log.LogInfo(string.Format("[AP Save] AP Profile set to: {0}", CurrentAPSavePrefix));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[AP Save] Failed to persist active profile: {0}", ex.Message));
            }
        }

        public void SavePendingChecks(IEnumerable<long> pendingChecks)
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (long id in pendingChecks)
                {
                    lines.Add(id.ToString());
                }
                File.WriteAllLines(PendingChecksFilePath, [.. lines]);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[AP] Error saving pending checks: {0}", ex.Message));
            }
        }

        public Queue<long> LoadPendingChecks(HashSet<long> checkedLocations)
        {
            Queue<long> queue = new();
            try
            {
                string path = PendingChecksFilePath;
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string line in lines)
                    {
                        long id;
                        if (long.TryParse(line, out id) && !queue.Contains(id))
                        {
                            queue.Enqueue(id);
                            checkedLocations.Add(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[AP] Error loading pending checks: {0}", ex.Message));
            }
            return queue;
        }

        public void SaveItemIndex(int index)
        {
            try
            {
                File.WriteAllText(ItemIndexFilePath, index.ToString());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[AP] Error saving item index: {0}", ex.Message));
            }
        }

        public int LoadItemIndex()
        {
            try
            {
                string path = ItemIndexFilePath;
                if (File.Exists(path) && int.TryParse(File.ReadAllText(path), out int savedIndex))
                {
                    return savedIndex;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[AP] Error loading item index: {0}", ex.Message));
            }
            return 0;
        }

        public static string GetProfileFolderPath()
        {
            string apFolder = Path.Combine(Application.persistentDataPath, "AP_Saves");

            if (!string.IsNullOrEmpty(CurrentAPSavePrefix))
            {
                apFolder = Path.Combine(apFolder, CurrentAPSavePrefix);
            }

            if (!Directory.Exists(apFolder))
            {
                Directory.CreateDirectory(apFolder);
            }

            return apFolder;
        }

        public static string GetDataFilePath()
        {
            return Path.Combine(GetProfileFolderPath(), "Stats.txt");
        }
    }
}