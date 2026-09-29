using HarmonyLib;
using UnityEngine;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches for OutfitStoreItem to replace standard outfit items with Archipelago location checks.
    /// </summary>
    [HarmonyPatch(typeof(OutfitStoreItem))]
    public class OutfitStoreItemPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(OutfitStoreItem.Start))]
        public static void StartPostfix(OutfitStoreItem __instance)
        {
            string SlotFormat = "Outfit Shop Slot {0}";

            // Player need to have at least 1 shop upgrade to unlock all the outfit store
            int maxSlots = ItemHandler.GetTotalShopUpgrade() == 0 ? 0 : 16;

            if (!ShopService.TryGetNextLocation(SlotFormat, maxSlots, out long locId, out string locName))
            {
                ShopService.DestroyShopItem(__instance.gameObject, __instance.priceMarker);
                return;
            }

            __instance.overheadPromptHeight = 3.8f;
            __instance.usePlatinumCost = true;
            // On Plaza, setup arbitrary price of 20 Chaos gems
            __instance.costStat?.Initialize(20);

            __instance.priceMarker?.SetText(__instance.Cost.ToString());

            // Remove mannequin to correctly show archipelago logo
            if (__instance.itemBGSpriteRenderer != null) __instance.itemBGSpriteRenderer.enabled = false;

            if (__instance.itemSpriteRenderer != null)
            {
                __instance.itemSpriteRenderer.material = new Material(Shader.Find("Sprites/Default"));

                Vector3 pos = __instance.itemSpriteRenderer.transform.localPosition;
                __instance.itemSpriteRenderer.transform.localPosition = new Vector3(pos.x, pos.y + 1f, pos.z);
            }

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
        [HarmonyPatch(nameof(OutfitStoreItem.Buy))]
        public static bool BuyPrefix(OutfitStoreItem __instance)
        {
            return ShopService.ProcessPurchase(
                __instance.gameObject,
                __instance.priceMarker,
                __instance.Cost,
                usePlatinumCost: true,
                onSuccess: () => __instance.parentNpc?.PlayDefaultEmote(),
                onFailure: () => {
                    SoundManager.PlayAudio("MenuError");
                    __instance.parentNpc?.PlayEmote(EmoteType.No);
                }
            );
        }
    }
}