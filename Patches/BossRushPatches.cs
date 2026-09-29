using System.Collections.Generic;
using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches to deactivate boss rush logic which breaks level generation with biome keys and progression Locations.
    /// </summary>
    public static class BossRushPatches
    {
        [HarmonyPatch(typeof(BossRushNpc), nameof(BossRushNpc.Start))]
        public static class BossRushNpcStartPatch
        {
            private static readonly AccessTools.FieldRef<RunModifier, Dictionary<string, RunMod>> RunModsRef =
                AccessTools.FieldRefAccess<RunModifier, Dictionary<string, RunMod>>("runMods");

            [HarmonyPostfix]
            public static void Postfix(BossRushNpc __instance)
            {
                if (__instance == null) return;

                __instance.bossRushTP?.SetActive(false);
                __instance.bossRushTPDisabled?.SetActive(true);

                if (RunModifier.Instance != null)
                {
                    var mods = RunModsRef(RunModifier.Instance);
                    if (mods != null && mods.ContainsKey("RushRunMod"))
                    {
                        RunMod mod = mods["RushRunMod"];
                        if (mod != null)
                        {
                            mod.Deactivate();
                        }
                        mods.Remove("RushRunMod");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BossRushNpc), nameof(BossRushNpc.HandleConditionalInteraction))]
        public static class BossRushNpcInteractionPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(BossRushNpc __instance, ref bool __result)
            {
                GameUI.BroadcastNoticeMessage("Boss Rush deactivated in Archipelago mode !");
                SoundManager.PlayAudio("MenuError");

                __instance.playerLeavingDialog = true;
                __result = true;
                return false;
            }
        }
    }
}