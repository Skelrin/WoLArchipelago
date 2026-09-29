using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to handle player loading into the game to assign correct starting skills or resync its HP based on Max HP Boost received.
    /// </summary>
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