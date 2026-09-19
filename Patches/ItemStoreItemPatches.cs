using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(ItemStoreItem))]
    public static class ItemStoreItemPatches
    {
        private static readonly string[] SlotFormats = { "Relic Shop Slot {0}" };

        public static void ClearSceneAssignments() => ShopService.ClearSceneAssignments();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemStoreItem.Start))]
        public static void StartPostfix(ItemStoreItem __instance)
        {
            if (!ShopService.TryGetNextLocation(SlotFormats, ShopService.GetMaxShopSlots(), out long locId, out string locName))
            {
                ShopService.DestroyShopItem(__instance.gameObject, __instance.priceMarker);
                return;
            }

            int defaultCost = __instance.usePlatinumCost ? 20 : 150;
            __instance.costStat?.Initialize(defaultCost);

            if (__instance.priceMarker != null)
                __instance.priceMarker.SetText(__instance.Cost.ToString());

            __instance.gameObject.AddComponent<APShopSlot>().Initialize(locName);

            if (__instance.itemSpriteRenderer != null)
                __instance.itemSpriteRenderer.sprite = APSpriteManager.APSprite;

            if (__instance.itemText != null)
                __instance.itemText.gameObject.SetActive(false);

            if (__instance.descText != null)
                __instance.descText.text = Plugin.AP.GetLocationDescription(locId);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemStoreItem.Buy))]
        public static bool BuyPrefix(ItemStoreItem __instance, Player player)
        {
            return ShopService.ProcessPurchase(
                __instance.gameObject,
                __instance.priceMarker,
                __instance.Cost,
                __instance.usePlatinumCost,
                onSuccess: () => __instance.parentNpc?.PlayDefaultEmote(),
                onFailure: () => __instance.PlayDenyBuyEffects()
            );
        }
    }
}