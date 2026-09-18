using UnityEngine;

namespace WoLArchipelago
{
    public class DebugController : MonoBehaviour
    {
        public static bool BypassBoss { get; private set; } = false;
        public static bool GodModeActive { get; private set; } = false;

        public static bool CanEnterNextBossStage(int tierCount, int stageCount)
        {
            return BypassBoss || ItemHandler.CanAccessBossStage(tierCount, stageCount);
        }

        private void Update()
        {
            // F1: Allow Access to Boss
            if (Input.GetKeyDown(KeyCode.F1))
            {
                BypassBoss = !BypassBoss;
                GameUI.BroadcastNoticeMessage($"Boss Access (x-2): {(BypassBoss ? "ALLOWED" : "BLOCKED")}");
            }

            // F2: TP to Boss
            if (Input.GetKeyDown(KeyCode.F2))
            {
                TeleportToAltar();
            }

            // F3: Toggle Menu AP
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ArchipelagoUI.Instance?.Toggle();
            }

            // F4: Godmode
            if (Input.GetKeyDown(KeyCode.F4))
            {
                ToggleGodMode();
            }
        }

        private void ToggleGodMode()
        {
            GodModeActive = !GodModeActive;
            GameUI.BroadcastNoticeMessage($"God Mode: {(GodModeActive ? "ENABLED" : "DISABLED")}");

            Player player = FindObjectOfType<Player>();
            if (player != null && player.health != null)
            {
                if (GodModeActive)
                {
                    player.health.healthStat.AddMod(new NumVarStatMod("GodModeHP", 99999f, 10, VarStatModType.Override, fillToNewMax: true));
                    Player.goldWallet?.Deposit(9999);
                    Player.platWallet?.Deposit(999);
                }
                else
                {
                    player.health.healthStat.RemoveMod("GodModeHP");
                    if (player.health.healthStat.CurrentValue > player.health.healthStat.ModifiedValue)
                    {
                        player.health.healthStat.CurrentValue = player.health.healthStat.ModifiedValue;
                    }
                }
            }
        }

        private void TeleportToAltar()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return;

            MiniBossActivationCircle altar = FindObjectOfType<MiniBossActivationCircle>();
            if (altar != null)
            {
                player.transform.position = altar.transform.position;
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            }
        }
    }
}