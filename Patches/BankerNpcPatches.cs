using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(BankerNpc))]
    public class BankerNpcPatches
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