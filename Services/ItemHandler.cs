using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using UnityEngine;

namespace WoLArchipelago.Services
{
    /// <summary>
    /// Handles receiving items from Archipelago multiworld and applying them in-game.
    /// </summary>
    public static class ItemHandler
    {
        public static List<ItemInfo> CachedItems { get; set; }

        private static List<string> _cachedCursedItemIDs;
        private static string[] _cachedEnemyNames;

        static ItemHandler()
        {
            CachedItems = [];
        }

        private static List<string> CursedItemIDs
        {
            get
            {
                if (_cachedCursedItemIDs == null || _cachedCursedItemIDs.Count == 0)
                {
                    _cachedCursedItemIDs = [];
                    if (LootManager.completeItemDict != null)
                    {
                        foreach (KeyValuePair<string, Item> kvp in LootManager.completeItemDict)
                        {
                            if (kvp.Value != null && kvp.Value.isCursed)
                            {
                                _cachedCursedItemIDs.Add(kvp.Key);
                            }
                        }
                    }
                }
                return _cachedCursedItemIDs;
            }
        }

        private static string[] EnemyNames
        {
            get
            {
                if (_cachedEnemyNames == null || _cachedEnemyNames.Length == 0)
                {
                    _cachedEnemyNames = Enum.GetNames(typeof(Enemy.EName));
                }
                return _cachedEnemyNames;
            }
        }

        public static Player GetActivePlayer(bool mustBeAlive)
        {
            var players = GameController.activePlayers;
            if (players == null) return null;

            for (int i = 0; i < players.Count(); i++)
            {
                Player p = players[i];
                if (p == null) continue;
                if (mustBeAlive && p.fsm != null && p.fsm.currentStateName.Contains("Dead")) continue;
                return p;
            }
            return null;
        }

        public static Player GetActivePlayer()
        {
            return GetActivePlayer(false);
        }

        public static bool IsItemUnlocked(long itemId)
        {
            return CachedItems != null && CachedItems.Exists(item => item.ItemId == itemId);
        }

        public static int GetItemCountByName(string name)
        {
            int count = 0;
            int total = CachedItems.Count;
            for (int i = 0; i < total; i++)
            {
                if (APItemLocationDatabase.GetItemName(CachedItems[i].ItemId) == name)
                {
                    count++;
                }
            }
            return count;
        }

        public static int GetTotalBossKeys() { return GetItemCountByName("Boss Key"); }
        public static int GetTotalChaosFragment() { return GetItemCountByName("Chaos Fragment"); }
        public static int GetTotalShopUpgrade() { return GetItemCountByName("Shop Upgrade"); }

        public static bool CanAccessBossStage(int tierCount, int stageCount)
        {
            if (stageCount == 2 && tierCount >= 0 && tierCount <= 2)
            {
                return GetTotalBossKeys() >= (tierCount + 1);
            }
            return true;
        }

        public static bool CanAccessFinalBossStage()
        {
            return GetTotalChaosFragment() >= APManager.ChaosFragmentsRequired;
        }

        /// <summary>
        /// Recalculates and applies Max HP stat modifiers based on received HP Boost items.
        /// </summary>
        public static void SyncPlayerMaxHP(Player player, bool restoreAllHP)
        {
            if (player?.health == null) return;

            int hpBoostCount = GetItemCountByName("Max HP Boost");
            int totalBonusHP = hpBoostCount * 50;

            NumVarStatMod hpMod = new("AP_MaxHP_Mod", totalBonusHP);
            player.health.healthStat.AddMod(hpMod);

            if (restoreAllHP)
            {
                player.health.CurrentHealthValue = Mathf.RoundToInt(player.health.healthStat.ModifiedValue);
            }
        }

        public static void ProcessHPBoostItem()
        {
            Player player = GetActivePlayer();
            if (player?.health != null)
            {
                float maxHpBefore = player.health.healthStat.ModifiedValue;

                SyncPlayerMaxHP(player, false);

                float maxHpAfter = player.health.healthStat.ModifiedValue;
                int actualHpGain = Mathf.RoundToInt(maxHpAfter - maxHpBefore);

                if (actualHpGain > 0)
                {
                    player.health.RestoreHealth(actualHpGain, showDisplayText: true, playSound: true);
                }

                GameUI.BroadcastNoticeMessage("Max HP Boost Received!");
            }
        }

