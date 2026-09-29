using HarmonyLib;
using UnityEngine;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Redirects game save directory calls to profile-specific Archipelago folders.
    /// </summary>
    [HarmonyPatch(typeof(GameDataManager))]
    public static class APSavePatches
    {
        [HarmonyPatch("SavePathStr", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool GetSavePathStrPrefix(ref string __result)
        {
            return OverrideSavePath(ref __result);
        }

        [HarmonyPatch("BasePathStr", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool GetBasePathStrPrefix(ref string __result)
        {
            return OverrideSavePath(ref __result);
        }

        private static bool OverrideSavePath(ref string result)
        {
            if (!string.IsNullOrEmpty(APManager.CurrentAPSavePrefix))
            {
                string apFolder = StorageService.GetProfileFolderPath();

                if (!apFolder.EndsWith("/") && !apFolder.EndsWith("\\"))
                {
                    apFolder += "/";
                }

                result = apFolder;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Flushes custom Archipelago data to disk upon application exit.
    /// </summary>
    [HarmonyPatch(typeof(Application), "Quit")]
    public class SaveOnQuitPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            DataManager.SaveData();
        }
    }
}