using HarmonyLib;
using UnityEngine;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches for managing level transitions, stage progression gating, and level generation order.
    /// </summary>
    public static class LevelPatches
    {
        /// <summary>
        /// Patch to enforce Archipelago boss keys and chaos fragments requirements before stage transitions.
        /// </summary>
        [HarmonyPatch(typeof(NextLevelLoader), nameof(NextLevelLoader.OnTriggerStay2D))]
        public static class NextLevelLoaderPatches
        {
            private const float MessageCooldown = 1.0f;
            private static float lastMessageTime;

            private static readonly AccessTools.FieldRef<NextLevelLoader, Player> playerRef = 
                AccessTools.FieldRefAccess<NextLevelLoader, Player>("player");
            private static readonly AccessTools.FieldRef<NextLevelLoader, bool> portalEnteredRef = 
                AccessTools.FieldRefAccess<NextLevelLoader, bool>("portalEntered");

            private static readonly string[] CouncilLevels = ["First", "Second", "Third"];

            [HarmonyPrefix]
            public static bool OnTriggerStay2DPrefix(NextLevelLoader __instance, Collider2D col)
            {   
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
                if (stage == 2)
                    return !ItemHandler.CanAccessBossStage(tier, stage);

                if (tier == 2 && stage == 3)
                    return !ItemHandler.CanAccessFinalBossStage();

                return false;
            }

            /// <summary>
            /// Displays an in-game message specifying the missing keys or fragments required for entry.
            /// </summary>
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

            /// <summary>
            /// Sends Archipelago location check completion events based on current stage clearance.
            /// </summary>
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

        /// <summary>
        /// Patch to handle save data persistence and custom biome order.
        /// </summary>
        [HarmonyPatch(typeof(GameController), nameof(GameController.LoadLevel))]
        public static class LoadLevelPatches
        {
            [HarmonyPrefix]
            public static void LoadLevelPrefix(ref string givenLevelName)
            {
                if (givenLevelName == "Hub" || givenLevelName == "PlayerRoom" || 
                    givenLevelName == "TitleScreen" || givenLevelName == "InitialLoad" || 
                    givenLevelName == "Tutorial" || givenLevelName == "Credits")
                {
                    BiomesHandler.ResetRun();
                }
                else if (BiomesHandler.AllBiomes.Contains(givenLevelName))
                {
                    BiomesHandler.OrganizeLevelList();
                    
                    givenLevelName = GameController.levelNameList[GameController.tierCount];
                }
                // We save data when changing level in case of game crash
                DataManager.SaveData();
            }
        }

        /// <summary>
        /// Patch to disable standard level shuffling, keeping stage sequence managed by Archipelago logic.
        /// </summary>
        [HarmonyPatch(typeof(GameController))]
        public static class DisableNextLevelShufflePatches
        {
            [HarmonyPatch(nameof(GameController.NextLevelName), MethodType.Setter)]
            [HarmonyPrefix]
            public static bool NextLevelNameSetterPrefix()
            {
                if (GameController.loadingScreen != null && GameController.loadingScreen.gameProgressBoard != null)
                {
                    GameController.loadingScreen.gameProgressBoard.InitializeTierOrders();
                }

                return false;
            }
        }
    }
}