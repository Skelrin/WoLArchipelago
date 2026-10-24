using HarmonyLib;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to detect when player successfully give a relic to Cremire the Collector to send an Archipelago check.
    /// </summary>
    [HarmonyPatch(typeof(CollectorNpc))]
    public class CollectorNpcPatches
    {
        private static RelicWell _well;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(CollectorNpc.Start))]
        public static void StartPostfix(RelicWell ___relicWell)
        {
            _well = ___relicWell;
            RefreshWell();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnRelicCollect")]
        public static void OnRelicCollectPostfix()
        {
            CheckHandler.SendNpcCheck("Cremire the Collector Slot {0}", 10);
        }

        /// <summary>
        /// Enable/Disable relic well if player unlocked TokenCollector
        /// </summary>
        public static void RefreshWell()
        {
            if (_well == null) return;

            bool unlocked = ItemHandler.GetItemCountByName("TokenCollector") > 0;
            if (_well.gameObject.activeSelf != unlocked)
                _well.gameObject.SetActive(unlocked);
        }
    }
}