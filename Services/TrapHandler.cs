using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WoLArchipelago.Services
{
    /// <summary>
    /// Handle Archipelago traps sent to the player. Queue them if multiple are sent at the same time.
    /// </summary>
    public static class TrapHandler
    {
        private const int CountdownSeconds = 5;
        private const int CursedDamage = 50;
        private const float MobSpawnRadius = 1.5f;
        private const float MinCooldown = 10f;
        private const float MaxCooldown = 60f;

        private static float _nextTrapTime;

        private static readonly WaitForSeconds Wait1 = new(1f);
        private static readonly WaitForSeconds Wait2 = new(2f);

        private static readonly Dictionary<string, Action<Player>> Traps = new()
        {
            ["Cursed Trap"] = CursedTrap,
            ["Mob Trap"] = MobTrap,
            ["Heal Trap"] = HealTrap,
            ["Gold Trap"] = GoldTrap,
        };

        private static readonly Queue<Action<Player>> Pending = new();
        private static bool _running;

        public static void ApplyTrapOnPlayer(string trapType)
        {
            if (!Traps.TryGetValue(trapType, out Action<Player> effect))
            {
                Plugin.Log.LogWarning($"[AP] Unknown trap: {trapType}");
                return;
            }

            Pending.Enqueue(effect);
            if (!_running)
            {
                Plugin.Instance.StartCoroutine(RunQueue());
            }
        }

        private static IEnumerator RunQueue()
        {
            _running = true;

            Player player = null;
            bool isReady() => GameController.inGameScene && (player = ItemHandler.GetActivePlayer(true)) != null;
            var playerReady = new WaitUntil(isReady);
            var canStart = new WaitUntil(() => Time.time >= _nextTrapTime && isReady());

            while (Pending.Count > 0)
            {
                Action<Player> effect = Pending.Dequeue();

                yield return canStart;
                yield return Countdown();
                yield return playerReady;

                try { effect(player); }
                catch (Exception e) { Plugin.Log.LogError($"[AP] Trap failed: {e}"); }

                _nextTrapTime = Time.time + Random.Range(MinCooldown, MaxCooldown);
            }

            _running = false;
        }

        private static IEnumerator Countdown()
        {
            GameUI.BroadcastNoticeMessage("You've been caught in a trap!", 2f);
            SoundManager.PlayAudio("BuyCursedRelic");
            yield return Wait2;

            for (int i = CountdownSeconds; i > 0; i--)
            {
                GameUI.BroadcastNoticeMessage($"Trap triggering in {i}...");
                yield return Wait1;
            }
        }

        private static void CursedTrap(Player player)
        {
            List<string> pool = ItemHandler.CursedItemIDs;
            if (pool.Count > 0)
            {
                player.inventory.AddItem(pool[Random.Range(0, pool.Count)], showNotice: true);
            }

            if (player.health == null) return;
            
            player.health.CurrentHealthValue = Mathf.Max(1, player.health.CurrentHealthValue - CursedDamage);
        }

        private static void MobTrap(Player player)
        {
            string[] enemies = ItemHandler.EnemyNames;
            int mobCount = Random.Range(3, 9);

            for (int i = 0; i < mobCount; i++)
            {
                Vector3 pos = player.transform.position + (Vector3)(Random.insideUnitCircle * MobSpawnRadius);
                DebugMenu.SpawnEnemy(enemies[Random.Range(0, enemies.Length)], pos);
            }
        }

        private static void HealTrap(Player player)
        {
            if (player.health == null) return;

            player.health.CurrentHealthValue = Mathf.Max(1, player.health.CurrentHealthValue / 2);
            GameUI.BroadcastNoticeMessage("Oops what happened to your health ?", 2f);
        }

        private static void GoldTrap(Player player)
        {
            if (Player.goldWallet != null)
            {
                Player.goldWallet.balance = 0;
            }
            GameUI.BroadcastNoticeMessage("Oops what happened to your gold ?", 2f);
        }
    }
}