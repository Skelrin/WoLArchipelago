using HarmonyLib;
using System.Collections.Generic;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to handle assigning multiple relics to the player on the plaza based on its relic slot upgrade.
    /// </summary>
    [HarmonyPatch(typeof(RelicChestUI), "AssignPlayerItem")]
    public class RelicChestUIAssignPlayerItemPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result,
            List<string> ___currentSelectionList, int ___currentStartIndex,
            int ___currentSelectionIndex, Player ___currentPlayer)
        {
            string text = ___currentSelectionList[___currentStartIndex + ___currentSelectionIndex];

            if (!Item.IsUnlocked(text))
            {
                SoundManager.PlayAudio("MenuError");
                __result = false;
                return false;
            }

            int maxAllowedRelics = 1 + ItemHandler.GetItemCountByName("Relic Slot Upgrade");
            Inventory inv = ___currentPlayer.inventory;

            if (inv.Count == 0) return true; // vanilla

            if (inv.Count < maxAllowedRelics || ItemDropPatches.IsItemGroupable(___currentPlayer, text))
            {
                inv.AddItem(text, showNotice: true);
                ___currentPlayer.designatedItemID = text;
                SaveHubRelic(text);

                SoundManager.PlayAudio("WoodPickUp");
                __result = true;
                return false;
            }

            string toRemove = null;
            foreach (var kvp in inv.itemDict)
            {
                if (kvp.Value != null && !kvp.Value.isCursed)
                {
                    toRemove = kvp.Key;
                    break;
                }
            }

            if (toRemove == null)
            {
                SoundManager.PlayAudio("MenuError");
                __result = false;
                return false;
            }

            if (inv.GetItem(toRemove) is GroupItem group)
            {
                foreach (string memberID in group.itemInven.GetItemIDList())
                    DataManager.SavedHubRelics.Remove(memberID);
            }
            else
            {
                DataManager.SavedHubRelics.Remove(toRemove);
            }

            inv.RemoveItem(toRemove);
            inv.AddItem(text, showNotice: true);
            ___currentPlayer.designatedItemID = text;
            SaveHubRelic(text);

            SoundManager.PlayAudio("WoodPickUp");
            __result = true;
            return false;
        }

        private static void SaveHubRelic(string relicID)
        {
            if (!DataManager.SavedHubRelics.Contains(relicID))
            {
                DataManager.SavedHubRelics.Add(relicID);
            }
            DataManager.SaveData();
        }
    }
}