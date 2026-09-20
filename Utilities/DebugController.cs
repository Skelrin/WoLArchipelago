using UnityEngine;

namespace WoLArchipelago
{
    public class DebugController : MonoBehaviour
    {
        public static bool BypassBoss { get; private set; } = false;

        public static bool tpLastBoss { get; private set; } = false;
        public static bool GodModeActive { get; private set; } = false;

        public bool CanEnterNextBossStage(int tierCount, int stageCount)
        {
            return BypassBoss || Services.ItemHandler.CanAccessBossStage(tierCount, stageCount);
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

            // F5: TP to Npc
            if (Input.GetKeyDown(KeyCode.F5))
            {
                TeleportToDoctorNpc();
                TeleportToCremireNpc();
                TeleportToTailorNpc();
                TeleportToBankerNpc();
                TeleportToShufflerNpc();
            }

            // F6: Unlock tokens
            if (Input.GetKeyDown(KeyCode.F6))
            {
                UnlockTokens();
            }

            // F8: Tp Stage 2-3
            if (Input.GetKeyDown(KeyCode.F8))
            {
                tpLastBoss = !tpLastBoss;
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

        private void TeleportToDoctorNpc()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return;

            DoctorNpc doctor = FindObjectOfType<DoctorNpc>();
            if (doctor != null)
            {
                player.transform.position = doctor.transform.position;
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            }
        }

        private void TeleportToCremireNpc()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return;

            CollectorNpc doctor = FindObjectOfType<CollectorNpc>();
            if (doctor != null)
            {
                player.transform.position = doctor.transform.position;
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            }
        }

        private void TeleportToTailorNpc()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return;

            TailorNpc doctor = FindObjectOfType<TailorNpc>();
            if (doctor != null)
            {
                player.transform.position = doctor.transform.position;
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            }
        }

        private void TeleportToBankerNpc()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return;

            BankerNpc doctor = FindObjectOfType<BankerNpc>();
            if (doctor != null)
            {
                player.transform.position = doctor.transform.position;
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            }
        }

        private void TeleportToShufflerNpc()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return;

            ShufflerNpc doctor = FindObjectOfType<ShufflerNpc>();
            if (doctor != null)
            {
                player.transform.position = doctor.transform.position;
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            }
        }

        private void UnlockTokens()
        {
            Item.IsUnlocked("TokenCollector", setUnlocked: true);
            Item.IsUnlocked("TokenShuffler", setUnlocked: true);
            Item.IsUnlocked("TokenDoctor", setUnlocked: true);
            Item.IsUnlocked("TokenTailor", setUnlocked: true);
            Item.IsUnlocked("TokenBanker", setUnlocked: true);
        }
    }
}