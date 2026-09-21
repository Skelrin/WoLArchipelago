using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using UnityEngine;

namespace WoLArchipelago.Services
{
    public class ItemHandler
    {
        public static List<ItemInfo> CachedItems { get; set; } = new List<ItemInfo>();

        private static List<string> _cachedCursedItemIDs;
        private static string[] _cachedEnemyNames;

        private static List<string> CursedItemIDs
        {
            get
            {
                if (_cachedCursedItemIDs == null || _cachedCursedItemIDs.Count == 0)
                {
                    _cachedCursedItemIDs = new List<string>();
                    if (LootManager.completeItemDict != null)
                    {
                        foreach (var kvp in LootManager.completeItemDict)
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

        private static Player GetActivePlayer(bool mustBeAlive = false)
        {
            var players = GameController.activePlayers;
            if (players == null) return null;

            for (int i = 0; i < players.Count(); i++)
            {
                var p = players[i];
                if (p == null) continue;
                if (mustBeAlive && p.fsm != null && p.fsm.currentStateName.Contains("Dead")) continue;
                return p;
            }
            return null;
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

        public static int GetTotalBossKeys() => GetItemCountByName("Boss Key");
        public static int GetTotalChaosFragment() => GetItemCountByName("Chaos Fragment");
        public static int GetTotalShopUpgrade() => GetItemCountByName("Shop Upgrade");

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

        public static void SyncPlayerMaxHP(Player player, bool restoreAllHP)
        {
            if (player?.health == null) return;

            int hpBoostCount = GetItemCountByName("Max HP Boost");
            int totalBonusHP = hpBoostCount * 50;

            NumVarStatMod hpMod = new NumVarStatMod("AP_MaxHP_Mod", totalBonusHP, fillToNewMax: restoreAllHP);
            player.health.healthStat.AddMod(hpMod);
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
            Player player = GetActivePlayer();

            if (player == null || !GameController.inGameScene)
            {
                return;
            }

            if (UnityEngine.Random.value < 0.5f && !player.inventory.IsFull)
            {
                var cursedPool = CursedItemIDs;
                if (cursedPool.Count > 0)
                {
                    string randomCursedID = cursedPool[UnityEngine.Random.Range(0, cursedPool.Count)];
                    player.inventory.AddItem(randomCursedID, showNotice: true);

                    if (player.health != null)
                    {
                        int targetHealth = Mathf.Max(1, player.health.CurrentHealthValue - 100);
                        player.health.CurrentHealthValue = targetHealth;
                    }
                    return;
                }
            }

            SpawnRandomEnemy(player.transform.position);
        }

        private static void SpawnRandomEnemy(Vector3 position)
        {
            var enemies = EnemyNames;
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
                Plugin.Log.LogError($"[AP] No relics found for tier {relicTier}.");
                return;
            }

            List<string> availableRelics = new List<string>();
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
                GameUI.BroadcastNoticeMessage($"All relics in tier {relicTier} are already unlocked!");
                return;
            }

            string selectedRelic = availableRelics[UnityEngine.Random.Range(0, availableRelics.Count)];

            Item.IsUnlocked(selectedRelic, true);
            GameUI.BroadcastNoticeMessage($"Unlocked {TextManager.GetItemName($"{selectedRelic}")} relic");

            Player player = GetActivePlayer(mustBeAlive: true);

            if (player != null && GameController.inGameScene)
            {
                LootManager.DropItem(player.transform.position, 1, selectedRelic, false, 0);
            }
        }

        private static bool IsSkillUnlocked(Player player, string skillName)
        {
            if (player?.skillStates == null) return false;

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
            Player player = GetActivePlayer(mustBeAlive: true);

            if (!LootManager.skillTierDict.TryGetValue(skillTier - 1, out List<string> arcanaPool) || arcanaPool == null)
            {
                Plugin.Log.LogError($"[AP] No arcana found for tier {skillTier}.");
                return;
            }

            List<string> availableArcanas = new List<string>();
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
                GameUI.BroadcastNoticeMessage($"All arcanas in tier {skillTier} are already unlocked!");
                return;
            }

            string selectedArcana = availableArcanas[UnityEngine.Random.Range(0, availableArcanas.Count)];

            player.HandleSkillUnlock(selectedArcana,true);

            if (player != null && GameController.inGameScene)
            {
                ItemSpawner itemSpawner = GameController.itemSpawner ?? ItemSpawner.Instance;
                if (itemSpawner != null)
                {
                    itemSpawner.SpawnItem(ItemSpawner.PoolType.SkillDrop, player.transform.position, randomizeSpawnLocation: true, 1.5f, selectedArcana);
                    SoundManager.PlayAudio("DropSpell");
                }
            }
        }

        public static void GrantPlayerAPItem(long apItemId)
        {
            string itemName = APItemLocationDatabase.GetItemName(apItemId);
            Plugin.Log.LogInfo($"[AP] Processing Item: {itemName} (ID: {apItemId})");

            switch (itemName)
            {
                case "Boss Key":
                    GameUI.BroadcastNoticeMessage($"[AP] Boss Key Received! ({GetTotalBossKeys()}/3)");
                    return;
                case "Chaos Fragment":
                    GameUI.BroadcastNoticeMessage($"[AP] Chaos Fragment Received! ({GetTotalChaosFragment()}/{APManager.ChaosFragmentsRequired})");
                    return;
                case "Shop Upgrade":
                    GameUI.BroadcastNoticeMessage($"[AP] Shop Upgrade Received! ({GetTotalShopUpgrade()}/3)");
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
                GameUI.BroadcastNoticeMessage($"[AP] Unlocked {TextManager.GetOutfitName(outfit)} outfit");
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
                Item.IsUnlocked(itemName, true);
                GameUI.BroadcastNoticeMessage($"[AP] {TextManager.GetItemName(itemName)} Received");
            }
            else
            {
                GameUI.BroadcastNoticeMessage($"[AP] {itemName} Received");
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