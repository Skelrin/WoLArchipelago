using HarmonyLib;
using UnityEngine;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(OutfitStoreItem))]
    public class OutfitStoreItemPatches
    {
        private static readonly string SlotFormat = "Outfit Shop Slot {0}";

        public static void ClearSceneAssignments() => Services.ShopService.ClearSceneAssignments();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(OutfitStoreItem.Start))]
        public static void StartPostfix(OutfitStoreItem __instance)
        {

            int maxSlots = Services.ItemHandler.GetTotalShopUpgrade() == 0 ? 0 : 16;

            if (!Services.ShopService.TryGetNextLocation(SlotFormat, maxSlots, out long locId, out string locName))
            {
                Services.ShopService.DestroyShopItem(__instance.gameObject, __instance.priceMarker);
                return;
            }

            __instance.overheadPromptHeight = 3.8f;
            __instance.usePlatinumCost = true;
            __instance.costStat?.Initialize(20);

            if (__instance.priceMarker != null)
                __instance.priceMarker.SetText(__instance.Cost.ToString());

            __instance.gameObject.AddComponent<APShopSlot>().Initialize(locName);

            if (__instance.itemBGSpriteRenderer != null)
                __instance.itemBGSpriteRenderer.enabled = false;

            if (__instance.itemSpriteRenderer != null)
            {
                __instance.itemSpriteRenderer.sprite = APSpriteManager.APSprite;
                __instance.itemSpriteRenderer.material = new Material(Shader.Find("Sprites/Default"));

                Vector3 pos = __instance.itemSpriteRenderer.transform.localPosition;
                __instance.itemSpriteRenderer.transform.localPosition = new Vector3(pos.x, pos.y + 1f, pos.z);
            }

            if (__instance.itemText != null)
                __instance.itemText.text = Plugin.AP.GetLocationInfo(locId).First;

            if (__instance.descText != null)
                __instance.descText.text = Plugin.AP.GetLocationInfo(locId).Second;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(OutfitStoreItem.Buy))]
        public static bool BuyPrefix(OutfitStoreItem __instance)
        {
            return Services.ShopService.ProcessPurchase(
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