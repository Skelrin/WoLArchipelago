using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace WoLArchipelago.Services
{
    public class StorageService
    {
        private static readonly string lastProfileFilePath = Path.Combine(Paths.ConfigPath, "WoL_AP_LastProfile.txt");
        public static string CurrentAPSavePrefix { get; private set; } = "AP_Default";

        private string PendingChecksFilePath => Path.Combine(Paths.ConfigPath, $"{CurrentAPSavePrefix}_PendingChecks.txt");
        private string ItemIndexFilePath => Path.Combine(Paths.ConfigPath, $"{CurrentAPSavePrefix}_ItemIndex.txt");
        private string GoalFilePath => Path.Combine(Paths.ConfigPath, $"{CurrentAPSavePrefix}_Goal.txt");

        public static void InitProfile()
        {
            if (!File.Exists(lastProfileFilePath)) return;
            try
            {
                string savedPrefix = File.ReadAllText(lastProfileFilePath).Trim();
                if (!string.IsNullOrEmpty(savedPrefix))
                {
                    CurrentAPSavePrefix = savedPrefix;
                    Plugin.Log.LogInfo($"[AP Save] Loaded AP Profile: {CurrentAPSavePrefix}");
                }
            }
            catch { }
        }

        public void SetAPSaveProfile(string seed, string slotName)
        {
            string safeSlot = string.Join("_", slotName.Split(Path.GetInvalidFileNameChars()));
            CurrentAPSavePrefix = $"AP_{seed}_{safeSlot}";

            try
            {
                File.WriteAllText(lastProfileFilePath, CurrentAPSavePrefix);
                Plugin.Log.LogInfo($"[AP Save] AP Profile set to: {CurrentAPSavePrefix}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[AP Save] Failed to save profile: {ex.Message}");
            }
        }

        public void SavePendingChecks(IEnumerable<long> pendingChecks)
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (long id in pendingChecks) lines.Add(id.ToString());
                File.WriteAllLines(PendingChecksFilePath, lines.ToArray());
            }
            catch (Exception ex) { Plugin.Log.LogError($"[AP] Error saving pending checks: {ex.Message}"); }
        }

        public Queue<long> LoadPendingChecks(HashSet<long> checkedLocations)
        {
            Queue<long> queue = new Queue<long>();
            try
            {
                if (File.Exists(PendingChecksFilePath))
                {
                    string[] lines = File.ReadAllLines(PendingChecksFilePath);
                    foreach (string line in lines)
                    {
                        if (long.TryParse(line, out long id) && !queue.Contains(id))
                        {
                            queue.Enqueue(id);
                            checkedLocations.Add(id);
                        }
                    }
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"[AP] Error loading pending checks: {ex.Message}"); }
            return queue;
        }

        public void SaveItemIndex(int index)
        {
            try { File.WriteAllText(ItemIndexFilePath, index.ToString()); }
            catch (Exception ex) { Plugin.Log.LogError($"[AP] Error saving item index: {ex.Message}"); }
        }

        public int LoadItemIndex()
        {
            try
            {
                if (File.Exists(ItemIndexFilePath) && int.TryParse(File.ReadAllText(ItemIndexFilePath), out int savedIndex))
                {
                    return savedIndex;
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"[AP] Error loading item index: {ex.Message}"); }
            return 0;
        }

        public void SaveGoalCompletion(bool isCompleted)
        {
            try
            {
                File.WriteAllText(GoalFilePath, isCompleted.ToString());
            }
            catch (Exception ex) { Plugin.Log.LogError($"[AP] Error saving goal completion: {ex.Message}"); }
        }

        public bool LoadGoalCompletion()
        {
            try
            {
                if (File.Exists(GoalFilePath) && bool.TryParse(File.ReadAllText(GoalFilePath), out bool isCompleted))
                {
                    return isCompleted;
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"[AP] Error loading goal completion: {ex.Message}"); }
            return false;
        }

        public static string GetDataFilePath()
        {
            string apFolder = Path.Combine(Application.persistentDataPath, "AP_Saves");
            
            if (!string.IsNullOrEmpty(APManager.CurrentAPSavePrefix))
            {
                apFolder = Path.Combine(apFolder, APManager.CurrentAPSavePrefix);
            }

            if (!Directory.Exists(apFolder))
            {
                Directory.CreateDirectory(apFolder);
            }

            return Path.Combine(apFolder, "Stats.txt");
        }
    }
}