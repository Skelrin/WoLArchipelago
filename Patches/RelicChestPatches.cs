using HarmonyLib;
using System.Collections.Generic;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(RelicChestUI), "AssignPlayerItem")]
    public class RelicChestUIAssignPlayerItemPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result, 
            List<string> ___currentSelectionList, int ___currentStartIndex, 
            int ___currentSelectionIndex, Player ___currentPlayer)
        {
            string text = ___currentSelectionList[___currentStartIndex + ___currentSelectionIndex];
            
            if (Item.IsUnlocked(text))
            {
                int maxAllowedRelics = 1 + ItemHandler.GetItemCountByName("Relic Slot Upgrade");
                int currentRelicCount = ___currentPlayer.inventory.Count;

                if (currentRelicCount == 0) return true;

                if (currentRelicCount < maxAllowedRelics)
                {
                    ___currentPlayer.inventory.AddItem(text, showNotice: true);
                    
                    if (!DataManager.SavedHubRelics.Contains(text))
                    {
                        DataManager.SavedHubRelics.Add(text);
                        DataManager.SaveData();
                    }

                    SoundManager.PlayAudio("WoodPickUp");
                    __result = true; 
                    return false;
                }
                return true;
            }

            SoundManager.PlayAudio("MenuError");
            __result = false;
            return false;
        }
    }
}