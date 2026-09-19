using System;
using System.Collections.Generic;
using System.IO;

namespace WoLArchipelago
{
    public static class StatsManager
    {
        public static int TotalDashes { get; set; } = 0;
        public static int TotalStandardChests { get; set; } = 0;
        public static int TotalMiniChests { get; set; } = 0;
        public static int TotalMiniBossChests { get; set; } = 0;
        public static int TotalBossChests { get; set; } = 0;
        public static int TotalElementalChests { get; set; } = 0;
        public static int TotalPartyChests { get; set; } = 0;

        public static void LoadStats()
        {
            string path = StorageService.GetStatsFilePath();

            TotalDashes = 0;
            TotalStandardChests = 0;
            TotalMiniChests = 0;
            TotalMiniBossChests = 0;
            TotalBossChests = 0;
            TotalElementalChests = 0;
            TotalPartyChests = 0;

            if (!File.Exists(path))
            {
                SaveStats();
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string rawLine in lines)
                {
                    if (string.IsNullOrEmpty(rawLine)) continue;

                    string line = rawLine.Trim();
                    if (line.StartsWith("#") || line.StartsWith("//")) continue;

                    string[] parts = line.Split(['='], 2);
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int val))
                    {
                        switch (key)
                        {
                            case "TotalDashes": TotalDashes = val; break;
                            case "TotalStandardChests": TotalStandardChests = val; break;
                            case "TotalMiniChests": TotalMiniChests = val; break;
                            case "TotalMiniBossChests": TotalMiniBossChests = val; break;
                            case "TotalBossChests": TotalBossChests = val; break;
                            case "TotalElementalChests": TotalElementalChests = val; break;
                            case "TotalPartyChests": TotalPartyChests = val; break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[StatsManager] Error loading : {ex.Message}");
            }
        }

        public static void SaveStats()
        {
            string path = StorageService.GetStatsFilePath();
            Dictionary<string, string> stats = new Dictionary<string, string>();

            if (File.Exists(path))
            {
                try
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string rawLine in lines)
                    {
                        if (string.IsNullOrEmpty(rawLine)) continue;

                        string line = rawLine.Trim();
                        if (line.StartsWith("#") || line.StartsWith("//")) continue;

                        string[] parts = line.Split(['='], 2);
                        if (parts.Length == 2)
                        {
                            stats[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[StatsManager] Error reading before backup : {ex.Message}");
                }
            }

            stats["TotalDashes"] = TotalDashes.ToString();
            stats["TotalStandardChests"] = TotalStandardChests.ToString();
            stats["TotalMiniChests"] = TotalMiniChests.ToString();
            stats["TotalMiniBossChests"] = TotalMiniBossChests.ToString();
            stats["TotalBossChests"] = TotalBossChests.ToString();
            stats["TotalElementalChests"] = TotalElementalChests.ToString();
            stats["TotalPartyChests"] = TotalPartyChests.ToString();

            try
            {
                string[] output = new string[stats.Count];
                int index = 0;
                foreach (KeyValuePair<string, string> kvp in stats)
                {
                    output[index++] = kvp.Key + "=" + kvp.Value;
                }

                File.WriteAllLines(path, output);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[StatsManager] Error writing : {ex.Message}");
            }
        }

        public static int IncrementChestCount(string chestTypeName)
        {
            int newCount = -1;

            switch (chestTypeName)
            {
                case "Standard": TotalStandardChests++; newCount = TotalStandardChests; break;
                case "Mini": TotalMiniChests++; newCount = TotalMiniChests; break;
                case "MiniBoss": TotalMiniBossChests++; newCount = TotalMiniBossChests; break;
                case "Boss": TotalBossChests++; newCount = TotalBossChests; break;
                case "Elemental": TotalElementalChests++; newCount = TotalElementalChests; break;
                case "Party": TotalPartyChests++; newCount = TotalPartyChests; break;
            }

            if (newCount != -1)
            {
                SaveStats();
            }

            return newCount;
        }
    }
}