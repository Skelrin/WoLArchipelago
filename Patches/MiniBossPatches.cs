using HarmonyLib;

namespace WoLArchipelago
{
    [HarmonyPatch(typeof(MiniBoss.MiniBossDeadState), nameof(MiniBoss.MiniBossDeadState.OnEnter))]
    public static class MiniBossDeadStatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(MiniBoss.MiniBossDeadState __instance)
        {
            if (__instance != null && __instance.parent != null)
            {
                LocationHandler.OnMiniBossDefeated(__instance.parent);
            }
        }
    }
}