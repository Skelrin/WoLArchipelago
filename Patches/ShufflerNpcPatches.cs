using HarmonyLib;

namespace WoLArchipelago.Patches
{
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