using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WoLArchipelago.Patches
{
    public static class SpellBookPatches
    {
       [HarmonyPatch(typeof(SBPlayerPageUI), nameof(SBPlayerPageUI.SetHighlightedCard))]
        public static class SBPlayerPageUISetHighlightedCardPatch
        {
            public static void Postfix(SBPlayerPageUI __instance)
            {
                for (int slotIndex = 2; slotIndex <= 3; slotIndex++)
                {
                    if (!SlotManager.IsSlotUnlocked(slotIndex))
                    {
                        if (__instance.cardBGTransArray[slotIndex] != null) __instance.cardBGTransArray[slotIndex].gameObject.SetActive(false);
                        if (__instance.spellIconArray[slotIndex] != null) __instance.spellIconArray[slotIndex].gameObject.SetActive(false);
                        if (__instance.cardTextArray[slotIndex] != null) __instance.cardTextArray[slotIndex].gameObject.SetActive(false);
                        if (__instance.newCardMarkerArray[slotIndex] != null) __instance.newCardMarkerArray[slotIndex].gameObject.SetActive(false);
                        if (__instance.cardSelArray[slotIndex] != null) __instance.cardSelArray[slotIndex].SetActive(false);
                    }
                    else
                    {
                        if (__instance.cardBGTransArray[slotIndex] != null) __instance.cardBGTransArray[slotIndex].gameObject.SetActive(true);
                        if (__instance.spellIconArray[slotIndex] != null) __instance.spellIconArray[slotIndex].gameObject.SetActive(true);
                        if (__instance.cardTextArray[slotIndex] != null) __instance.cardTextArray[slotIndex].gameObject.SetActive(true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SpellBookUI), "HandlePlayerSelection")]
        public static class SpellBookUIHandlePlayerSelectionPatch
        {
            public static bool Prefix(SpellBookUI __instance)
            {
                var traverse = Traverse.Create(__instance);
                var inputDev = traverse.Field("inputDev").GetValue<ChaosInputDevice>();
                int currentPlayerIndex = traverse.Field("currentPlayerIndex").GetValue<int>();

                InputDirection dir = Globals.GetInputDirection(inputDev.GetMoveVector());
                if (dir == InputDirection.None)
                {
                    traverse.Field("initNavDelayReset").SetValue(true);
                    traverse.Field("navTimer").Property("IsRunning").SetValue(false);
                    traverse.Field("autoNavTimer").Property("IsRunning").SetValue(false);
                    return false;
                }

                var navTimer = traverse.Field("navTimer").GetValue<ChaosQuickStopwatch>();
                var autoNavTimer = traverse.Field("autoNavTimer").GetValue<ChaosQuickStopwatch>();

                if (navTimer.IsRunning || autoNavTimer.IsRunning) return false;

                int nextIndex = currentPlayerIndex;

                if (dir == InputDirection.Right || dir == InputDirection.Left)
                {
                    int step = (dir == InputDirection.Right) ? 1 : -1;
                    nextIndex = GetNextUnlockedPlayerIndex(currentPlayerIndex, step);
                }
                else if (dir == InputDirection.Down && currentPlayerIndex <= 3)
                {
                    nextIndex = 4;
                }
                else if (dir == InputDirection.Up && currentPlayerIndex > 3)
                {
                    nextIndex = GetLastUnlockedCardIndex();
                }

                if (nextIndex != currentPlayerIndex)
                {
                    SoundManager.PlayAudio("MenuMove");
                    
                    if (traverse.Field("initNavDelayReset").GetValue<bool>())
                    {
                        traverse.Field("initNavDelayReset").SetValue(false);
                        navTimer.IsRunning = true;
                    }
                    else
                    {
                        autoNavTimer.IsRunning = true;
                    }

                    traverse.Field("currentPlayerIndex").SetValue(nextIndex);
                    var sbRefTraverse = traverse.Field("sbRef");
                    var playerPage = sbRefTraverse.Field("playerPage").GetValue<SBPlayerPageUI>();

                    if (nextIndex > 3)
                    {
                        traverse.Field("currentSkill").SetValue(null);
                        playerPage.allSkillsFocusedObj.SetActive(true);
                        playerPage.allSkillsUnfocusedObj.SetActive(false);
                    }
                    else
                    {
                        playerPage.allSkillsFocusedObj.SetActive(false);
                        playerPage.allSkillsUnfocusedObj.SetActive(true);

                        SpellBookUI.SkillEquipType selectedType = (SpellBookUI.SkillEquipType)nextIndex;
                        traverse.Field("playerInfoSelectedType").SetValue(selectedType);

                        var plTypeSkillDict = sbRefTraverse.Field("plTypeSkillDict").GetValue<Dictionary<SpellBookUI.SkillEquipType, Player.SkillState>>();
                        traverse.Field("currentSkill").SetValue(plTypeSkillDict != null && plTypeSkillDict.ContainsKey(selectedType) ? plTypeSkillDict[selectedType] : null);
                    }

                    playerPage.SetHighlightedCard(nextIndex);
                }

                return false;
            }

            private static int GetNextUnlockedPlayerIndex(int currentIndex, int step)
            {
                int next = currentIndex + step;
                while (next == 2 || next == 3)
                {
                    if (SlotManager.IsSlotUnlocked(next)) break;
                    next += step;
                }
                return (next < 0 || next > 4) ? currentIndex : next;
            }

            private static int GetLastUnlockedCardIndex()
            {
                if (SlotManager.IsSlotUnlocked(3)) return 3;
                if (SlotManager.IsSlotUnlocked(2)) return 2;
                return 1;
            }
        }

        [HarmonyPatch(typeof(SpellBookUI), "AssignPlayerSkill")]
        public static class SpellBookUIAssignPlayerSkillPatch
        {
            public static bool Prefix(SpellBookUI __instance, SpellBookUI.SBFocus givenFocus, ref bool __result)
            {
                var traverse = Traverse.Create(__instance);
                Player.SkillState currentSkill = traverse.Field("currentSkill").GetValue<Player.SkillState>();
                
                if (currentSkill == null) return true;

                int targetSlot = 0;
                switch (givenFocus)
                {
                    case SpellBookUI.SBFocus.Player:
                        var playerInfoSelectedType = traverse.Field("playerInfoSelectedType").GetValue<SpellBookUI.SkillEquipType>();
                        var skillEquipSlots = traverse.Field("sbRef").Field("skillEquipSlots").GetValue<Dictionary<SpellBookUI.SkillEquipType, int>>();
                        if (skillEquipSlots != null && skillEquipSlots.TryGetValue(playerInfoSelectedType, out int slot))
                        {
                            targetSlot = slot;
                        }
                        break;

                    case SpellBookUI.SBFocus.Overdrive:
                        targetSlot = 3;
                        break;

                    case SpellBookUI.SBFocus.Spell:
                        targetSlot = (!currentSkill.isBasic) ? (currentSkill.isDash ? 1 : 2) : 0;
                        break;
                }

                if (targetSlot >= 2 && !SlotManager.IsSlotUnlocked(targetSlot))
                {
                    SoundManager.PlayAudio("MenuError");
                    __result = false; 
                    return false;     
                }

                return true;
            }
        }
    }
}