using HarmonyLib;
using UnityEngine;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(NextLevelLoader), nameof(NextLevelLoader.OnTriggerStay2D))]
    public class NextLevelLoaderTriggerPatch
    {
        private const float MessageCooldown = 1.0f;
        private static float lastMessageTime;

        private static readonly AccessTools.FieldRef<NextLevelLoader, Player> playerRef = 
            AccessTools.FieldRefAccess<NextLevelLoader, Player>("player");
        private static readonly AccessTools.FieldRef<NextLevelLoader, bool> portalEnteredRef = 
            AccessTools.FieldRefAccess<NextLevelLoader, bool>("portalEntered");

        private static readonly string[] CouncilLevels = { "First", "Second", "Third" };

        [HarmonyPrefix]
        public static bool Prefix(NextLevelLoader __instance, Collider2D col)
        {   
            if (DebugController.tpLastBoss)
            {
                GameController.tierCount = 2;
                GameController.stageCount = 2;
            }

            int currentTier = GameController.tierCount;
            int currentStage = GameController.stageCount;

            if (IsStageLocked(currentTier, currentStage))
            {
                Player player = Player.CheckForPlayer(col);
                if (player != null && player.IsAvailable && player.inputDevice.GetButtonDown("Interact"))
                {
                    if (Time.time - lastMessageTime > MessageCooldown)
                    {
                        DisplayRequirements(currentTier, currentStage);
                        lastMessageTime = Time.time;
                    }
                    playerRef(__instance) = null;
                    portalEnteredRef(__instance) = false;
                }
                return false;
            }

            ProcessLocationCheck(currentTier, currentStage);

            return true;
        }

        private static bool IsStageLocked(int tier, int stage)
        {
            if (DebugController.BypassBoss) return false;

            if (stage == 2)
                return !ItemHandler.CanAccessBossStage(tier, stage);

            if (tier == 2 && stage == 3)
                return !ItemHandler.CanAccessFinalBossStage();

            return false;
        }

        private static void DisplayRequirements(int tier, int stage)
        {
            SoundManager.PlayAudio("MenuError");

            if (stage == 2)
            {
                int keysNeeded = tier + 1;
                int haveKeys = ItemHandler.GetTotalBossKeys();
                GameUI.BroadcastNoticeMessage($"Boss locked! Requires {keysNeeded} Key(s) (Have: {haveKeys}/3)");
            }
            else
            {
                int req = APManager.ChaosFragmentsRequired;
                int have = ItemHandler.GetTotalChaosFragment();
                GameUI.BroadcastNoticeMessage($"Final Boss locked! Requires {req} Chaos Fragment(s) (Have: {have}/{req})");
            }
        }

        private static void ProcessLocationCheck(int tier, int stage)
        {
            string candidateName = stage != 3
                ? $"Stage {tier + 1}-{stage} Cleared"
                : $"{GetCouncilLevelName(tier)} Council Member Defeated";

            CheckHandler.CheckLocation(candidateName);
        }

        private static string GetCouncilLevelName(int tier)
        {
            return (tier >= 0 && tier < CouncilLevels.Length) ? CouncilLevels[tier] : "Unknown";
        }
    }

    [HarmonyPatch(typeof(GameController), nameof(GameController.LoadLevel))]
    public class SaveOnLevelChangePatch
    {
        [HarmonyPrefix]
        public static void Prefix(string givenLevelName)
        {
            if (givenLevelName == "Hub" || givenLevelName == "PlayerRoom" || 
                givenLevelName == "TitleScreen" || givenLevelName == "InitialLoad" || 
                givenLevelName == "Tutorial" || givenLevelName == "Credits")
            {
                BiomesHandler.ResetRun();
            }
            DataManager.SaveData();
        }
    }
}