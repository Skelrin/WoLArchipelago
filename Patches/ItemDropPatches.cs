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
            if (player == null || itemDrop == null || player.inventory.ContainsItem(itemDrop.itemID))
            {
                return true;
            }

            int maxAllowedRelics = 1 + ItemHandler.GetItemCountByName("Relic Slot Upgrade");

            if (player.inventory.Count < maxAllowedRelics)
            {
                return true;
            }

            string itemToDrop = null;
            foreach (var kvp in player.inventory.itemDict)
            {
                if (kvp.Value != null && !kvp.Value.isCursed)
                {
                    itemToDrop = kvp.Key;
                    break;
                }
            }

            if (itemToDrop != null)
            {
                if (player.inventory.RemoveItem(itemToDrop))
                {
                    LootManager.DropItem(player.transform.position, 1, itemToDrop, false, 0);
                }
                return true;
            }

            if (playAudioOnError)
            {
                SoundManager.PlayErrorAudio();
            }

            return false;
        }
    }
}