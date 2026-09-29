using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to detect when player successfully enhance its outfit from Savile the Tailor to send an Archipelago check.
    /// </summary>
    [HarmonyPatch(typeof(TailorNpc))]
    public class TailorNpcPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("UpgradePlayerOutfit")]
        public static void UpgradePlayerOutfitPostfix(TailorNpc __instance)
        {
            if (__instance.player != null && __instance.player.outfitEnhanced)
            {
                string locName = "Savile the Tailor Slot {0}";

                Services.CheckHandler.SendNpcCheck(locName, 10);
            }
        }
    }
}