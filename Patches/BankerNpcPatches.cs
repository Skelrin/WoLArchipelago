using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to detect when player successfully trade gold with Doki the Banker to send an Archipelago check.
    /// </summary>
    [HarmonyPatch(typeof(BankerNpc))]
    public static class BankerNpcPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("TradeGold")]
        public static void TradeGoldPostfix(BankerNpc __instance)
        {
            bool goldTraded = Traverse.Create(__instance).Field<bool>("goldTraded").Value;

            string locName = "Doki the Banker Slot {0}";
            
            if (goldTraded)
            {
                Services.CheckHandler.SendNpcCheck(locName, 10);
            }
        }
    }
}