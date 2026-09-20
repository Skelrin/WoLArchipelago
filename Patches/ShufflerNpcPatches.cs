using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(ShufflerNpc))]
    public class ShufflerNpcPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnCardSelected")]
        public static void OnCardSelectedPostfix()
        {
            string locName = $"Nocturne the Cardist Slot {0}";

            Services.CheckHandler.SendNpcCheck(locName, 10);
        }
    }
}