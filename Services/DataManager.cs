using System;
using System.Collections.Generic;
using System.IO;

namespace WoLArchipelago.Services
{
    public static class DataManager
    {
        public static int TotalDashes { get; set; }
        public static int TotalChestsOpened { get; set; }
        public static bool IsStartingInventoryApplied { get; set; }

        public static Dictionary<string, int> ChestCounts { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, int> EnemyCounts { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public static List<string> SavedHubRelics { get; set; } = new List<string>();

        public static void LoadData()
        {
            TotalDashes = 0;
            TotalChestsOpened = 0;
            IsStartingInventoryApplied = false;
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
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//")) continue;

                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string valStr = parts[1].Trim();

                    if (int.TryParse(valStr, out int val))
                    {
                        if (key == nameof(TotalDashes)) TotalDashes = val;
                        else if (key == nameof(TotalChestsOpened)) TotalChestsOpened = val;
                        else if (key.StartsWith("Chest_")) ChestCounts[key.Substring(6)] = val;
                        else if (key.StartsWith("Enemy_")) EnemyCounts[key.Substring(6)] = val;
                        else if (key == nameof(SavedHubRelics)) SavedHubRelics = [.. valStr.Split(',')];
                    }
                    else if (key == nameof(IsStartingInventoryApplied) && bool.TryParse(valStr, out bool boolVal))
                    {
                        IsStartingInventoryApplied = boolVal;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[DataManager] Error loading: {ex.Message}");
            }
        }

        public static void SaveData()
        {
            try
            {
                List<string> lines = new List<string>
                {
                    $"{nameof(TotalDashes)}={TotalDashes}",
                    $"{nameof(TotalChestsOpened)}={TotalChestsOpened}",
                    $"{nameof(IsStartingInventoryApplied)}={IsStartingInventoryApplied}",
                    $"{nameof(SavedHubRelics)}={string.Join(",", SavedHubRelics.ToArray())}"
                };

                foreach (KeyValuePair<string, int> kvp in ChestCounts) lines.Add($"Chest_{kvp.Key}={kvp.Value}");
                foreach (KeyValuePair<string, int> kvp in EnemyCounts) lines.Add($"Enemy_{kvp.Key}={kvp.Value}");

                File.WriteAllLines(StorageService.GetDataFilePath(), lines.ToArray());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[DataManager] Error saving: {ex.Message}");
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