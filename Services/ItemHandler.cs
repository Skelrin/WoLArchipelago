using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;

namespace WoLArchipelago
{
    public static class ItemHandler
    {
        public static List<ItemInfo> CachedItems { get; set; } = new List<ItemInfo>();

        public static int GetTotalBossKeys()
        {
            int count = 0;
            foreach (var item in CachedItems)
            {
                if (APItemLocationDatabase.GetItemName(item.ItemId) == "Boss Key")
                {
                    count++;
                }
            }
            return count;
        }

        public static int GetTotalChaosFragment()
        {
            int count = 0;
            foreach (var item in CachedItems)
            {
                if (APItemLocationDatabase.GetItemName(item.ItemId) == "Chaos Fragment")
                {
                    count++;
                }
            }
            return count;
        }

        public static int GetTotalShopUpgrade()
        {
            int count = 0;
            foreach (var item in CachedItems)
            {
                if (APItemLocationDatabase.GetItemName(item.ItemId) == "Shop Upgrade")
                {
                    count++;
                }
            }
            return count;
        }

        public static bool CanAccessBossStage(int tierCount, int stageCount)
        {
            int totalKeys = GetTotalBossKeys();

            if (tierCount == 0 && stageCount == 2) return totalKeys >= 1;
            if (tierCount == 1 && stageCount == 2) return totalKeys >= 2;
            if (tierCount == 2 && stageCount == 2) return totalKeys >= 3;
            return true;
        }

        public static bool CanAccessFinalBossStage()
        {
            int totalChaosFragments = GetTotalChaosFragment();
            
            return totalChaosFragments >= APManager.ChaosFragmentsRequired;
        }

        public static void GrantPlayerAPItem(long apItemId)
        {
            string itemName = APItemLocationDatabase.GetItemName(apItemId);
            Plugin.Log.LogInfo($"[Archipelago] Processing Item: {itemName} (ID: {apItemId})");

            switch (itemName)
            {
                case "Boss Key":
                    GameUI.BroadcastNoticeMessage($"AP Item: Boss Key Received! ({GetTotalBossKeys()}/3)");
                    break;
                case "Chaos Fragment":
                    GameUI.BroadcastNoticeMessage($"AP Item: Chaos Fragment Received! ({GetTotalChaosFragment()}/{APManager.ChaosFragmentsRequired})");
                    break;
                case "Shop Upgrade":
                    GameUI.BroadcastNoticeMessage($"AP Item: Shop Upgrade Received! ({GetTotalShopUpgrade()}/3)");
                    break;
                case "Chaos Gems Pack":
                    Player.platWallet?.Deposit(100);
                    GameUI.BroadcastNoticeMessage("AP Item: +100 Gems!");
                    break;
                case "Gold Pack":
                    Player.goldWallet?.Deposit(200);
                    GameUI.BroadcastNoticeMessage("AP Item: +200 Gold!");
                    break;
                default:
                    GameUI.BroadcastNoticeMessage($"AP Item: {itemName}");
                    break;
            }
        }
    }
}