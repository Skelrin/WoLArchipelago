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
                Transform coopBtn = __instance.transform.Find("TitleMenu/TwoPlayers");
                Transform versusBtn = __instance.transform.Find("TitleMenu/Versus");

                if (coopBtn != null) coopBtn.gameObject.SetActive(false);
                if (versusBtn != null) versusBtn.gameObject.SetActive(false);

                Transform singleBtn = __instance.transform.Find("TitleMenu/SinglePlayer");
                if (singleBtn != null)
                {
                    Text textComp = singleBtn.GetComponent<Text>();
                    if (textComp != null)
                    {
                        textComp.text = "CONNECT TO ARCHIPELAGO";
                    }
                }

                Plugin.Log.LogInfo("[AP UI] Title screen modified successfully!");
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
        public static void Prefix(TitleScreen __instance, ref int newIndex)
        {
            if (newIndex == 1 || newIndex == 2)
            {
                if (__instance.currentMenuIndex == 0)
                {
                    newIndex = 3;
                }
                else if (__instance.currentMenuIndex == 3)
                {
                    newIndex = 0;
                }
                else
                {
                    newIndex = 0;
                }
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