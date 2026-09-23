using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WoLArchipelago.Patches
{
    public static class SpellBookPatches
    {
        public static void RefreshPlayerPageSlotVisibility(SpellBookUI spellBookUI)
        {
            if (spellBookUI == null) return;

            var traverse = Traverse.Create(spellBookUI);
            var sbRefTraverse = traverse.Field("sbRef");
            var playerPage = sbRefTraverse.Field("playerPage").GetValue<SBPlayerPageUI>();
            if (playerPage == null) return;

            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                bool unlocked = SlotManager.IsSlotUnlocked(slotIndex);

                if (playerPage.cardBGTransArray != null && slotIndex < playerPage.cardBGTransArray.Length && playerPage.cardBGTransArray[slotIndex] != null)
                {
                    playerPage.cardBGTransArray[slotIndex].gameObject.SetActive(unlocked);
                }

                if (playerPage.spellIconArray != null && slotIndex < playerPage.spellIconArray.Length && playerPage.spellIconArray[slotIndex] != null)
                {
                    playerPage.spellIconArray[slotIndex].gameObject.SetActive(unlocked);
                }

                Transform slotTrans = playerPage.transform.Find("CardSlot" + slotIndex)
                                   ?? playerPage.transform.Find("Card" + slotIndex)
                                   ?? playerPage.transform.Find("Slot" + slotIndex);
                if (slotTrans != null)
                {
                    slotTrans.gameObject.SetActive(unlocked);
                }
            }
        }

        [HarmonyPatch(typeof(SpellBookUI), nameof(SpellBookUI.Activate))]
        public static class SpellBookUIActivatePatch
        {
            public static void Postfix(SpellBookUI __instance)
            {
                RefreshPlayerPageSlotVisibility(__instance);
            }
        }

        [HarmonyPatch(typeof(SpellBookUI), "SetFocus")]
        public static class SpellBookUISetFocusPatch
        {
            public static void Postfix(SpellBookUI __instance)
            {
                var traverse = Traverse.Create(__instance);
                SpellBookUI.SBFocus currentFocus = traverse.Field("currentFocus").GetValue<SpellBookUI.SBFocus>();

                if (currentFocus == SpellBookUI.SBFocus.Player)
                {
                    int currentIndex = traverse.Field("currentPlayerIndex").GetValue<int>();
                    if (currentIndex >= 0 && currentIndex <= 3 && !SlotManager.IsSlotUnlocked(currentIndex))
                    {
                        traverse.Field("currentPlayerIndex").SetValue(0);
                    }
                    RefreshPlayerPageSlotVisibility(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(SpellBookUI), "HandlePlayerSelection")]
        public static class SpellBookUIHandlePlayerSelectionPatch
        {
            public static bool Prefix(SpellBookUI __instance)
            {
                var traverse = Traverse.Create(__instance);
                ChaosInputDevice inputDev = traverse.Field("inputDev").GetValue<ChaosInputDevice>();
                int currentPlayerIndex = traverse.Field("currentPlayerIndex").GetValue<int>();

                InputDirection dir = Globals.GetInputDirection(inputDev.GetMoveVector());
                if (dir == InputDirection.None)
                {
                    traverse.Field("initNavDelayReset").SetValue(true);
                    traverse.Field("navTimer").Property("IsRunning").SetValue(false);
                    traverse.Field("autoNavTimer").Property("IsRunning").SetValue(false);
                    return false;
                }

                ChaosQuickStopwatch navTimer = traverse.Field("navTimer").GetValue<ChaosQuickStopwatch>();
                ChaosQuickStopwatch autoNavTimer = traverse.Field("autoNavTimer").GetValue<ChaosQuickStopwatch>();

                if (navTimer.IsRunning || autoNavTimer.IsRunning)
                {
                    return false;
                }

                int step = (dir == InputDirection.Right || dir == InputDirection.Down) ? 1 : -1;
                int nextIndex = currentPlayerIndex;

                if (dir == InputDirection.Right || dir == InputDirection.Left)
                {
                    nextIndex = GetNextUnlockedPlayerIndex(currentPlayerIndex, step);
                }
                else if (dir == InputDirection.Up || dir == InputDirection.Down)
                {
                    if (dir == InputDirection.Down && currentPlayerIndex <= 3)
                    {
                        nextIndex = 4;
                    }
                    else if (dir == InputDirection.Up && currentPlayerIndex > 3)
                    {
                        nextIndex = GetLastUnlockedCardIndex();
                    }
                }

                if (nextIndex != currentPlayerIndex)
                {
                    SoundManager.PlayAudio("MenuMove");

                    bool initNavDelayReset = traverse.Field("initNavDelayReset").GetValue<bool>();
                    if (initNavDelayReset)
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

                        SpellBookUI.SkillEquipType selectedType = SpellBookUI.SkillEquipType.Basic;
                        switch (nextIndex)
                        {
                            case 0: selectedType = SpellBookUI.SkillEquipType.Basic; break;
                            case 1: selectedType = SpellBookUI.SkillEquipType.Dash; break;
                            case 2: selectedType = SpellBookUI.SkillEquipType.Optional; break;
                            case 3: selectedType = SpellBookUI.SkillEquipType.Signature; break;
                        }
                        traverse.Field("playerInfoSelectedType").SetValue(selectedType);

                        var plTypeSkillDict = sbRefTraverse.Field("plTypeSkillDict").GetValue<Dictionary<SpellBookUI.SkillEquipType, Player.SkillState>>();
                        Player.SkillState skill = null;
                        if (plTypeSkillDict != null && plTypeSkillDict.TryGetValue(selectedType, out var s))
                        {
                            skill = s;
                        }
                        traverse.Field("currentSkill").SetValue(skill);
                    }

                    playerPage.SetHighlightedCard(nextIndex);
                }

                return false;
            }

            private static bool IsPlayerIndexUnlocked(int index)
            {
                if (index >= 0 && index <= 3)
                {
                    return SlotManager.IsSlotUnlocked(index);
                }
                return true;
            }

            private static int GetNextUnlockedPlayerIndex(int currentIndex, int step)
            {
                int next = currentIndex + step;
                while (next >= 0 && next <= 4 && !IsPlayerIndexUnlocked(next))
                {
                    next += step;
                }

                if (next < 0 || next > 4)
                    return currentIndex;

                return next;
            }

            private static int GetLastUnlockedCardIndex()
            {
                for (int i = 3; i >= 0; i--)
                {
                    if (IsPlayerIndexUnlocked(i)) return i;
                }
                return 0;
            }
        }

        [HarmonyPatch(typeof(SpellBookUI), nameof(SpellBookUI.ConfirmSelected))]
        public static class SpellBookUIConfirmSelectedPatch
        {
            public static bool Prefix(SpellBookUI __instance)
            {
                var traverse = Traverse.Create(__instance);
                SpellBookUI.SBFocus currentFocus = traverse.Field("currentFocus").GetValue<SpellBookUI.SBFocus>();
                Player.SkillState currentSkill = traverse.Field("currentSkill").GetValue<Player.SkillState>();

                if (currentFocus == SpellBookUI.SBFocus.Overdrive)
                {
                    if (!SlotManager.IsSlotUnlocked(3))
                    {
                        SoundManager.PlayAudio("MenuError");
                        return false;
                    }
                }
                else if (currentFocus == SpellBookUI.SBFocus.Spell && currentSkill != null)
                {
                    int targetSlot = (!currentSkill.isBasic) ? (currentSkill.isDash ? 1 : 2) : 0;
                    if (!SlotManager.IsSlotUnlocked(targetSlot))
                    {
                        SoundManager.PlayAudio("MenuError");
                        return false;
                    }
                }
                else if (currentFocus == SpellBookUI.SBFocus.Player)
                {
                    int currentPlayerIndex = traverse.Field("currentPlayerIndex").GetValue<int>();
                    if (currentPlayerIndex >= 0 && currentPlayerIndex <= 3 && !SlotManager.IsSlotUnlocked(currentPlayerIndex))
                    {
                        SoundManager.PlayAudio("MenuError");
                        return false;
                    }
                }

                return true;
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
                        SpellBookUI.SkillEquipType playerInfoSelectedType = traverse.Field("playerInfoSelectedType").GetValue<SpellBookUI.SkillEquipType>();
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

                if (!SlotManager.IsSlotUnlocked(targetSlot))
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