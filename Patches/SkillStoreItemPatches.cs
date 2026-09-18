using HarmonyLib;

namespace WoLArchipelago
{
    [HarmonyPatch(typeof(SkillStoreItem))]
    public static class SkillStoreItemPatches
    {
        private static readonly string[] SlotFormats = { "Arcana Shop Slot {0}" };

        public static void ClearSceneAssignments() => ShopService.ClearSceneAssignments();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SkillStoreItem.Start))]
        public static void StartPostfix(SkillStoreItem __instance)
        {
            if (!ShopService.TryGetNextLocation(SlotFormats, ShopService.GetMaxShopSlots(), out long locId, out string locName))
            {
                ShopService.DestroyShopItem(__instance.gameObject, __instance.priceMarker);
                return;
            }

            Traverse.Create(__instance).Field("initialized").SetValue(true);

            int defaultCost = __instance.usePlatinumCost ? 20 : 150;
            __instance.costStat?.Initialize(defaultCost);

            if (__instance.priceMarker != null)
                __instance.priceMarker.SetText(__instance.Cost.ToString());

            __instance.gameObject.AddComponent<APShopSlot>().Initialize(locName);

            if (__instance.itemSpriteRenderer != null)
                __instance.itemSpriteRenderer.sprite = APSpriteManager.APSprite;

            if (__instance.standardSR != null) __instance.standardSR.enabled = false;
            if (__instance.empoweredSR != null) __instance.empoweredSR.enabled = false;
            if (__instance.sigSR != null) __instance.sigSR.enabled = false;

            if (__instance.itemText != null)
                __instance.itemText.gameObject.SetActive(false);

            if (__instance.descText != null)
                __instance.descText.text = Plugin.AP.GetLocationDescription(locId);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(SkillStoreItem.Buy))]
        public static bool BuyPrefix(SkillStoreItem __instance, Player player)
        {
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