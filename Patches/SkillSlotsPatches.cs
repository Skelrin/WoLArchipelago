using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace WoLArchipelago.Patches
{
    public static class SkillSlotsPatches
    {
        [HarmonyPatch(typeof(CooldownUI), nameof(CooldownUI.RefreshEntries))]
        public static class CooldownUIRefreshEntriesPatch
        {
            // Deactivate LowerHUD locked skills and prevent deactivated skill reappear if the game refresh its UI.
            public static void Postfix(CooldownUI __instance)
            {
                for (int i = 0; i < 6; i++)
                {
                    Transform skillTrans = __instance.transform.Find("Skill" + i);
                    if (skillTrans != null)
                    {
                        skillTrans.gameObject.SetActive(SlotManager.IsSlotUnlocked(i));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EquipMenu), nameof(EquipMenu.LoadEquipMenu))]
        public static class EquipMenuLoadEquipMenuPatch
        {
            // In the inventory deactivate skills UI not unlocked
            public static void Postfix(EquipMenu __instance, ref int ___navigationIndex)
            {
                Transform equipBar = __instance.transform.Find("EquipBar");
                if (equipBar != null)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        bool unlocked = SlotManager.IsSlotUnlocked(i);

                        Transform skillTrans = equipBar.Find("Skills/Skill" + (i + 1));
                        if (skillTrans != null)
                        {
                            skillTrans.gameObject.SetActive(unlocked);
                        }

                        Transform buttonTrans = equipBar.Find("ButtonIcons/Button" + (i + 1));
                        if (buttonTrans != null)
                        {
                            buttonTrans.gameObject.SetActive(unlocked);
                        }
                    }
                }

                if (!SlotManager.IsSlotUnlocked(___navigationIndex))
                {
                    ___navigationIndex = SlotManager.GetFirstUnlockedSlot();
                    __instance.SelectSlot(___navigationIndex);
                }
            }
        }

        // Prevent navigating on hidden locked skills
        [HarmonyPatch(typeof(EquipMenu), "ChangeHorizontalIndex")]
        public static class EquipMenuChangeHorizontalIndexPatch
        {
            public static bool Prefix(int navIndex, bool increment, ref int __result)
            {
                int step = increment ? 1 : -1;
                int next = (navIndex + step + 6) % 6;
                int count = 0;

                while (!SlotManager.IsSlotUnlocked(next) && count < 6)
                {
                    next = (next + step + 6) % 6;
                    count++;
                }

                __result = next;
                return false;
            }
        }

        // Prevent selecting a locked skill slot for swapping skills
        [HarmonyPatch(typeof(EquipMenu), nameof(EquipMenu.SelectSlot))]
        public static class EquipMenuSelectSlotPatch
        {
            public static void Prefix(ref int navIndex)
            {
                if (navIndex >= 0 && navIndex < 6 && !SlotManager.IsSlotUnlocked(navIndex))
                {
                    navIndex = SlotManager.GetLastUnlockedSlot();
                }
            }
        }

        // Rewriting HandleQuickSwap to prevent quickswapping stockSkill with a locked slot
        [HarmonyPatch(typeof(EquipMenu), "HandleQuickSwap")]
        public static class EquipMenuHandleQuickSwapPatch
        {
            public static bool Prefix(EquipMenu __instance, ref bool __result)
            {
                var traverse = Traverse.Create(__instance);
                bool quickSwapCompleted = traverse.Field("quickSwapCompleted").GetValue<bool>();
                Player player = __instance.player;

                if (!quickSwapCompleted && player.inputDevice.GetButton("EquipMenu") && player.stockSkill != null)
                {
                    Image[] skillBorders = traverse.Field("skillBorders").GetValue<Image[]>();
                    Text swapText = traverse.Field("swapText").GetValue<Text>();
                    GameObject swapPrompt = traverse.Field("swapPrompt").GetValue<GameObject>();

                    skillBorders[6].color = Color.green;
                    swapText.gameObject.SetActive(true);
                    swapPrompt.SetActive(false);

                    for (int i = 0; i < player.assignedSkills.Length; i++)
                    {
                        if (player.inputDevice.GetButton("Skill" + i))
                        {
                            if (!SlotManager.IsSlotUnlocked(i))
                            {
                                SoundManager.PlayAudio("MenuError");
                                break;
                            }

                            bool isSignificant = traverse.Method("IsSignificantSkill", player.assignedSkills[i], player.stockSkill).GetValue<bool>();
                            if (!isSignificant)
                            {
                                Player.SkillState stockSkill = player.stockSkill;
                                player.stockSkill = player.assignedSkills[i];
                                player.AssignSkillSlot(i, stockSkill.skillID);
                                SoundManager.PlayAudio("Equip");
                                __instance.parentHUD.cooldownUI.RefreshEntries();
                                traverse.Field("quickSwapCompleted").SetValue(true);
                                break;
                            }
                            SoundManager.PlayAudio("MenuError");
                        }
                    }
                    __result = true;
                    return false;
                }

                bool currentlySwapping = traverse.Field("currentlySwapping").GetValue<bool>();
                if (!currentlySwapping)
                {
                    GameObject skillSelectedBorder = traverse.Field("skillSelectedBorder").GetValue<GameObject>();
                    Image[] skillBorders = traverse.Field("skillBorders").GetValue<Image[]>();
                    Text swapText = traverse.Field("swapText").GetValue<Text>();
                    GameObject swapPrompt = traverse.Field("swapPrompt").GetValue<GameObject>();
                    int navigationIndex = traverse.Field("navigationIndex").GetValue<int>();

                    skillSelectedBorder.transform.position = skillBorders[navigationIndex].transform.position;
                    skillBorders[6].color = Color.white;
                    swapText.gameObject.SetActive(false);
                    swapPrompt.SetActive(true);
                }

                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(EquipMenu), nameof(EquipMenu.SkillClicked))]
        public static class EquipMenuSkillClickedPatch
        {
            public static bool Prefix(int slotNumber)
            {
                return slotNumber >= 6 || SlotManager.IsSlotUnlocked(slotNumber);
            }
        }

        [HarmonyPatch(typeof(EquipMenu), nameof(EquipMenu.SkillHovered))]
        public static class EquipMenuSkillHoveredPatch
        {
            public static bool Prefix(int slotNumber)
            {
                return slotNumber >= 6 || SlotManager.IsSlotUnlocked(slotNumber);
            }
        }

        // Prevent using a skill slot that is locked in-game
        [HarmonyPatch(typeof(ChaosInputDevice))]
        public static class ChaosInputDeviceBlockLockedSlotsPatch
        {
            private static bool IsLockedSkillButton(string buttonName)
            {
                if (string.IsNullOrEmpty(buttonName) || !buttonName.StartsWith("Skill"))
                    return false;

                if (int.TryParse(buttonName.Substring(5), out int slotIndex))
                {
                    return !SlotManager.IsSlotUnlocked(slotIndex);
                }

                return false;
            }

            [HarmonyPatch(nameof(ChaosInputDevice.GetButton), new[] { typeof(string) })]
            [HarmonyPrefix]
            public static bool GetButton_Prefix(string buttonName, ref bool __result)
            {
                if (IsLockedSkillButton(buttonName))
                {
                    __result = false;
                    return false;
                }
                return true;
            }

            [HarmonyPatch(nameof(ChaosInputDevice.GetButtonDown), new[] { typeof(string) })]
            [HarmonyPrefix]
            public static bool GetButtonDown_Prefix(string buttonName, ref bool __result)
            {
                if (IsLockedSkillButton(buttonName))
                {
                    __result = false;
                    return false;
                }
                return true;
            }

            [HarmonyPatch(nameof(ChaosInputDevice.GetButtonUp), new[] { typeof(string) })]
            [HarmonyPrefix]
            public static bool GetButtonUp_Prefix(string buttonName, ref bool __result)
            {
                if (IsLockedSkillButton(buttonName))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        // Rewriting PickupSkill logic to manage locked skill slot :
        // if it's a basic or dash skill it replaces the current one and drop it (as intended)
        // if it's a signature skill and its skill slot is unlocked it replaces the current one else it goes to the stockSkill slot
        // if it's a standard skill it searches the first standard slot unlocked and empty, if there isn't it places it in the stockskill
        // if the stockskill is full, the new standard skill replace the first unlocked standard skill slot and drop the current one, 
        // if it cannot it replaces the stockskill and drop the current stock skill
        [HarmonyPatch(typeof(Player), nameof(Player.PickUpSkill))]
        public static class PlayerPickUpSkillPatch
        {
            private static int DropSkill(Player player, Player.SkillState skill)
            {
                if (skill == null) return -1;
                return Traverse.Create(player).Method("HandleSkillDrop", skill).GetValue<int>();
            }

            public static bool Prefix(Player __instance, string givenID, bool isSignature, bool isEmpowered)
            {
                int num = -1;

                if (isSignature)
                {
                    isEmpowered = true;
                }

                if (__instance.HasSkill(givenID))
                {
                    if (!isEmpowered)
                    {
                        DropSkill(__instance, __instance.GetSkill(givenID));
                    }
                    else
                    {
                        num = __instance.GetSkill(givenID).skillSlot;
                    }
                }
                else
                {
                    Player.SkillState newSkill = __instance.skillsDict[givenID];

                    if (newSkill.isBasic)
                    {
                        num = DropSkill(__instance, __instance.GetBasicSkill());
                        if (num < 0) num = 0;
                    }
                    else if (newSkill.isDash)
                    {
                        num = DropSkill(__instance, __instance.GetDashSkill());
                        if (num < 0) num = 1;
                    }
                    else if (isSignature)
                    {
                        if (SlotManager.IsSlotUnlocked(3))
                        {
                            if (__instance.assignedSkills[3] != null)
                            {
                                DropSkill(__instance, __instance.assignedSkills[3]);
                            }
                            num = 3;
                        }
                        else
                        {
                            num = -1;
                        }
                    }
                    else
                    {
                        int[] standardSlots = new int[] { 2, 4, 5 };
                        int targetSlot = -1;

                        foreach (int slot in standardSlots)
                        {
                            if (SlotManager.IsSlotUnlocked(slot) && __instance.assignedSkills[slot] == null)
                            {
                                targetSlot = slot;
                                break;
                            }
                        }

                        if (targetSlot == -1 && __instance.stockSkill == null)
                        {
                            num = -1;
                        }
                        else if (targetSlot == -1)
                        {
                            foreach (int slot in standardSlots)
                            {
                                if (SlotManager.IsSlotUnlocked(slot) && __instance.assignedSkills[slot] != null)
                                {
                                    targetSlot = slot;
                                    break;
                                }
                            }

                            if (targetSlot != -1)
                            {
                                DropSkill(__instance, __instance.assignedSkills[targetSlot]);
                            }
                        }

                        num = targetSlot;
                    }

                    if (num > -1)
                    {
                        __instance.AssignSkillSlot(num, givenID);
                    }
                    else
                    {
                        if (__instance.stockSkill != null)
                        {
                            DropSkill(__instance, __instance.stockSkill);
                        }
                        __instance.stockSkill = __instance.skillsDict[givenID];
                    }
                }

                __instance.skillsDict[givenID].SetEmpowered(isEmpowered, EmpowerStatMods.DefaultEmpowerMod);

                if (__instance.lowerHUD != null && __instance.lowerHUD.cooldownUI != null)
                {
                    __instance.lowerHUD.cooldownUI.RefreshEntries();
                }

                Sprite keySprite = CooldownUI.GetKeySpriteFromSkillSlot(num, __instance.inputDevice);
                __instance.newItemNoticeUI.Display(
                    TextManager.GetSkillName(givenID),
                    IconManager.GetSkillIcon(givenID),
                    keySprite,
                    isSkill: true,
                    isSignature,
                    isEmpowered
                );

                if (isEmpowered)
                {
                    SoundManager.PlayAudio("BuyEmpoweredArcana");
                }
                else
                {
                    SoundManager.PlayAudio("Powerup");
                }

                var traverse = Traverse.Create(__instance);
                var skillPickUpEventHandlers = traverse.Field("skillPickUpEventHandlers").GetValue<MulticastDelegate>();
                if (skillPickUpEventHandlers != null)
                {
                    skillPickUpEventHandlers.DynamicInvoke(__instance.skillsDict[givenID]);
                }

                __instance.AnnounceSkillChanged(__instance.skillsDict[givenID]);

                return false;
            }
        }

        // To prevent NPCs taking a skill from a locked skill slot, 
        // we intercept GetRandomStandardSkill to only return a random unlocked skill slot not empty
        [HarmonyPatch(typeof(Player), nameof(Player.GetRandomStandardSkill))]
        public static class PlayerGetRandomStandardSkillPatch
        {
            public static bool Prefix(Player __instance, bool allowEmp, ref Player.SkillState __result)
            {
                List<Player.SkillState> candidates = new List<Player.SkillState>();

                int[] eligibleSlots = new int[] { 2, 3, 4, 5 };

                foreach (int slot in eligibleSlots)
                {
                    if (SlotManager.IsSlotUnlocked(slot) && __instance.assignedSkills[slot] != null)
                    {
                        candidates.Add(__instance.assignedSkills[slot]);
                    }
                }

                if (__instance.stockSkill != null)
                {
                    candidates.Add(__instance.stockSkill);
                }

                if (candidates.Count == 0)
                {
                    __result = null;
                    return false;
                }

                if (!allowEmp)
                {
                    List<Player.SkillState> nonEmpowered = candidates.FindAll(s => !s.IsEmpowered);
                    if (nonEmpowered.Count > 0)
                    {
                        __result = nonEmpowered[UnityEngine.Random.Range(0, nonEmpowered.Count)];
                        return false;
                    }
                }

                __result = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                return false;
            }
        }
    }
}