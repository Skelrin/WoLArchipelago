using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(Npc), nameof(Npc.Interacted))]
    public static class NpcInteractedPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Npc __instance)
        {
            if (__instance is EpicBeardWizardNpc)
            {
                return false;
            }
            return true;
        }
    }
}