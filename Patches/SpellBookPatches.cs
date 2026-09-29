using System.Collections.Generic;
using HarmonyLib;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches for the SpellBook UI to manage locked skill slots, navigation skipping, and spell assignment validation.
    /// </summary>
    public static class SpellBookPatches
    {
        /// <summary>
        /// Prevent seeing the card slot if the corresponding skill slot is locked.
        /// </summary>
        [HarmonyPatch(typeof(SBPlayerPageUI), nameof(SBPlayerPageUI.SetHighlightedCard))]
        public static class SBPlayerPageUISetHighlightedCardPatch
        {
            public static void Postfix(SBPlayerPageUI __instance)
            {
                for (int slotIndex = 2; slotIndex <= 3; slotIndex++)
                {
                    bool isUnlocked = SlotManager.IsSlotUnlocked(slotIndex);

                    if (!isUnlocked)
                    {
                        __instance.cardBGTransArray[slotIndex]?.gameObject.SetActive(false);
                        __instance.spellIconArray[slotIndex]?.gameObject.SetActive(false);
                        __instance.cardTextArray[slotIndex]?.gameObject.SetActive(false);
                        __instance.newCardMarkerArray[slotIndex]?.gameObject.SetActive(false);
                        __instance.cardSelArray[slotIndex]?.SetActive(false);
                    }
                    else
                    {
                        __instance.cardBGTransArray[slotIndex]?.gameObject.SetActive(true);
                        __instance.spellIconArray[slotIndex]?.gameObject.SetActive(true);
                        __instance.cardTextArray[slotIndex]?.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Overrides input navigation in the SpellBook UI to skip over locked skill card slots.
        /// </summary>
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

                if (navTimer.IsRunning || autoNavTimer.IsRunning) 
                    return false;

                int nextIndex = CalculateNextPlayerIndex(currentPlayerIndex, dir);

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
                        traverse.Field("currentSkill").SetValue(plTypeSkillDict != null && plTypeSkillDict.TryGetValue(selectedType, out var skill) ? skill : null);
                    }

                    playerPage.SetHighlightedCard(nextIndex);
                }

                return false;
            }

            private static int CalculateNextPlayerIndex(int currentIndex, InputDirection dir)
            {
                if (dir == InputDirection.Right || dir == InputDirection.Left)
                {
                    int step = (dir == InputDirection.Right) ? 1 : -1;
                    return GetNextUnlockedPlayerIndex(currentIndex, step);
                }
                
                if (dir == InputDirection.Down && currentIndex <= 3)
                {
                    return 4;
                }
                
                if (dir == InputDirection.Up && currentIndex > 3)
                {
                    return GetLastUnlockedCardIndex();
                }

                return currentIndex;
            }

            private static int GetNextUnlockedPlayerIndex(int currentIndex, int step)
            {
                int next = currentIndex + step;
                while (next == 2 || next == 3)
                {
                    if (SlotManager.IsSlotUnlocked(next)) 
                        break;
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

        /// <summary>
        /// Prevent assigning a skill if the player doesn't have the correct license or skill slot unlocked.
        /// </summary>
        [HarmonyPatch(typeof(SpellBookUI), "AssignPlayerSkill")]
        public static class SpellBookUIAssignPlayerSkillPatch
        {
            public static bool Prefix(SpellBookUI __instance, SpellBookUI.SBFocus givenFocus, ref bool __result)
            {
                var traverse = Traverse.Create(__instance);
                Player.SkillState currentSkill = traverse.Field("currentSkill").GetValue<Player.SkillState>();
                
                if (currentSkill == null) 
                    return true;

                if (!SlotManager.PlayerHasLicenseToPickupSkill(currentSkill))
                {
                    SoundManager.PlayAudio("MenuError");
                    __result = false;
                    return false;
                }

                int targetSlot = DetermineTargetSlot(traverse, givenFocus, currentSkill);

                if (targetSlot >= 2 && !SlotManager.IsSlotUnlocked(targetSlot))
                {
                    SoundManager.PlayAudio("MenuError");
                    __result = false; 
                    return false;     
                }

                return true;
            }

            private static int DetermineTargetSlot(Traverse traverse, SpellBookUI.SBFocus focus, Player.SkillState skill)
            {
                switch (focus)
                {
                    case SpellBookUI.SBFocus.Player:
                        var selectedType = traverse.Field("playerInfoSelectedType").GetValue<SpellBookUI.SkillEquipType>();
                        var skillEquipSlots = traverse.Field("sbRef").Field("skillEquipSlots").GetValue<Dictionary<SpellBookUI.SkillEquipType, int>>();
                        if (skillEquipSlots != null && skillEquipSlots.TryGetValue(selectedType, out int slot))
                        {
                            return slot;
                        }
                        return 0;

                    case SpellBookUI.SBFocus.Overdrive:
                        return 3;

                    case SpellBookUI.SBFocus.Spell:
                        if (skill.isBasic) return 0;
                        if (skill.isDash) return 1;
                        return 2;

                    default:
                        return 0;
                }
            }
        }
    }
}