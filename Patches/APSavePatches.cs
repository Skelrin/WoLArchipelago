using System.IO;
using HarmonyLib;
using UnityEngine;

namespace WoLArchipelago
{
    [HarmonyPatch(typeof(GameDataManager))]
    public static class APSavePatches
    {
        [HarmonyPatch("SavePathStr", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool GetSavePathStrPrefix(ref string __result)
        {
            if (!string.IsNullOrEmpty(APManager.CurrentAPSavePrefix))
            {
                string apFolder = Path.Combine(Application.persistentDataPath, "AP_Saves");
                apFolder = Path.Combine(apFolder, APManager.CurrentAPSavePrefix);

                if (!apFolder.EndsWith("/") && !apFolder.EndsWith("\\"))
                {
                    apFolder += "/";
                }

                __result = apFolder;
                return false;
            }
            return true;
        }

        [HarmonyPatch("BasePathStr", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool GetBasePathStrPrefix(ref string __result)
        {
            if (!string.IsNullOrEmpty(APManager.CurrentAPSavePrefix))
            {
                string apFolder = Path.Combine(Application.persistentDataPath, "AP_Saves");
                apFolder = Path.Combine(apFolder, APManager.CurrentAPSavePrefix);

                if (!apFolder.EndsWith("/") && !apFolder.EndsWith("\\"))
                {
                    apFolder += "/";
                }

                __result = apFolder;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Application), "Quit")]
    public class SaveOnQuitPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            Services.DataManager.SaveData();
        }
    }
}