using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to detect when player successfully give a relic to Cremire the Collector to send an Archipelago check.
    /// </summary>
    [HarmonyPatch(typeof(CollectorNpc))]
    public class CollectorNpcPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnRelicCollect")]
        public static void OnRelicCollectPostfix()
        {
            string locName = "Cremire the Collector Slot {0}";

            Services.CheckHandler.SendNpcCheck(locName, 10);
        }
    }
}