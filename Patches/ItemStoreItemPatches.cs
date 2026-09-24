using HarmonyLib;
using UnityEngine;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(ItemStoreItem))]
    public class ItemStoreItemPatches
    {
        public static void ClearSceneAssignments() => Services.ShopService.ClearSceneAssignments();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemStoreItem.Start))]
        public static void StartPostfix(ItemStoreItem __instance)
        {
            string slotFormat;
            int maxSlots;

            if (__instance.cursedOnly)
            {
                slotFormat = "Nox the Unfortunate Slot {0}";
                maxSlots = 40;
            }
            else
            {
                slotFormat = "Relic Shop Slot {0}";
                maxSlots = Services.ShopService.GetMaxShopSlots();
            }

            if (!Services.ShopService.TryGetNextLocation(slotFormat, maxSlots, out long locId, out string locName))
            {
                Services.ShopService.DestroyShopItem(__instance.gameObject, __instance.priceMarker);
                return;
            }

            if (__instance.usePlatinumCost)
            {
                __instance.costStat?.Initialize(20);
            }

            if (__instance.priceMarker != null)
                __instance.priceMarker.SetText(__instance.Cost.ToString());

            __instance.gameObject.AddComponent<APShopSlot>().Initialize(locName);

            if (__instance.itemSpriteRenderer != null)
                __instance.itemSpriteRenderer.sprite = APSpriteManager.APSprite;

            if (__instance.itemText != null)
                __instance.itemText.text = Plugin.AP.GetLocationInfo(locId).First;

            if (__instance.descText != null)
                __instance.descText.text = Plugin.AP.GetLocationInfo(locId).Second;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemStoreItem.Buy))]
        public static bool BuyPrefix(ItemStoreItem __instance, Player player)
        {
            return Services.ShopService.ProcessPurchase(
                __instance.gameObject,
                __instance.priceMarker,
                __instance.Cost,
                __instance.usePlatinumCost,
                onSuccess: () =>
                {
                    __instance.parentNpc?.PlayDefaultEmote();

                    if (__instance.cursedOnly)
                    {
                        Player targetPlayer = player ?? Services.ItemHandler.GetActivePlayer();
                        if (targetPlayer?.health != null)
                        {
                            targetPlayer.health.CurrentHealthValue = Mathf.Max(1, targetPlayer.health.CurrentHealthValue - 50);
                        }
                    }
                },
                onFailure: __instance.PlayDenyBuyEffects
            );
        }
    }
}