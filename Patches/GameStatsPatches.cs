using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(GameData.GameStats), "EnemyDefeated")]
    public class GameStatsEnemyDefeatedPatch
    {
        public static void Postfix(GameData.GameStats __instance, string name)
        {
            int count = __instance.GetEnemyDefeatedCount(name);

            Plugin.Log.LogInfo($"Name : {name} Count : {count}");
            string locationName = (count == 1) 
                ? $"{name} Defeated" 
                : $"{name} Defeated {count} times";

            long locId = APItemLocationDatabase.GetLocationId(locationName);

            if (locId != -1)
            {
                Plugin.AP.SendLocationCheck(locId);
            }
        }
    }

    [HarmonyPatch(typeof(GameData.GameStats), "UpdateIntStatValue")]
    public class GameStatsUpdateIntStatValuePatch
    {
        public static void Postfix(GameData.GameStats __instance, GameData.Stat givenStat)
        {
            int total = __instance.GetIntStatValue(givenStat);
            string locationName = null;

            switch (givenStat)
            {
                case GameData.Stat.Painting:
                    locationName = $"Break {total} Paintings";
                    break;
                case GameData.Stat.Death:
                    locationName = $"Die {total} times";
                    break;
                case GameData.Stat.Fall:
                    locationName = $"Fall {total} times";
                    break;
                default:
                    break;
            }

            if (locationName != null)
            {
                long locId = APItemLocationDatabase.GetLocationId(locationName);
                
                if (locId != -1)
                {
                    Plugin.AP.SendLocationCheck(locId);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player.SkillState), "BaseOnEnter")]
    public class DashTrackerPatch
    {
        public static int CachedDashes = -1;

        private static string GetStatsFilePath()
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

        private static int LoadDashes(string path)
        {
            if (!File.Exists(path)) return 0;

            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                if (line.StartsWith("TotalDashes="))
                {
                    if (int.TryParse(line.Substring(12), out int val))
                    {
                        return val;
                    }
                }
            }
            return 0;
        }

        public static void SaveDashesToDisk()
        {
            if (CachedDashes == -1) return;

            string path = GetStatsFilePath();
            Dictionary<string, string> stats = new Dictionary<string, string>();
            
            if (File.Exists(path))
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        stats[parts[0]] = parts[1];
                    }
                }
            }
            
            stats["TotalDashes"] = CachedDashes.ToString();
            
            List<string> output = new List<string>();
            foreach (var kvp in stats)
            {
                output.Add($"{kvp.Key}={kvp.Value}");
            }
            
            File.WriteAllLines(path, output.ToArray());
        }

        [HarmonyPostfix]
        public static void Postfix(Player.SkillState __instance)
        {
            if (__instance == null || !__instance.isDash) return;

            if (CachedDashes == -1)
            {
                CachedDashes = LoadDashes(GetStatsFilePath());
            }

            if (CachedDashes >= 500) return;

            CachedDashes++;

            if (CachedDashes == 100 || CachedDashes == 500)
            {
                string locationName = $"Dash {CachedDashes} times";
                long locId = APItemLocationDatabase.GetLocationId(locationName);
            
                if (locId != -1)
                {
                    Plugin.AP.SendLocationCheck(locId);
                }
            }
        }
    }
}