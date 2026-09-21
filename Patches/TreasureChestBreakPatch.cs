using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.Break))]
    public static class TreasureChestBreakPatch
    {
        public static bool IsOpeningBossChest { get; private set; }

        [HarmonyPrefix]
        public static void Prefix(TreasureChest __instance)
        {
            if (__instance.chestType == TreasureChestType.Boss)
            {
                IsOpeningBossChest = true;
            }

            if (__instance != null && !__instance.opened && !__instance.destroyed && __instance.dropLoot)
            {
                string chestTypeName = __instance.chestType.ToString();
                
                int currentCount = Services.StatsManager.IncrementChestCount(chestTypeName);
                if (currentCount <= 0) return;

                string locationName = chestTypeName + " Chest Slot " + currentCount;
                long locId = APItemLocationDatabase.GetLocationId(locationName);

                if (locId != -1)
                {
                    Plugin.AP.SendLocationCheck(locId);
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            IsOpeningBossChest = false;
        }
    }

    [HarmonyPatch(typeof(LootManager), nameof(LootManager.DropSkill))]
    public static class LootManagerDropSkillPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (TreasureChestBreakPatch.IsOpeningBossChest)
            {
                return false;
            }

            return true;
        }
    }
}