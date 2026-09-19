using System.Collections.Generic;
using HarmonyLib;

namespace WoLArchipelago.Patches
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

            if (__instance.bossRushTP != null) __instance.bossRushTP.SetActive(false);
            if (__instance.bossRushTPDisabled != null) __instance.bossRushTPDisabled.SetActive(true);

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