        public static void TriggerCursedTrap()
        {
            Plugin.Instance.StartCoroutine(CursedTrapCoroutine());
        }

        private static IEnumerator CursedTrapCoroutine()
        {
            Player player = GetActivePlayer(true);
            if (player == null || !GameController.inGameScene)
            {
                yield break;
            }

            GameUI.BroadcastNoticeMessage("You've been caught in a trap!", 2f);
            SoundManager.PlayAudio("BuyCursedRelic");
            yield return new WaitForSeconds(2f);

            for (int i = 5; i > 0; i--)
            {
                GameUI.BroadcastNoticeMessage(string.Format("Trap triggering in {0}...", i));
                yield return new WaitForSeconds(1f);

                if (GetActivePlayer(true) == null || !GameController.inGameScene)
                {
                    yield break;
                }
            }

            player = GetActivePlayer(true);
            if (player == null) yield break;

            // 50% chance to force-add a cursed relic or spawns an enemy wave on failure/full inventory
            if (UnityEngine.Random.value < 0.5f && !player.inventory.IsFull)
            {
                List<string> cursedPool = CursedItemIDs;
                if (cursedPool.Count > 0)
                {
                    string randomCursedID = cursedPool[UnityEngine.Random.Range(0, cursedPool.Count)];
                    player.inventory.AddItem(randomCursedID, showNotice: true);

                    if (player.health != null)
                    {
                        int targetHealth = Mathf.Max(1, player.health.CurrentHealthValue - 100);
                        player.health.CurrentHealthValue = targetHealth;
                    }
                    yield break;
                }
            }

            SpawnRandomEnemy(player.transform.position);
        }

        private static void SpawnRandomEnemy(Vector3 position)
        {
            string[] enemies = EnemyNames;
            if (enemies.Length == 0) return;

            int mobCount = UnityEngine.Random.Range(3, 9);

            for (int i = 0; i < mobCount; i++)
            {
                string randomEnemy = enemies[UnityEngine.Random.Range(0, enemies.Length)];

                Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.5f;
                Vector3 spawnPosition = position + new Vector3(offset.x, 0f, offset.y);

                DebugMenu.SpawnEnemy(randomEnemy, spawnPosition);
            }
        }

        private static void GenerateRandomRelic(int relicTier)
        {
            if (!LootManager.itemTierDict.TryGetValue(relicTier - 1, out List<string> relicPool) || relicPool == null)
            {
                Plugin.Log.LogError(string.Format("[AP] No relics found for tier {0}.", relicTier));
                return;
            }

            List<string> availableRelics = [];
            for (int i = 0; i < relicPool.Count; i++)
            {
                string relic = relicPool[i];
                if (!Item.IsUnlocked(relic))
                {
                    availableRelics.Add(relic);
                }
            }

            if (availableRelics.Count == 0)
            {
                GameUI.BroadcastNoticeMessage(string.Format("All relics in tier {0} are already unlocked!", relicTier));
                return;
            }

            string selectedRelic = availableRelics[UnityEngine.Random.Range(0, availableRelics.Count)];

            Item.IsUnlocked(selectedRelic, true);
            GameUI.BroadcastNoticeMessage(string.Format("Unlocked {0} relic", TextManager.GetItemName(selectedRelic)));

            Player player = GetActivePlayer(true);
            if (player != null && GameController.inGameScene)
            {
                LootManager.DropItem(player.transform.position, 1, selectedRelic, false, 0);
                SoundManager.PlayAudio("DropItem");
            }
        }

