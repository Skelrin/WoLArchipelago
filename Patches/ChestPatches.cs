using HarmonyLib;

namespace WoLArchipelago
{
    [HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.Break))]
    public static class TreasureChestBreakPatch
    {
        [HarmonyPrefix]
        public static void Prefix(TreasureChest __instance)
        {
            if (__instance != null && !__instance.opened && !__instance.destroyed && __instance.dropLoot)
            {
                Plugin.Log.LogInfo(__instance.chestType.ToString());
            }
        }
    }
}