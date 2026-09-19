using HarmonyLib;
using UnityEngine;

namespace WoLArchipelago
{
    [HarmonyPatch(typeof(NextLevelLoader), nameof(NextLevelLoader.OnTriggerStay2D))]
    public static class NextLevelLoaderTriggerPatch
    {
        private static float lastMessageTime = 0f;
        private static readonly AccessTools.FieldRef<NextLevelLoader, Player> playerRef = AccessTools.FieldRefAccess<NextLevelLoader, Player>("player");
        private static readonly AccessTools.FieldRef<NextLevelLoader, bool> portalEnteredRef = AccessTools.FieldRefAccess<NextLevelLoader, bool>("portalEntered");

        [HarmonyPrefix]
        public static bool Prefix(NextLevelLoader __instance, Collider2D col)
        {
            int currentTier = GameController.tierCount;
            int currentStage = GameController.stageCount;

            if (currentStage == 2)
            {
                if (!(DebugController.BypassBoss || ItemHandler.CanAccessBossStage(currentTier, currentStage)))
                {
                    Player player = Player.CheckForPlayer(col);
                    if (player != null && player.IsAvailable && player.inputDevice.GetButtonDown("Interact"))
                    {
                        if (Time.time - lastMessageTime > 1.0f)
                        {
                            int keysNeeded = currentTier == 0 ? 1 : (currentTier == 1 ? 2 : 3);
                            GameUI.BroadcastNoticeMessage($"Boss locked! Requires {keysNeeded} Key(s) (Have: {ItemHandler.GetTotalBossKeys()}/3)");
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
                if (!(DebugController.BypassBoss || ItemHandler.CanAccessFinalBossStage()))
                {
                    Player player = Player.CheckForPlayer(col);
                    if (player != null && player.IsAvailable && player.inputDevice.GetButtonDown("Interact"))
                    {
                        if (Time.time - lastMessageTime > 1.0f)
                        {
                            GameUI.BroadcastNoticeMessage($"Final Boss locked! Requires {APManager.ChaosFragmentsRequired} Chaos Fragment(s) (Have: {ItemHandler.GetTotalChaosFragment()}/{APManager.ChaosFragmentsRequired})");
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
    public static class SaveOnLevelChangePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            Patches.DashTrackerPatch.SaveDashesToDisk();
        }
    }
}