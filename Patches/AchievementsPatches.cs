using HarmonyLib;
using WoLArchipelago.Services;

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

    [HarmonyPatch(typeof(BossRoomEventHandler), nameof(BossRoomEventHandler.CheckForPerfectFight))]
    public class BossRoomEventHandler_CheckForPerfectFight_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(BossRoomEventHandler __instance, int[] ___playerStartHealthValues)
        {
            if (___playerStartHealthValues == null) return;

            bool isPerfect = false;
            Player[] activePlayers = GameController.activePlayers;

            if (activePlayers == null || activePlayers.Length == 0) return;

            foreach (Player player in activePlayers)
            {
                if (player == null || player.health == null) continue;

                isPerfect = true;
                if (player.playerID < ___playerStartHealthValues.Length &&
                    player.health.CurrentHealthValue < ___playerStartHealthValues[player.playerID])
                {
                    isPerfect = false;
                    break;
                }
            }

            if (isPerfect)
            {
                CheckHandler.CheckLocation("Perfect a boss");
            }
        }
    }

    [HarmonyPatch(typeof(Gold), "OnTriggerStay2D")]
    public class GoldOnTriggerStay2DPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Player.goldWallet != null && Player.goldWallet.balance >= 1000)
            {
                CheckHandler.CheckLocation("Have 1000 gold");
            }
        }
    }

    [HarmonyPatch(typeof(Platinum), "OnTriggerStay2D")]
    public class PlatinumOnTriggerStay2DPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Player.platWallet != null && Player.platWallet.balance >= 500)
            {
                CheckHandler.CheckLocation("Have 500 chaos gems");
            }
        }
    }
}