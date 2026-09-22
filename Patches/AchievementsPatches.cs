using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(AchievementManager), nameof(AchievementManager.UnlockAchievement))]
    public static class AchievementsPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }
}