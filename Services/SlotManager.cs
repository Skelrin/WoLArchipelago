using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace WoLArchipelago
{
    public static class SlotManager
    {
        public static readonly int[] SlotItemIds = new int[]
        {
            0,          // Slot 0 (Basic)
            0,          // Slot 1 (Dash)
            871122001,  // Slot 2 (Standard Arcana Slot)
            871122002,  // Slot 3 (Signature Arcana Slot)
            871122003,  // Slot 4 (Bonus Arcana Slot 1)
            871122004   // Slot 5 (Bonus Arcana Slot 2)
        };

        public static bool IsSlotUnlocked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 6) return true;
            
            if (slotIndex < 2) return true;

            return Services.ItemHandler.IsItemUnlocked(SlotItemIds[slotIndex]);
        }

        public static int GetFirstUnlockedSlot()
        {
            for (int i = 0; i < 6; i++)
            {
                if (IsSlotUnlocked(i)) return i;
            }
            return 0;
        }

        public static int GetLastUnlockedSlot()
        {
            for (int i = 5; i >= 0; i--)
            {
                if (IsSlotUnlocked(i)) return i;
            }
            return 0;
        }
    }
}