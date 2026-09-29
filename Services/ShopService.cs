using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WoLArchipelago.Services
{
    /// <summary>
    /// Core service handling Archipelago logic for in-game shops, including location assignment, visuals, and transactions.
    /// </summary>
    public class ShopService
    {
        private static readonly HashSet<long> assignedLocationsInScene = [];

        public static void ClearSceneAssignments()
        {
            assignedLocationsInScene.Clear();
        }

        public static int GetMaxShopSlots()
        {
            return Mathf.Min(ItemHandler.GetTotalShopUpgrade() * 16, 64);
        }

        /// <summary>
        /// Iterates through shop slots to find the next available, unchecked Archipelago location.
        /// </summary>
        public static bool TryGetNextLocation(string slotNameFormat, int maxSlots, out long locId, out string locName)
        {
            locId = -1;
            locName = string.Empty;

            for (int slotIndex = 1; slotIndex <= maxSlots; slotIndex++)
            {
                string candidateName = string.Format(slotNameFormat, slotIndex);
                long candidateId = APItemLocationDatabase.GetLocationId(candidateName);

                if (candidateId == -1) continue;

                bool isChecked = Plugin.AP.GetCheckedLocation().Contains(candidateId);
                bool isAlreadySpawned = assignedLocationsInScene.Contains(candidateId);

                if (!isChecked && !isAlreadySpawned)
                {
                    locId = candidateId;
                    locName = candidateName;
                    assignedLocationsInScene.Add(candidateId);
                    return true;
                }
            }

            return false;
        }

        public static void DestroyShopItem(GameObject gameObject, Component priceMarker)
        {
            if (priceMarker != null)
            {
                Object.Destroy(priceMarker.gameObject);
            }
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }

        /// <summary>
        /// Configure an in-game shop item to represent an Archipelago location.
        /// </summary>
        public static void SetupAPShopItem(
            GameObject gameObject,
            string locationName,
            long locationId,
            SpriteRenderer itemSpriteRenderer,
            Action<string, string> setTextAction)
        {
            gameObject.AddComponent<APShopSlot>().Initialize(locationName);

            if (itemSpriteRenderer != null)
            {
                itemSpriteRenderer.sprite = APSpriteManager.APSprite;
            }

            var locationInfo = Plugin.AP.GetLocationInfo(locationId);
            setTextAction?.Invoke(locationInfo.First, locationInfo.Second);
        }

        /// <summary>
        /// Handles the validation and transaction logic when a player attempts to buy an Archipelago item.
        /// </summary>
        public static bool ProcessPurchase(
            GameObject gameObject,
            Component priceMarker,
            int cost,
            bool usePlatinumCost,
            Action onSuccess = null,
            Action onFailure = null)
        {
            APShopSlot apSlot = gameObject.GetComponent<APShopSlot>();
            if (apSlot == null) return true;

            bool canAfford = usePlatinumCost
                ? (Player.platWallet != null && Player.platWallet.balance >= cost)
                : (Player.goldWallet != null && Player.goldWallet.balance >= cost);

            if (canAfford)
            {
                if (usePlatinumCost)
                    Player.platWallet.Withdraw(cost);
                else
                    Player.goldWallet.Withdraw(cost);

                Plugin.AP.SendLocationCheck(apSlot.LocationId);
                SoundManager.PlayAudio("MenuBuy");

                onSuccess?.Invoke();
                DestroyShopItem(gameObject, priceMarker);
            }
            else
            {
                if (onFailure != null)
                {
                    onFailure.Invoke();
                }
                else
                {
                    SoundManager.PlayAudio("MenuError");
                }

                string errorMsg = usePlatinumCost ? "Not enough chaos gems !" : "Not enough gold !";
                GameUI.BroadcastNoticeMessage(errorMsg);
            }

            return false;
        }
    }
}