using UnityEngine;

namespace WoLArchipelago
{
    public static class LocationHandler
    {

        public static void Initialize()
        {
            Boss.bossDeadEventHandlers += OnMainBossDefeated;
        }

        public static void Unsubscribe()
        {
            Boss.bossDeadEventHandlers -= OnMainBossDefeated;
        }

        private static void OnMainBossDefeated(Boss boss)
        {
            if (boss == null) return;
            string bossName = string.IsNullOrEmpty(boss.BossDisplayName) ? boss.name : boss.BossDisplayName;
            Plugin.Log.LogInfo($"[Archipelago Check] Main Boss Defeated: {bossName}");

            long locId = APItemLocationDatabase.GetLocationId($"{bossName} Defeated");
            if (locId != -1) Plugin.AP.SendLocationCheck(locId);
        }

        public static void OnMiniBossDefeated(MiniBoss miniboss)
        {
            if (miniboss == null) return;
            string mbName = miniboss.name.Replace("(Clone)", "").Trim();
            Plugin.Log.LogInfo($"[Archipelago Check] MiniBoss Defeated: {mbName}");

            long locId = APItemLocationDatabase.GetLocationId($"{mbName} Defeated");
            if (locId != -1) Plugin.AP.SendLocationCheck(locId);
        }

        public static void OnChestOpened(string chestCategory, string lootTable)
        {
            Plugin.Log.LogInfo($"[Archipelago Check] Chest Opened [{chestCategory}]");
            long locId = APItemLocationDatabase.GetLocationId("Standard Chest Slot 1");
            if (locId != -1) Plugin.AP.SendLocationCheck(locId);
        }
    }
}