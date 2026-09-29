using HarmonyLib;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Harmony patches for SkillStoreItem to replace Arcana shop items with Archipelago location checks.
    /// </summary>
    [HarmonyPatch(typeof(SkillStoreItem))]
    public class SkillStoreItemPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SkillStoreItem.Start))]
        public static void StartPostfix(SkillStoreItem __instance)
        {
            string SlotFormat = "Arcana Shop Slot {0}";

            if (__instance.isShufflerItem) return;

            if (!ShopService.TryGetNextLocation(SlotFormat, ShopService.GetMaxShopSlots(), out long locId, out string locName))
            {
                ShopService.DestroyShopItem(__instance.gameObject, __instance.priceMarker);
                return;
            }

            Traverse.Create(__instance).Field("initialized").SetValue(true);

            // On Plaza, setup arbitrary price of 20 Chaos gems
            int defaultCost = __instance.usePlatinumCost ? 20 : 125;
            __instance.costStat?.Initialize(defaultCost);

            __instance.priceMarker?.SetText(__instance.Cost.ToString());

            // Hide card sprite
            if (__instance.standardSR != null) __instance.standardSR.enabled = false;
            if (__instance.empoweredSR != null) __instance.empoweredSR.enabled = false;
            if (__instance.sigSR != null) __instance.sigSR.enabled = false;

            ShopService.SetupAPShopItem(
                __instance.gameObject,
                locName,
                locId,
                __instance.itemSpriteRenderer,
                (title, desc) =>
                {
                    if (__instance.itemText != null) __instance.itemText.text = title;
                    if (__instance.descText != null) __instance.descText.text = desc;
                }
            );
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(SkillStoreItem.Buy))]
        public static bool BuyPrefix(SkillStoreItem __instance)
        {
            if (__instance.isShufflerItem) return true;

            return ShopService.ProcessPurchase(
                __instance.gameObject,
                __instance.priceMarker,
                __instance.Cost,
                __instance.usePlatinumCost,
                onSuccess: () => __instance.parentNpc?.PlayDefaultEmote(),
                onFailure: () => {
                    SoundManager.PlayAudio("MenuError");
                    __instance.parentNpc?.PlayEmote(EmoteType.No);
                }
            );
        }
    }
}