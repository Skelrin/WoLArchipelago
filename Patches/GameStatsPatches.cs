using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(GameData.GameStats), "EnemyDefeated")]
    public class GameStatsEnemyDefeatedPatch
    {
        [HarmonyPostfix]
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
        [HarmonyPostfix]
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
        [HarmonyPostfix]
        public static void Postfix(Player.SkillState __instance)
        {
            if (__instance == null || !__instance.isDash) return;

            if (Services.StatsManager.TotalDashes >= 500) return;

            Services.StatsManager.TotalDashes++;
            Services.StatsManager.SaveStats();

            int dashes = Services.StatsManager.TotalDashes;
            if (dashes == 100 || dashes == 500)
            {
                string locationName = "Dash " + dashes + " times";
                long locId = APItemLocationDatabase.GetLocationId(locationName);

                if (locId != -1)
                {
                    Plugin.AP.SendLocationCheck(locId);
                }
            }
        }
    }
}