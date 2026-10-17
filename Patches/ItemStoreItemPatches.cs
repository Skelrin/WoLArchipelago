using HarmonyLib;
using UnityEngine;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches for ItemStoreItem that convert standard relic and cursed shop items with Archipelago location checks.
    /// Vanilla items are kept as a fallback in the dungeon when no AP location is available (or Nox is locked).
    /// </summary>
    [HarmonyPatch(typeof(ItemStoreItem))]
    public class ItemStoreItemPatches
    {
        private const string TokenCursedItemId = "TokenCursed";

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemStoreItem.Start))]
        public static void StartPostfix(ItemStoreItem __instance)
        {
            string slotFormat;
            int maxSlots;

            if (__instance.cursedOnly)
            {
                slotFormat = "Nox the Unfortunate Slot {0}";
                maxSlots = 20;
            }
            else
            {
                slotFormat = "Relic Shop Slot {0}";
                maxSlots = ShopService.GetMaxShopSlots();
            }

            bool cursedUnlocked = !__instance.cursedOnly
                                || ItemHandler.GetItemCountByName(TokenCursedItemId) > 0;

            if (!cursedUnlocked || !ShopService.TryGetNextLocation(slotFormat, maxSlots, out long locId, out string locName))
            {
                if (__instance.usePlatinumCost)
                {
                    ShopService.DestroyShopItem(__instance.gameObject, __instance.priceMarker);
                }
                return;
            }

            // On Plaza, setup arbitrary price of 20 Chaos gems
            if (__instance.usePlatinumCost)
            {
                __instance.costStat?.Initialize(20);
            }

            __instance.priceMarker?.SetText(__instance.Cost.ToString());

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
        [HarmonyPatch(nameof(ItemStoreItem.Buy))]
        public static bool BuyPrefix(ItemStoreItem __instance, Player player)
        {
            if (!ShopService.IsAPShopItem(__instance.gameObject))
                return PrepareVanillaPurchase(__instance, player);

            return ShopService.ProcessPurchase(
                __instance.gameObject,
                __instance.priceMarker,
                __instance.Cost,
                __instance.usePlatinumCost,
                onSuccess: () =>
                {
                    __instance.parentNpc?.PlayDefaultEmote();

                    // When acquiring Nox's Archipelago Locations, the player get a health malus to balance the gratuity of the Location
                    if (__instance.cursedOnly)
                    {
                        Player targetPlayer = player ?? ItemHandler.GetActivePlayer();
                        if (targetPlayer?.health != null)
                        {
                            targetPlayer.health.CurrentHealthValue = Mathf.Max(1, targetPlayer.health.CurrentHealthValue - 50);
                        }
                    }
                },
                onFailure: __instance.PlayDenyBuyEffects
            );
        }

        private static bool PrepareVanillaPurchase(ItemStoreItem item, Player player)
        {
            if (player == null) return true;

            if (player.inventory.CursedCount >= 12
                || player.inventory.ContainsItem(item.ID)
                || Player.goldWallet.balance < item.Cost)
                return true;

            if (ItemDropPatches.TryMakeRoomForPickup(player, item.ID))
                return true;

            item.PlayDenyBuyEffects();
            return false;
        }
    }
}