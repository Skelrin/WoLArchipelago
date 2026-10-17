using System.Collections.Generic;
using HarmonyLib;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches for tracking game statistics triggering Archipelago location checks.
    /// </summary>
    public static class GameStatsPatches
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

                if (PerfectFightHelper.MiniBosses.Contains(name) && PerfectFightHelper.MiniBossStartHealth != null)
                {
                    if (PerfectFightHelper.IsPerfect(PerfectFightHelper.MiniBossStartHealth))
                        CheckHandler.CheckLocation($"Perfect {name}");

                    PerfectFightHelper.MiniBossStartHealth = null;
                }
            }
        }

        [HarmonyPatch(typeof(GameData.GameStats), "UpdateIntStatValue")]
        public class GameStatsUpdateIntStatValuePatch
        {
            [HarmonyPostfix]
            public static void Postfix(GameData.GameStats __instance, GameData.Stat givenStat)
            {
                if (givenStat == GameData.Stat.Death)
                {
                    PerfectFightHelper.PlayerDied = true;
                    PerfectFightHelper.MiniBossStartHealth = null;
                }

                int total = __instance.GetIntStatValue(givenStat);

                string locationName = givenStat switch
                {
                    GameData.Stat.Painting => $"Break {total} Paintings",
                    _ => null
                };

                if (locationName != null)
                {
                    CheckHandler.CheckLocation(locationName);
                }
            }
        }
    }
}