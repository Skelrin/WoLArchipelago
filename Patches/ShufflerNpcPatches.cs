using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to detect when player successfully give an arcana to Nocturne the Cardist to send an Archipelago check.
    /// </summary>
    [HarmonyPatch(typeof(ShufflerNpc))]
    public class ShufflerNpcPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("InitCardShuffle")]
        public static void InitCardShufflePostfix()
        {
            string locName = "Nocturne the Cardist Slot {0}";

            Services.CheckHandler.SendNpcCheck(locName, 10);
        }
    }
}