        private static bool IsSkillUnlocked(Player player, string skillName)
        {
            if (player == null || player.skillStates == null) return false;

            for (int i = 0; i < player.skillStates.Length; i++)
            {
                var state = player.skillStates[i];
                if (state == null) continue;

                if (state.GetType().Name.IndexOf(skillName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return state.isUnlocked;
                }
            }

            return false;
        }

        private static void GenerateRandomSkill(int skillTier)
        {
            Player player = GetActivePlayer(true);
            if (player == null) return;

            if (!LootManager.skillTierDict.TryGetValue(skillTier - 1, out List<string> arcanaPool) || arcanaPool == null)
            {
                Plugin.Log.LogError(string.Format("[AP] No arcana found for tier {0}.", skillTier));
                return;
            }

            List<string> availableArcanas = [];
            for (int i = 0; i < arcanaPool.Count; i++)
            {
                string arcana = arcanaPool[i];
                if (!IsSkillUnlocked(player, arcana))
                {
                    availableArcanas.Add(arcana);
                }
            }

            if (availableArcanas.Count == 0)
            {
                GameUI.BroadcastNoticeMessage(string.Format("All arcanas in tier {0} are already unlocked!", skillTier));
                return;
            }

            string selectedArcana = availableArcanas[UnityEngine.Random.Range(0, availableArcanas.Count)];

            player.HandleSkillUnlock(selectedArcana, true);

            if (GameController.inGameScene)
            {
                ItemSpawner itemSpawner = GameController.itemSpawner ?? ItemSpawner.Instance;
                if (itemSpawner != null)
                {
                    itemSpawner.SpawnItem(ItemSpawner.PoolType.SkillDrop, player.transform.position, true, 1.5f, selectedArcana);
                    SoundManager.PlayAudio("DropSpell");
                }
            }
        }

        /// <summary>
        /// Main entry point for dispatching incoming items.
        /// </summary>
        public static void GrantPlayerAPItem(long apItemId)
        {
            string itemName = APItemLocationDatabase.GetItemName(apItemId);
            Plugin.Log.LogInfo(string.Format("[AP] Processing Item: {0} (ID: {1})", itemName, apItemId));

            switch (itemName)
            {
                case "Boss Key":
                    GameUI.BroadcastNoticeMessage(string.Format("[AP] Boss Key Received! ({0}/3)", GetTotalBossKeys()));
                    return;
                case "Chaos Fragment":
                    GameUI.BroadcastNoticeMessage(string.Format("[AP] Chaos Fragment Received! ({0}/{1})", GetTotalChaosFragment(), APManager.ChaosFragmentsRequired));
                    return;
                case "Shop Upgrade":
                    GameUI.BroadcastNoticeMessage(string.Format("[AP] Shop Upgrade Received! ({0}/4)", GetTotalShopUpgrade()));
                    return;
                case "Chaos Gems Pack":
                    Player.platWallet?.Deposit(100);
                    GameUI.BroadcastNoticeMessage("[AP] 100 Chaos Gems Received!");
                    return;
                case "Gold Pack":
                    Player.goldWallet?.Deposit(200);
                    GameUI.BroadcastNoticeMessage("[AP] 200 Gold Received!");
                    return;
                case "Max HP Boost":
                    ProcessHPBoostItem();
                    return;
                case "Cursed Trap":
                    GameUI.BroadcastNoticeMessage("[AP] Cursed Trap Received!");
                    TriggerCursedTrap();
                    return;
            }

            if (itemName.Contains("Outfit"))
            {
                int colonIndex = itemName.IndexOf(':');
                string outfit = colonIndex >= 0 ? itemName.Substring(colonIndex + 1).Trim() : itemName;
                Outfit.UnlockOutfit(outfit);
                GameUI.BroadcastNoticeMessage(string.Format("[AP] Unlocked {0} outfit", TextManager.GetOutfitName(outfit)));
            }
            else if (itemName.Contains("Relic") && TryExtractTier(itemName, out int relicTier))
            {
                GenerateRandomRelic(relicTier);
            }
            else if (itemName.Contains("Arcana") && !itemName.Contains("Bonus") && TryExtractTier(itemName, out int arcanaTier))
            {
                GenerateRandomSkill(arcanaTier);
            }
            else if (itemName.Contains("Doctor") || itemName.Contains("Heal"))
            {
                GameUI.BroadcastNoticeMessage(string.Format("[AP] {0} Received", TextManager.GetItemName(itemName)));
                Item.IsUnlocked(itemName, true);
                Player player = GetActivePlayer();
                if (player != null && GameController.inGameScene)
                {
                    LootManager.DropItem(player.transform.position, 1, itemName, false, 0);
                    SoundManager.PlayAudio("DropItem");
                }
            }
            else
            {
                GameUI.BroadcastNoticeMessage(string.Format("[AP] {0} Received", itemName));
                GameUI.RefreshCDUI();
            }
        }

        private static bool TryExtractTier(string itemName, out int tier)
        {
            int lastSpace = itemName.LastIndexOf(' ');
            if (lastSpace >= 0 && int.TryParse(itemName.Substring(lastSpace + 1), out tier))
            {
                return true;
            }
            tier = 0;
            return false;
        }
    }
}