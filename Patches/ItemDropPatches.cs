using HarmonyLib;
using UnityEngine;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches for ItemDrop to handle picking up relic with relic slot upgrade logic.
    /// </summary>
    public static class ItemDropPatches
    {
        /// <summary>
        /// Intercepts manual interaction with item drops to handle item swapping when the inventory is full.
        /// </summary>
        [HarmonyPatch(typeof(ItemDrop), "OnInteract")]
        public static class ItemDropOnInteractPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ItemDrop __instance, Player player)
            {
                return TryMakeRoomForPickup(player, __instance, playAudioOnError: true);
            }
        }

        /// <summary>
        /// Intercepts collision-based automatic item pickups to handle item swapping when the inventory is full.
        /// </summary>
        [HarmonyPatch(typeof(ItemDrop), "OnTriggerStay2D")]
        public static class ItemDropOnTriggerStay2DPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ItemDrop __instance, Collider2D col, bool ___noPickUpMode)
            {
                if (col == null || col.gameObject.tag != "Player" || ___noPickUpMode)
                {
                    return true;
                }

                Player player = col.transform.parent != null 
                    ? col.transform.parent.GetComponent<Player>() 
                    : null;

                if (player == null || __instance.requireInteract)
                {
                    return true;
                }

                return TryMakeRoomForPickup(player, __instance, playAudioOnError: false);
            }
        }

        /// <summary>
        /// Checks player inventory capacity against relic slot upgrade unlocked and drops a non-cursed relic to make room for a new item.
        /// </summary>
        private static bool TryMakeRoomForPickup(Player player, ItemDrop itemDrop, bool playAudioOnError)
        {
            if (itemDrop == null) return true;

            bool canPickUp = TryMakeRoomForPickup(player, itemDrop.itemID);

            if (!canPickUp && playAudioOnError)
                SoundManager.PlayErrorAudio();

            return canPickUp;
        }

        public static bool TryMakeRoomForPickup(Player player, string itemID)
        {
            if (player == null || player.inventory.ContainsItem(itemID))
                return true;

            int maxAllowedRelics = 1 + ItemHandler.GetItemCountByName("Relic Slot Upgrade");

            if (player.inventory.Count < maxAllowedRelics || IsItemGroupable(player, itemID))
                return true;

            return TryDropNonCursedRelic(player);
        }

        /// <summary>
        /// Drops one non-cursed relic (standalone first, grouped as fallback) to free a slot.
        /// </summary>
        public static bool TryDropNonCursedRelic(Player player)
        {
            string itemToDrop = null;
            string groupToDrop = null;

            foreach (var kvp in player.inventory.itemDict)
            {
                if (kvp.Value == null || kvp.Value.isCursed)
                    continue;

                if (kvp.Value.isGroupItem)
                {
                    groupToDrop ??= kvp.Key;
                }
                else
                {
                    itemToDrop = kvp.Key;
                    break;
                }
            }

            itemToDrop ??= groupToDrop;
            return itemToDrop != null && player.inventory.DropItem(itemToDrop);
        }

        /// <summary>
        /// Checks if item can be grouped with other items from player inventory
        /// </summary>
        public static bool IsItemGroupable(Player player, string itemID)
        {
            return LootManager.completeItemDict.TryGetValue(itemID, out Item item) && player.inventory.CheckItemCombine(item);
        }
    }
}