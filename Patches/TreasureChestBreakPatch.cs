using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.Break))]
    public class TreasureChestBreakPatch
    {
        [HarmonyPrefix]
        public static void Prefix(TreasureChest __instance)
        {
            if (__instance != null && !__instance.opened && !__instance.destroyed && __instance.dropLoot)
            {
                string chestTypeName = __instance.chestType.ToString();
                
                int currentCount = Services.StatsManager.IncrementChestCount(chestTypeName);
                if (currentCount <= 0) return;

                string locationName = chestTypeName + " Chest Slot " + currentCount;
                long locId = APItemLocationDatabase.GetLocationId(locationName);

                if (locId != -1)
                {
                    Plugin.Log.LogInfo("Opened : " + locationName);
                    Plugin.AP.SendLocationCheck(locId);
                }
            }
        }
    }
}