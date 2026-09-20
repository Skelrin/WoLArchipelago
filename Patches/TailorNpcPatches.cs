using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(TailorNpc))]
    public class TailorNpcPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("UpgradePlayerOutfit")]
        public static void UpgradePlayerOutfitPostfix(TailorNpc __instance)
        {
            if (__instance.player != null && __instance.player.outfitEnhanced)
            {
                string locName = $"Savile the Tailor Slot {0}";

                Services.CheckHandler.SendNpcCheck(locName, 10);
            }
        }
    }
}