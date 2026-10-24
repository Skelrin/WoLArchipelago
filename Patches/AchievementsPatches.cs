using System.Collections.Generic;
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
    /// Patch to monitor boss fights to trigger the "Perfect boss" location check if no player takes damage.
    /// </summary>
        [HarmonyPatch(typeof(BossRoomEventHandler), nameof(BossRoomEventHandler.CheckForPerfectFight))]
    public static class BossRoomEventHandlerPatches
    {
        [HarmonyPostfix]
        public static void CheckForPerfectFightPostfix(BossRoomEventHandler __instance, int[] ___playerStartHealthValues)
        {
            if (PerfectFightHelper.IsPerfect(___playerStartHealthValues))
                CheckHandler.CheckLocation($"Perfect {__instance.bossID}");
        }
    }

    /// <summary>
    /// Patch to monitor mini boss fights to trigger the "Perfect miniboss" location check if no player takes damage.
    /// </summary>
    [HarmonyPatch(typeof(MiniBossActivationCircle), "Activate")]
    public static class MiniBossActivationPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => PerfectFightHelper.MiniBossStartHealth = PerfectFightHelper.Snapshot();
    }

    public static class PerfectFightHelper
    {
        public static readonly HashSet<string> MiniBosses =
        [
            "SuperArcher", "SuperCoffin", "SuperKnight", "SuperLancer",
            "SuperMage", "SuperRogue", "SuperSummoner"
        ];

        public static int[] MiniBossStartHealth;

        public static bool PlayerDied;

        public static void ResetFight()
        {
            PlayerDied = false;
            MiniBossStartHealth = null;
        }

        public static int[] Snapshot()
        {
            Player[] players = GameController.activePlayers;
            if (players == null) return null;

            int[] values = new int[players.Length];
            foreach (Player p in players)
            {
                if (p == null || p.health == null || p.playerID >= values.Length) continue;
                values[p.playerID] = p.health.CurrentHealthValue;
            }
            return values;
        }

        public static bool IsPerfect(int[] start)
        {
            if (PlayerDied) return false;

            Player[] players = GameController.activePlayers;
            if (start == null || start.Length == 0 || players == null || players.Length == 0) return false;

            bool hasValidPlayer = false;
            foreach (Player p in players)
            {
                if (p == null || p.health == null) continue;

                if (ItemHandler.IsPlayerDead(p)) return false;

                hasValidPlayer = true;
                if (p.playerID < start.Length && p.health.CurrentHealthValue < start[p.playerID])
                    return false;
            }
            return hasValidPlayer;
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
}