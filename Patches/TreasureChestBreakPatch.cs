using HarmonyLib;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.Break))]
    public static class TreasureChestBreakPatch
    {
        public static bool IsOpeningBossChest { get; private set; }

        [HarmonyPrefix]
        public static void Prefix(TreasureChest __instance)
        {
            if (__instance == null) return;

            if (__instance.chestType == TreasureChestType.Boss)
            {
                IsOpeningBossChest = true;
            }

            if (!__instance.opened && !__instance.destroyed && __instance.dropLoot)
            {
                string chestTypeName = __instance.chestType.ToString();

                if (!chestTypeName.Contains("Boss") && !chestTypeName.Contains("MiniBoss") && !chestTypeName.Contains("Party"))
                {
                    chestTypeName = Level.element.ToString();
                }

                int currentCount = DataManager.IncrementChestCount(chestTypeName);
                if (currentCount > 0)
                {
                    CheckHandler.CheckLocation($"{chestTypeName} Chest Slot {currentCount}");
                    CheckHandler.CheckLocation($"Open {DataManager.TotalChestsOpened} Total Chests");
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
        public static bool Prefix() => !TreasureChestBreakPatch.IsOpeningBossChest;
    }
}