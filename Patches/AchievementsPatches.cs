using HarmonyLib;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to prevent triggering Steam Achievements.
    /// </summary>
    [HarmonyPatch(typeof(AchievementManager), nameof(AchievementManager.UnlockAchievement))]
    public static class AchievementManagerPatches
    {
        [HarmonyPrefix]
        public static bool UnlockAchievementPrefix()
        {
            return false;
        }
    }

    /// <summary>
    /// Patch to monitor boss fights to trigger the "Perfect a boss" location check if no player takes damage.
    /// </summary>
    [HarmonyPatch(typeof(BossRoomEventHandler), nameof(BossRoomEventHandler.CheckForPerfectFight))]
    public static class BossRoomEventHandlerPatches
    {
        [HarmonyPostfix]
        public static void CheckForPerfectFightPostfix(int[] ___playerStartHealthValues)
        {
            if (___playerStartHealthValues == null || ___playerStartHealthValues.Length == 0) return;

            Player[] activePlayers = GameController.activePlayers;
            if (activePlayers == null || activePlayers.Length == 0) return;

            bool hasValidPlayer = false;
            bool tookDamage = false;

            foreach (Player player in activePlayers)
            {
                if (player == null || player.health == null) continue;

                hasValidPlayer = true;

                if (player.playerID < ___playerStartHealthValues.Length &&
                    player.health.CurrentHealthValue < ___playerStartHealthValues[player.playerID])
                {
                    tookDamage = true;
                    break;
                }
            }

            if (hasValidPlayer && !tookDamage)
            {
                CheckHandler.CheckLocation("Perfect a boss");
            }
        }
    }

    /// <summary>
    /// Patch to trigger the "Have 1000 gold" location check when gold balance reaches the required threshold when picking up gold.
    /// </summary>
    [HarmonyPatch(typeof(Gold), "OnTriggerStay2D")]
    public static class GoldPatches
    {
        [HarmonyPostfix]
        public static void OnTriggerStay2DPostfix()
        {
            if (Player.goldWallet != null && Player.goldWallet.balance >= 1000)
            {
                CheckHandler.CheckLocation("Have 1000 gold");
            }
        }
    }

    /// <summary>
    /// Patch to trigger the "Have 500 chaos gems" location check when chaos gem balance reaches the required threshold when picking up chaos gems.
    /// </summary>
    [HarmonyPatch(typeof(Platinum), "OnTriggerStay2D")]
    public static class PlatinumPatches
    {
        [HarmonyPostfix]
        public static void OnTriggerStay2DPostfix()
        {
            if (Player.platWallet != null && Player.platWallet.balance >= 500)
            {
                CheckHandler.CheckLocation("Have 500 chaos gems");
            }
        }
    }
}