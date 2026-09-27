using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public class PlayerInitPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance == null) return;
            
            Services.StartingInventoryHandler.ApplyArchipelagoStartingSkills();
            Services.ItemHandler.SyncPlayerMaxHP(__instance, true);
        }
    }
}