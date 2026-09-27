using HarmonyLib;
using UnityEngine;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(ItemDrop), "OnInteract")]
    public class ItemDrop_OnInteract_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ItemDrop __instance, Player player)
        {
            if (player == null || player.inventory.ContainsItem(__instance.itemID))
            {
                return true;
            }

            int maxAllowedRelics = 1 + ItemHandler.GetItemCountByName("Relic Slot Upgrade");

            if (player.inventory.Count >= maxAllowedRelics)
            {
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
                }
                else
                {
                    SoundManager.PlayErrorAudio();
                    return false; 
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ItemDrop), "OnTriggerStay2D")]
    public class ItemDrop_OnTriggerStay2D_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ItemDrop __instance, Collider2D col, bool ___noPickUpMode)
        {
            if (col.gameObject.tag != "Player" || ___noPickUpMode)
            {
                return true;
            }

            Player component = col.transform.parent.GetComponent<Player>();

            if (component == null || component.inventory.ContainsItem(__instance.itemID) || __instance.requireInteract)
            {
                return true;
            }

            int maxAllowedRelics = 1 + ItemHandler.GetItemCountByName("Relic Slot Upgrade");

            if (component.inventory.Count >= maxAllowedRelics)
            {
                string itemToDrop = null;
                foreach (var kvp in component.inventory.itemDict)
                {
                    if (kvp.Value != null && !kvp.Value.isCursed)
                    {
                        itemToDrop = kvp.Key;
                        break;
                    }
                }

                if (itemToDrop != null)
                {
                    if (component.inventory.RemoveItem(itemToDrop))
                    {
                        LootManager.DropItem(component.transform.position, 1, itemToDrop, false, 0);
                    }
                }
                else
                {
                    return false; 
                }
            }

            return true;
        }
    }
}