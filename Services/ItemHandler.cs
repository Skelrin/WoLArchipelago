using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WoLArchipelago.Services
{
    /// <summary>
    /// Handles receiving items from Archipelago multiworld and applying them in-game.
    /// </summary>
    public static class ItemHandler
    {
        private const int MaxBossKeys = 3;
        private const int MaxShopUpgrades = 4;
        private const int ChaosGemsPerPack = 100;
        private const int GoldPerPack = 200;
        private const int HPPerBoost = 50;
        private const int HealOrbCount = 3;
        private const int ItemsPerBundle = 2;
        private const string TokenPrefix = "Token";

        private static volatile List<ItemInfo> _cachedItems = [];
        private static volatile HashSet<long> _unlockedIds = [];

        private static List<string> _cachedCursedItemIDs;
        private static string[] _cachedEnemyNames;

        private static readonly Dictionary<string, int> NameCounts = [];
        private static List<ItemInfo> _countedList;
        
        public static List<ItemInfo> CachedItems
        {
            get => _cachedItems;
            set
            {
                List<ItemInfo> list = value ?? [];
                HashSet<long> ids = [];
                foreach (ItemInfo it in list)
                    if (it != null) ids.Add(it.ItemId);

                _unlockedIds = ids;
                _cachedItems = list;
            }
        }

        public static List<string> CursedItemIDs
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

        public static string[] EnemyNames => 
            _cachedEnemyNames is { Length: > 0 } ? _cachedEnemyNames : (_cachedEnemyNames = Enum.GetNames(typeof(Enemy.EName)));

        public static bool IsPlayerDead(Player player)
        {
            if (player == null) return true;
            if (player.health != null && player.health.CurrentHealthValue <= 0) return true;
            return player.fsm != null && player.fsm.currentStateName.Contains("Dead");
        }

        public static Player GetActivePlayer(bool mustBeAlive = false)
        {
            var players = GameController.activePlayers;
            if (players == null) return null;

            foreach (Player player in players)
            {
                if (player == null) continue;
                if (mustBeAlive && IsPlayerDead(player)) continue;
                return player;
            }
            return null;
        }

        public static bool IsItemUnlocked(long itemId) => _unlockedIds.Contains(itemId);

        public static int GetItemCountByName(string name)
        {
            List<ItemInfo> items = _cachedItems;
            if (items == null) return 0;

            if (!ReferenceEquals(_countedList, items))
            {
                NameCounts.Clear();
                foreach (ItemInfo item in items)
                {
                    if (item == null) continue;
                    string n = APItemLocationDatabase.GetItemName(item.ItemId);
                    if (n == null) continue;
                    NameCounts[n] = NameCounts.TryGetValue(n, out int c) ? c + 1 : 1;
                }
                _countedList = items;
            }

            return NameCounts.TryGetValue(name, out int count) ? count : 0;
        }

        public static int GetTotalBossKeys() => GetItemCountByName("Boss Key");
        public static int GetTotalChaosFragment() => GetItemCountByName("Chaos Fragment");
        public static int GetTotalShopUpgrade() => GetItemCountByName("Shop Upgrade");

        public static bool CanAccessBossStage(int tierCount, int stageCount)
        {
            if (stageCount == 2 && tierCount >= 0 && tierCount <= 2)
            {
                return GetTotalBossKeys() >= tierCount + 1;
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

            int totalBonusHP = GetItemCountByName("Max HP Boost") * HPPerBoost;
            player.health.healthStat.AddMod(new NumVarStatMod("AP_MaxHP_Mod", totalBonusHP));

            if (restoreAllHP)
            {
                player.health.CurrentHealthValue = Mathf.RoundToInt(player.health.healthStat.ModifiedValue);
            }
        }

        public static void ProcessHPBoostItem()
        {
            Player player = GetActivePlayer();
            if (player?.health == null) return;

            float maxHpBefore = player.health.healthStat.ModifiedValue;
            SyncPlayerMaxHP(player, false);
            int actualHpGain = Mathf.RoundToInt(player.health.healthStat.ModifiedValue - maxHpBefore);

            if (actualHpGain > 0)
            {
                player.health.RestoreHealth(actualHpGain, showDisplayText: true, playSound: true);
            }

            GameUI.BroadcastNoticeMessage("Max HP Boost Received!");
        }

        private static List<string> PickRandom(List<string> pool, int count)
        {
            int n = Math.Min(count, pool.Count);
            for (int i = 0; i < n; i++)
            {
                int j = Random.Range(i, pool.Count);
                string tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }
            return pool.GetRange(0, n);
        }

        private static bool IsNpcToken(string itemId) => itemId.StartsWith(TokenPrefix, StringComparison.Ordinal);

        /// <summary>
        /// Unlock 2 relics of a specific tier and drop them in player is inside the dungeon.
        /// </summary>
        private static void GenerateRandomRelic(int relicTier)
        {
            if (!LootManager.itemTierDict.TryGetValue(relicTier - 1, out List<string> pool) || pool == null)
            {
                Plugin.Log.LogError($"[AP] No relics found for tier {relicTier}.");
                return;
            }

            List<string> available = pool.FindAll(r => !IsNpcToken(r) && !Item.IsUnlocked(r));
            if (available.Count == 0)
            {
                GameUI.BroadcastNoticeMessage($"All relics in tier {relicTier} are already unlocked!");
                return;
            }

            List<string> picked = PickRandom(available, ItemsPerBundle);

            foreach (string relic in picked)
            {
                Item.IsUnlocked(relic, true);
            }

            string names = string.Join(", ", [.. picked.ConvertAll(TextManager.GetItemName)]);
            GameUI.BroadcastNoticeMessage($"Unlocked {names} relic{(picked.Count > 1 ? "s" : "")}",5f);

            Player player = GetActivePlayer(true);
            if (player != null && GameController.inGameScene)
            {
                foreach (string relic in picked)
                {
                    LootManager.DropItem(player.transform.position, 1, relic, false, 0);
                }
                SoundManager.PlayAudio("DropItem");
            }
        }

        /// <summary>
        /// Unlock 2 arcanas (signature variant as well if it's a standard arcana unlocked) of a specific tier
        /// and drop them in player is inside the dungeon.
        /// </summary>
        private static void GenerateRandomSkill(int skillTier)
        {
            Player player = GetActivePlayer(true);
            if (player == null) return;

            if (!LootManager.skillTierDict.TryGetValue(skillTier - 1, out List<string> pool) || pool == null)
            {
                Plugin.Log.LogError($"[AP] No arcana found for tier {skillTier}.");
                return;
            }

            List<string> available = pool.FindAll(a => !IsSkillUnlocked(player, a));
            if (available.Count == 0)
            {
                GameUI.BroadcastNoticeMessage($"All arcanas in tier {skillTier} are already unlocked!");
                return;
            }

            List<string> picked = PickRandom(available, ItemsPerBundle);

            foreach (string arcana in picked)
            {
                player.HandleSkillUnlock(arcana, true);
            }

            if (!GameController.inGameScene) return;

            ItemSpawner spawner = GameController.itemSpawner ?? ItemSpawner.Instance;
            if (spawner != null)
            {
                foreach (string arcana in picked)
                {
                    spawner.SpawnItem(ItemSpawner.PoolType.SkillDrop, player.transform.position, true, 1.5f, arcana);
                }
                SoundManager.PlayAudio("DropSpell");
            }
        }

        private static bool IsSkillUnlocked(Player player, string skillName)
        {
            if (player.skillStates == null) return false;

            foreach (var state in player.skillStates)
            {
                if (state != null && state.GetType().Name.IndexOf(skillName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return state.isUnlocked;
                }
            }
            return false;
        }

        /// <summary>
        /// Main entry point for dispatching incoming items.
        /// </summary>
        public static void GrantPlayerAPItem(long apItemId)
        {
            string itemName = APItemLocationDatabase.GetItemName(apItemId);
            if (itemName == null)
            {
                Plugin.Log.LogWarning($"[AP] Unknown item ID: {apItemId}");
                return;
            }

            Plugin.Log.LogInfo($"[AP] Processing Item: {itemName} (ID: {apItemId})");

            switch (itemName)
            {
                case "Boss Key":
                    GameUI.BroadcastNoticeMessage($"Boss Key Received! ({GetTotalBossKeys()}/{MaxBossKeys})");
                    return;
                case "Chaos Fragment":
                    GameUI.BroadcastNoticeMessage($"Chaos Fragment Received! ({GetTotalChaosFragment()}/{APManager.ChaosFragmentsRequired})");
                    return;
                case "Shop Upgrade":
                    GameUI.BroadcastNoticeMessage($"Shop Upgrade Received! ({GetTotalShopUpgrade()}/{MaxShopUpgrades})");
                    return;
                case "Chaos Gems Pack":
                    Player.platWallet?.Deposit(ChaosGemsPerPack);
                    GameUI.BroadcastNoticeMessage($"{ChaosGemsPerPack} Chaos Gems Received!");
                    return;
                case "Gold Pack":
                    Player.goldWallet?.Deposit(GoldPerPack);
                    GameUI.BroadcastNoticeMessage($"{GoldPerPack} Gold Received!");
                    return;
                case "Heal Orb":
                    Player healTarget = GetActivePlayer(true);
                    if (healTarget != null)
                    {
                        LootManager.DropHealth(healTarget.transform.position, HealOrbCount);
                    }
                    GameUI.BroadcastNoticeMessage("Healing Orbs Received!");
                    return;
                case "Max HP Boost":
                    ProcessHPBoostItem();
                    return;
            }

            if (itemName.Contains("Outfit"))
            {
                int colon = itemName.IndexOf(':');
                string outfit = colon >= 0 ? itemName.Substring(colon + 1).Trim() : itemName;
                Outfit.UnlockOutfit(outfit);
                GameUI.BroadcastNoticeMessage($"Unlocked {TextManager.GetOutfitName(outfit)} outfit");
            }
            else if (itemName.Contains("Token"))
            {
                Item.IsUnlocked(itemName, true);
                GameUI.BroadcastNoticeMessage($"Unlocked {TextManager.GetItemName(itemName)} relic");

                Player player = GetActivePlayer(true);
                if (player != null && GameController.inGameScene)
                {
                    LootManager.DropItem(player.transform.position, 1, itemName, false, 0);
                    SoundManager.PlayAudio("DropItem");
                }

                if (itemName == "TokenCollector")
                {
                    Patches.CollectorNpcPatches.RefreshWell();
                }
            }
            else if (itemName.Contains("Relic Bundle") && TryExtractTier(itemName, out int relicTier))
            {
                GenerateRandomRelic(relicTier);
            }
            else if (itemName.Contains("Arcana Bundle") && TryExtractTier(itemName, out int arcanaTier))
            {
                GenerateRandomSkill(arcanaTier);
            }
            else if (itemName.Contains("Trap"))
            {
                TrapHandler.ApplyTrapOnPlayer(itemName);
            }
            else
            {
                GameUI.BroadcastNoticeMessage($"{itemName} Received");
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