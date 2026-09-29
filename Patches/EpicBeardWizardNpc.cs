using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches to deactivate Mah-Lind logic that fix first biome in the dungeon which breaks level generation with biome keys
    /// </summary>
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