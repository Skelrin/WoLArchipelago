using HarmonyLib;
using UnityEngine;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(NextLevelLoader), nameof(NextLevelLoader.OnTriggerStay2D))]
    public class NextLevelLoaderTriggerPatch
    {
        private static float lastMessageTime = 0f;
        private static readonly AccessTools.FieldRef<NextLevelLoader, Player> playerRef = AccessTools.FieldRefAccess<NextLevelLoader, Player>("player");
        private static readonly AccessTools.FieldRef<NextLevelLoader, bool> portalEnteredRef = AccessTools.FieldRefAccess<NextLevelLoader, bool>("portalEntered");

        [HarmonyPrefix]
        public static bool Prefix(NextLevelLoader __instance, Collider2D col)
        {
            int currentTier = GameController.tierCount;
            int currentStage = GameController.stageCount;

            if (DebugController.tpLastBoss)
            {
                GameController.tierCount = 2;
                GameController.stageCount = 2;
            }

            if (currentStage == 2)
            {
                if (!(DebugController.BypassBoss || Services.ItemHandler.CanAccessBossStage(currentTier, currentStage)))
                {
                    Player player = Player.CheckForPlayer(col);
                    if (player != null && player.IsAvailable && player.inputDevice.GetButtonDown("Interact"))
                    {
                        if (Time.time - lastMessageTime > 1.0f)
                        {
                            int keysNeeded = currentTier == 0 ? 1 : (currentTier == 1 ? 2 : 3);
                            GameUI.BroadcastNoticeMessage($"Boss locked! Requires {keysNeeded} Key(s) (Have: {Services.ItemHandler.GetTotalBossKeys()}/3)");
                            SoundManager.PlayAudio("MenuError");
                            lastMessageTime = Time.time;
                        }
                        playerRef(__instance) = null;
                        portalEnteredRef(__instance) = false;
                    }
                    return false;
                }
            }
            else if (currentTier == 2 && currentStage == 3)
            {
                if (!(DebugController.BypassBoss || Services.ItemHandler.CanAccessFinalBossStage()))
                {
                    Player player = Player.CheckForPlayer(col);
                    if (player != null && player.IsAvailable && player.inputDevice.GetButtonDown("Interact"))
                    {
                        if (Time.time - lastMessageTime > 1.0f)
                        {
                            GameUI.BroadcastNoticeMessage($"Final Boss locked! Requires {APManager.ChaosFragmentsRequired} Chaos Fragment(s) (Have: {Services.ItemHandler.GetTotalChaosFragment()}/{APManager.ChaosFragmentsRequired})");
                            SoundManager.PlayAudio("MenuError");
                            lastMessageTime = Time.time;
                        }
                        playerRef(__instance) = null;
                        portalEnteredRef(__instance) = false;
                    }
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GameController), "LoadLevel")]
    public class SaveOnLevelChangePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            Services.StatsManager.SaveStats();
        }
    }
}