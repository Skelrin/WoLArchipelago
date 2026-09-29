using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to detect when player successfully trigger loop from Strange Time Keeper to send an Archipelago check.
    /// </summary>
    [HarmonyPatch(typeof(LoopNpc))]
    public class LoopNpcPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("InitiateLoop")]
        public static void InitiateLoopPrefix()
        {
            string locName = "Strange Time Keeper Slot {0}";

            Services.CheckHandler.SendNpcCheck(locName, 5);
        }
    }
}