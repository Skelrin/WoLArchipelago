using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(CollectorNpc))]
    public class CollectorNpcPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnRelicCollect")]
        public static void OnRelicCollectPostfix()
        {
            string locName = $"Cremire the Collector Slot {0}";

            Services.CheckHandler.SendNpcCheck(locName, 10);
        }
    }
}