using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches customizing the pause menu UI to add a button to teleport to the hub.
    /// </summary>
    public static class PauseMenuPatches
    {
        [HarmonyPatch(typeof(PauseMenuUI), "GetOptionRefs")]
        public class PauseMenuGetOptionRefsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(PauseMenuUI __instance, ref List<Text> ___pauseMenuOptions)
            {
                try
                {
                    Transform resumeBtn = __instance.transform.Find("Resume");
                    Transform titleScreenBtn = __instance.transform.Find("TitleScreen");
                    Transform optionsBtn = __instance.transform.Find("Options");
                    Transform quitBtn = __instance.transform.Find("Quit");

                    if (resumeBtn == null || titleScreenBtn == null || optionsBtn == null || quitBtn == null)
                    {
                        return;
                    }

                    GameObject hubObj = Object.Instantiate(resumeBtn.gameObject, resumeBtn.parent);
                    hubObj.name = "TeleportToHub";
                    Transform hubBtn = hubObj.transform;

                    Text hubText = hubBtn.Find("Text")?.GetComponent<Text>();
                    if (hubText != null)
                    {
                        hubText.text = "TELEPORT TO HUB";
                        ___pauseMenuOptions.Insert(1, hubText);
                    }

                    float origSpacing = titleScreenBtn.localPosition.y - resumeBtn.localPosition.y;
                    float adjustedSpacing = origSpacing * 0.85f;

                    Vector3 startPos = resumeBtn.localPosition;
                    float startY = startPos.y - (adjustedSpacing * 0.5f);

                    resumeBtn.localPosition = new Vector3(startPos.x, startY, startPos.z);
                    hubBtn.localPosition = new Vector3(startPos.x, startY + adjustedSpacing, startPos.z);
                    titleScreenBtn.localPosition = new Vector3(startPos.x, startY + (adjustedSpacing * 2), startPos.z);
                    optionsBtn.localPosition = new Vector3(startPos.x, startY + (adjustedSpacing * 3), startPos.z);
                    quitBtn.localPosition = new Vector3(startPos.x, startY + (adjustedSpacing * 4), startPos.z);

                    Transform[] buttons = [resumeBtn, hubBtn, titleScreenBtn, optionsBtn, quitBtn];
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        GameObject btnObj = buttons[i].gameObject;
                        
                        var existingHandler = btnObj.GetComponent<PauseMenuButtonMouseHandler>();
                        if (existingHandler != null)
                        {
                            Object.Destroy(existingHandler);
                        }

                        var mouseHandler = btnObj.AddComponent<PauseMenuButtonMouseHandler>();
                        mouseHandler.pauseMenuUI = __instance;
                        mouseHandler.optionIndex = i;
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogError($"[AP] Error modifying pause menu layout: {ex}");
                }
            }
        }

        public class PauseMenuButtonMouseHandler : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
        {
            public PauseMenuUI pauseMenuUI;
            public int optionIndex;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (pauseMenuUI != null && pauseMenuUI.hasFocus)
                {
                    pauseMenuUI.SelectOption(optionIndex);
                }
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (pauseMenuUI != null && pauseMenuUI.hasFocus)
                {
                    pauseMenuUI.SelectOption(optionIndex);
                    pauseMenuUI.OptionWasClicked();
                }
            }
        }

        [HarmonyPatch(typeof(PauseMenuUI), nameof(PauseMenuUI.TogglePause))]
        public class PauseMenuTogglePausePatch
        {
            [HarmonyPostfix]
            public static void Postfix(PauseMenuUI __instance, ref int ___optionsCount)
            {
                if (__instance.paused)
                {
                    ___optionsCount = 5;
                }
            }
        }

        [HarmonyPatch(typeof(PauseMenuUI), "Update")]
        public class PauseMenuUpdatePatch
        {
            private static bool indexWasShifted = false;

            [HarmonyPrefix]
            public static bool Prefix(
                PauseMenuUI __instance, 
                ref int ___currentIndex, 
                ref bool ___optionClicked, 
                bool ___paused)
            {
                indexWasShifted = false;

                if (!___paused)
                {
                    return true;
                }

                Traverse.Create(__instance).Method("HandleConfirmInput").GetValue();

                if (___optionClicked)
                {
                    if (___currentIndex == 1)
                    {
                        SoundManager.PlayConfirmAudio();
                        __instance.TogglePause();
                        __instance.gameObject.SetActive(false);

                        ___optionClicked = false;

                        TeleportToHub();
                        return false;
                    }
                    else if (___currentIndex >= 2)
                    {
                        ___currentIndex -= 1;
                        indexWasShifted = true;
                    }
                }

                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(ref int ___currentIndex)
            {
                if (indexWasShifted)
                {
                    ___currentIndex += 1;
                    indexWasShifted = false;
                }
            }

            private static void TeleportToHub()
            {
                GameDataManager.Save();

                Level.isBossRushMode = false;
                Level.removeAllPits = false;
                Level.resetUsedRooms = true;
                HealStoreItem.healBuyCount = 0;
                SkillStoreItem.empoweredBuyCount = 0;
                if (Player.goldWallet != null)
                {
                    Player.goldWallet.balance = 0;
                }
                HubEventHandler.cursePurchased = false;
                GameController.stageCount = 0;
                GameController.tierCount = 0;
                GameController.loopCount = 0;
                GameController.NextLevelName = string.Empty;
                RunData.Reset();
                EnemyGlobalStatManager.ResetScaling();
                GameController.resetHubVars = true;

                GameController.LoadLevel("Hub");
                ItemHandler.SyncPlayerMaxHP(ItemHandler.GetActivePlayer(), true);
            }
        }
    }
}