using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WoLArchipelago
{
    public static class ShopService
    {
        private static readonly HashSet<long> assignedLocationsInScene = new HashSet<long>();

        public static void ClearSceneAssignments()
        {
            assignedLocationsInScene.Clear();
        }

        public static int GetMaxShopSlots()
        {
            return Mathf.Min((ItemHandler.GetTotalShopUpgrade() + 1) * 16, 64);
        }

        public static bool TryGetNextLocation(string[] slotNameFormats, int maxSlots, out long locId, out string locName)
        {
            locId = -1;
            locName = string.Empty;

            if (Plugin.AP == null || !Plugin.AP.IsConnected) return false;

            for (int slotIndex = 1; slotIndex <= maxSlots; slotIndex++)
            {
                foreach (string format in slotNameFormats)
                {
                    string candidateName = string.Format(format, slotIndex);
                    long candidateId = APItemLocationDatabase.GetLocationId(candidateName);

                    if (candidateId == -1) continue;

                    bool isChecked = Plugin.AP.session.Locations.AllLocationsChecked.Contains(candidateId);
                    bool isAlreadySpawned = assignedLocationsInScene.Contains(candidateId);

                    if (!isChecked && !isAlreadySpawned)
                    {
                        locId = candidateId;
                        locName = candidateName;
                        assignedLocationsInScene.Add(candidateId);
                        return true;
                    }
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
                GameUI.BroadcastNoticeMessage($"AP Check Sent: {apSlot.LocationName}!");

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