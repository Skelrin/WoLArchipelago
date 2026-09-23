using System;
using System.Collections.Generic;
using System.Linq;

namespace WoLArchipelago.Services
{
    public static class StartingInventoryHandler
    {
        public static void ApplyArchipelagoStartingSkills()
        {
            if (DataManager.IsStartingInventoryApplied) return;
            if (Plugin.AP == null || !Plugin.AP.IsConnected) return;

            Player player = ItemHandler.GetActivePlayer();
            if (player?.skillsDict == null || player.skillsDict.Count == 0) return;

            GameData gameData = GameDataManager.gameData;

            LockStartingItems(gameData);

            if (APManager.StartingArcanaMode == 0)
            {
                DataManager.IsStartingInventoryApplied = true;
                DataManager.SaveData();
                return;
            }

            if (APManager.StartingArcanaMode == 1)
            {
                LockStartingSkillsOnDataDictionary(gameData);
                LockStartingSkillsOnPlayer(player);
                
                ElementType? targetElement = GetTargetElement();

                List<Player.SkillState> allSkills = player.skillsDict.Values.ToList();

                Player.SkillState chosenBasic = GetRandomSkill(FilterSkills(allSkills, s => s.isBasic, targetElement));
                Player.SkillState chosenDash = GetRandomSkill(FilterSkills(allSkills, s => s.isDash, targetElement));
                Player.SkillState chosenStandard = GetRandomSkill(FilterSkills(allSkills, s => !s.isBasic && !s.isDash && !s.hasSignatureVariant, targetElement));
                Player.SkillState chosenSignature = GetRandomSkill(FilterSkills(allSkills, s => s.hasSignatureVariant, targetElement));

                Plugin.Log.LogInfo($"Chosen Skills: {chosenBasic?.skillID} | {chosenDash?.skillID} | {chosenStandard?.skillID} | {chosenSignature?.skillID}");

                if (player.assignedSkills == null || player.assignedSkills.Length < 4)
                {
                    player.assignedSkills = new Player.SkillState[4];
                }

                AssignSkillToSlot(player, gameData, 0, chosenBasic, false);
                AssignSkillToSlot(player, gameData, 1, chosenDash, false);
                AssignSkillToSlot(player, gameData, 2, chosenStandard, false);
                AssignSkillToSlot(player, gameData, 3, chosenSignature, true);

                DataManager.IsStartingInventoryApplied = true;
                DataManager.SaveData();
                GameUI.RefreshCDUI();
            }
        }

        private static ElementType? GetTargetElement()
        {
            if (APManager.ElementLicensesMode != 1 || string.IsNullOrEmpty(APManager.StartingElement))
            {
                return null;
            }

            string startingElement = APManager.StartingElement;
            foreach (string name in Enum.GetNames(typeof(ElementType)))
            {
                if (string.Equals(name, startingElement, StringComparison.OrdinalIgnoreCase))
                {
                    return (ElementType)Enum.Parse(typeof(ElementType), name);
                }
            }

            return null;
        }

        private static List<Player.SkillState> FilterSkills(List<Player.SkillState> skills, Func<Player.SkillState, bool> categoryPredicate, ElementType? targetElement)
        {
            var match = skills.Where(categoryPredicate).ToList();

            if (targetElement.HasValue)
            {
                var elementalMatch = match.Where(s => s.element == targetElement.Value).ToList();
                if (elementalMatch.Count > 0)
                {
                    return elementalMatch;
                }
            }

            return match;
        }

        private static Player.SkillState GetRandomSkill(List<Player.SkillState> pool)
        {
            if (pool == null || pool.Count == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        private static void AssignSkillToSlot(Player player, GameData gameData, int slotIndex, Player.SkillState skill, bool isSignature)
        {
            if (skill == null) return;

            skill.isUnlocked = true;
            if (isSignature)
            {
                skill.signatureUnlocked = true;
                skill.isSignature = true;
            }

            player.assignedSkills[slotIndex] = skill;

            if (player.playerData?.skills != null && player.playerData.skills.Length > slotIndex)
            {
                player.playerData.skills[slotIndex] = skill.skillID;
            }

            if (gameData?.skillDataDictionary != null)
            {
                if (!gameData.skillDataDictionary.TryGetValue(skill.skillID, out var storedData))
                {
                    storedData = new GameData.StoredSkillData();
                    gameData.skillDataDictionary[skill.skillID] = storedData;
                }

                storedData.isUnlocked = true;
                if (isSignature)
                {
                    storedData.signatureUnlocked = true;
                }
            }
        }

        private static void SetStoredSkillLockState(GameData gameData, string skillId, bool isUnlocked, bool signatureUnlocked)
        {
            if (!gameData.skillDataDictionary.TryGetValue(skillId, out var storedData))
            {
                storedData = new GameData.StoredSkillData();
                gameData.skillDataDictionary[skillId] = storedData;
            }
            storedData.isUnlocked = isUnlocked;
            storedData.signatureUnlocked = signatureUnlocked;
        }

        public static void LockStartingSkillsOnDataDictionary(GameData gameData)
        {
            if (gameData?.skillDataDictionary == null) return;

            foreach (string skillId in Globals.startingSkillIDList)
            {
                SetStoredSkillLockState(gameData, skillId, isUnlocked: false, signatureUnlocked: false);
            }

            foreach (string overdriveId in Globals.startingOverdriveIDList)
            {
                SetStoredSkillLockState(gameData, overdriveId, isUnlocked: false, signatureUnlocked: false);
            }
        }

        public static void LockStartingSkillsOnPlayer(Player player)
        {
            if (player?.skillsDict == null) return;

            foreach (string skillId in Globals.startingSkillIDList)
            {
                if (player.skillsDict.TryGetValue(skillId, out var skillState))
                {
                    skillState.isUnlocked = false;
                }
            }

            foreach (string overdriveId in Globals.startingOverdriveIDList)
            {
                if (player.skillsDict.TryGetValue(overdriveId, out var skillState))
                {
                    skillState.isUnlocked = false;
                    skillState.signatureUnlocked = false;
                }
            }
        }

        public static void LockStartingItems(GameData gameData)
        {
            gameData.UpdateItemDataEntry(PlayerStartItem.staticID, false);
            gameData.UpdateItemDataEntry(BuffWithFriendship.staticID, false);
            gameData.UpdateItemDataEntry(WaterChargeFamiliarItem.staticID, false);
        }
    }
}