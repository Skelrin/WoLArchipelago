using System.Collections.Generic;
using HarmonyLib;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(GameData.GameStats), "EnemyDefeated")]
    public class GameStatsEnemyDefeatedPatch
    {
        private static readonly Dictionary<string, string> ElementMap = new()
        {
            { "Frost", "Water" },
            { "Earth", "Earth" },
            { "Flame", "Fire" },
            { "Wind", "Air" },
            { "Thunder", "Lightning" }
        };

        [HarmonyPostfix]
        public static void Postfix(GameData.GameStats __instance, string name)
        {
            foreach (KeyValuePair<string, string> entry in ElementMap)
            {
                if (name.Contains(entry.Key))
                {
                    int total = DataManager.IncrementEnemyCount(entry.Value);
                    CheckHandler.CheckLocation($"{entry.Value} Enemies Defeated {total} times");
                    break;
                }
            }

            int count = __instance.GetEnemyDefeatedCount(name);
            string locName = (count == 1) ? $"{name} Defeated" : $"{name} Defeated {count} times";

            if (CheckHandler.CheckLocation(locName) && locName == "FinalBoss Defeated")
            {
                Plugin.AP.CompleteGoal();
            }
        }
    }

    [HarmonyPatch(typeof(GameData.GameStats), "UpdateIntStatValue")]
    public class GameStatsUpdateIntStatValuePatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameData.GameStats __instance, GameData.Stat givenStat)
        {
            int total = __instance.GetIntStatValue(givenStat);

            string locationName = givenStat switch
            {
                GameData.Stat.Painting => $"Break {total} Paintings",
                GameData.Stat.Death => $"Die {total} times",
                GameData.Stat.Fall => $"Fall {total} times",
                _ => null
            };

            if (locationName != null)
            {
                CheckHandler.CheckLocation(locationName);
            }
        }
    }

    [HarmonyPatch(typeof(Player.SkillState), "BaseOnEnter")]
    public class DashTrackerPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player.SkillState __instance)
        {
            if (__instance == null || !__instance.isDash || DataManager.TotalDashes >= 500) return;

            DataManager.TotalDashes++;

            if (DataManager.TotalDashes is 100 or 500)
            {
                CheckHandler.CheckLocation($"Dash {DataManager.TotalDashes} times");
            }
        }
    }
}