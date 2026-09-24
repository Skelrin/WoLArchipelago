using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(TitleScreen), "Start")]
    public class TitleScreenStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TitleScreen __instance)
        {
            try
            {
                Transform singleBtn = __instance.transform.Find("TitleMenu/SinglePlayer");
                Transform coopBtn = __instance.transform.Find("TitleMenu/TwoPlayers");
                Transform versusBtn = __instance.transform.Find("TitleMenu/Versus");
                Transform optionsBtn = __instance.transform.Find("TitleMenu/Options");
                Transform creditsBtn = __instance.transform.Find("TitleMenu/Credits");
                Transform exitBtn = __instance.transform.Find("TitleMenu/Exit");

                if (singleBtn != null)
                {
                    Text textComp = singleBtn.GetComponent<Text>();
                    if (textComp != null)
                    {
                        textComp.text = "CONNECT TO ARCHIPELAGO";
                    }
                }

                if (singleBtn != null && coopBtn != null && optionsBtn != null && creditsBtn != null && exitBtn != null)
                {
                    float spacing = coopBtn.localPosition.y - singleBtn.localPosition.y;

                    optionsBtn.localPosition = new Vector3(optionsBtn.localPosition.x, singleBtn.localPosition.y + spacing, optionsBtn.localPosition.z);
                    creditsBtn.localPosition = new Vector3(creditsBtn.localPosition.x, singleBtn.localPosition.y + (spacing * 2), creditsBtn.localPosition.z);
                    exitBtn.localPosition = new Vector3(exitBtn.localPosition.x, singleBtn.localPosition.y + (spacing * 3), exitBtn.localPosition.z);
                }

                coopBtn?.gameObject.SetActive(false);
                versusBtn?.gameObject.SetActive(false);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[AP UI] Error modifying title screen: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(TitleScreen), nameof(TitleScreen.SelectMenuIndex), new System.Type[] { typeof(int), typeof(bool) })]
    public class TitleScreenNavigationPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref int newIndex)
        {
            if (newIndex == 1)
            {
                newIndex = 3;
            }
            else if (newIndex == 2)
            {
                newIndex = 0;
            }
        }
    }

    [HarmonyPatch(typeof(TitleScreen), nameof(TitleScreen.ConfirmMenuOption))]
    public class TitleScreenConfirmPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TitleScreen __instance)
        {
            if (__instance.currentState == TitleScreen.TitleScreenState.Menu && __instance.currentMenuIndex == 0)
            {
                SoundManager.PlayConfirmAudio();

                if (Plugin.AP != null && Plugin.AP.IsConnected)
                {
                    Plugin.StartAPRun();
                }
                else
                {
                    ArchipelagoUI.Instance?.Toggle();
                }

                return false;
            }

            return true;
        }
    }
}