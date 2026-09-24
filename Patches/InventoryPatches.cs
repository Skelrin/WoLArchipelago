using HarmonyLib;
using System.Linq;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(Inventory), "DropItem")]
    public class InventoryDropItemPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Inventory __instance, string givenItemID, ref bool __result)
        {
            string loadedLevelName = GameController.currentLevelName;
            
            if (loadedLevelName == "PlayerRoom" || loadedLevelName == "Hub")
            {
                string idToRemove = givenItemID;
                
                if (string.IsNullOrEmpty(idToRemove))
                {
                    idToRemove = __instance.GetItemIDList().FirstOrDefault();
                }

                if (!string.IsNullOrEmpty(idToRemove) && __instance.RemoveItem(idToRemove, forceOverride: true))
                {
                    Entity parentEntity = Traverse.Create(__instance).Field("parentEntity").GetValue<Entity>();
                    Player p = parentEntity as Player;

                    if (p != null)
                    {
                        if (p.designatedItemID == idToRemove)
                        {
                            string nextRelic = __instance.GetItemIDList().FirstOrDefault(id => id != idToRemove);
                            p.designatedItemID = nextRelic ?? string.Empty;
                        }
                    }

                    DataManager.SavedHubRelics.Remove(idToRemove);
                    DataManager.SaveData();

                    __result = true;
                }
                else
                {
                    __result = false;
                }
                
                return false;
            }
            
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "GiveDesignatedItem")]
    public class PlayerGiveDesignatedItemPatch
    {
        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            string loadedLevelName = GameController.currentLevelName;
            if (loadedLevelName == "PlayerRoom" || loadedLevelName == "Hub")
            {
                foreach (string relicId in DataManager.SavedHubRelics)
                {
                    if (relicId != __instance.designatedItemID && !__instance.inventory.ContainsItem(relicId))
                    {
                        __instance.inventory.AddItem(relicId, showNotice: false, ignoreMax: true);
                    }
                }
            }
        }
    }
}