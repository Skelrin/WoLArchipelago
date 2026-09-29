using System;
using System.Collections.Generic;
using System.IO;

namespace WoLArchipelago.Services
{
    /// <summary>
    /// Manages persistent run statistics and slot-specific progression flags.
    /// </summary>
    public static class DataManager
    {
        public static int TotalDashes { get; set; }
        public static int TotalChestsOpened { get; set; }
        public static bool IsStartingInventoryApplied { get; set; }
        public static bool IsGoalCompleted { get; set; }

        public static Dictionary<string, int> ChestCounts { get; private set; }
        public static Dictionary<string, int> EnemyCounts { get; private set; }
        public static List<string> SavedHubRelics { get; set; }

        static DataManager()
        {
            ChestCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            EnemyCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            SavedHubRelics = new List<string>();
        }

        /// <summary>
        /// Loads slot stats and flags from the current AP profile folder.
        /// </summary>
        public static void LoadData()
        {
            TotalDashes = 0;
            TotalChestsOpened = 0;
            IsStartingInventoryApplied = false;
            IsGoalCompleted = false;

            ChestCounts.Clear();
            EnemyCounts.Clear();
            SavedHubRelics.Clear();

            string path = StorageService.GetDataFilePath();
            if (!File.Exists(path))
            {
                SaveData();
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//")) continue;

                    string[] parts = line.Split(['='], 2);
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string valStr = parts[1].Trim();


                    if (int.TryParse(valStr, out int val))
                    {
                        if (key == "TotalDashes") TotalDashes = val;
                        else if (key == "TotalChestsOpened") TotalChestsOpened = val;
                        else if (key.StartsWith("Chest_")) ChestCounts[key.Substring(6)] = val;
                        else if (key.StartsWith("Enemy_")) EnemyCounts[key.Substring(6)] = val;
                    }
                    else if (bool.TryParse(valStr, out bool boolVal))
                    {
                        if (key == "IsStartingInventoryApplied") IsStartingInventoryApplied = boolVal;
                        else if (key == "IsGoalCompleted") IsGoalCompleted = boolVal;
                    }
                    else if (key == "SavedHubRelics")
                    {
                        SavedHubRelics = string.IsNullOrEmpty(valStr)
                            ? []
                            : [.. valStr.Split(',')];
                    }
                }

                Plugin.Log.LogInfo(string.Format("[DataManager] Loaded SavedHubRelics: {0}", string.Join(",", [.. SavedHubRelics])));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[DataManager] Error loading data: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Saves current slot stats and flags to the active AP profile folder.
        /// </summary>
        public static void SaveData()
        {
            try
            {
                List<string> lines =
                [
                    string.Format("TotalDashes={0}", TotalDashes),
                    string.Format("TotalChestsOpened={0}", TotalChestsOpened),
                    string.Format("IsStartingInventoryApplied={0}", IsStartingInventoryApplied),
                    string.Format("IsGoalCompleted={0}", IsGoalCompleted),
                    string.Format("SavedHubRelics={0}", string.Join(",", SavedHubRelics.ToArray())),
                ];

                foreach (KeyValuePair<string, int> kvp in ChestCounts)
                {
                    lines.Add(string.Format("Chest_{0}={1}", kvp.Key, kvp.Value));
                }
                foreach (KeyValuePair<string, int> kvp in EnemyCounts)
                {
                    lines.Add(string.Format("Enemy_{0}={1}", kvp.Key, kvp.Value));
                }

                File.WriteAllLines(StorageService.GetDataFilePath(), lines.ToArray());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(string.Format("[DataManager] Error saving data: {0}", ex.Message));
            }
        }

        public static int IncrementEnemyCount(string enemyType)
        {
            EnemyCounts.TryGetValue(enemyType, out int current);
            int newCount = current + 1;
            EnemyCounts[enemyType] = newCount;

            SaveData();
            return newCount;
        }

        public static int IncrementChestCount(string chestType)
        {
            TotalChestsOpened++;

            ChestCounts.TryGetValue(chestType, out int current);
            int newCount = current + 1;
            ChestCounts[chestType] = newCount;

            SaveData();
            return newCount;
        }
    }
}