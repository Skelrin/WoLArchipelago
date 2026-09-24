using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(GameProgressBoard), nameof(GameProgressBoard.InitializeTierOrders))]
    public static class GameProgressBoardInitializeTierOrdersPatch
    {
        private static readonly List<GameObject> createdUIBiomesClones = new List<GameObject>();

        [HarmonyPrefix]
        public static bool Prefix(
            GameProgressBoard __instance,
            bool ___initializationReady,
            ref int ___curMaxStagesPerTier,
            ref int ___curMaxTierCount,
            ref int ___curTotalStageCount,
            ref int ___curFinalStageIndex,
            ref Vector2[] ___curStagePositions,
            ref RectTransform[] ___stagesInOrder,
            ref Image[] ___dimmersInOrder,
            ref RectTransform[] ___bossesInOrder,
            Dictionary<string, RectTransform> ___bossCircles,
            Dictionary<string, Image> ___bossDimmers,
            Dictionary<string, RectTransform[]> ___levelMarkers,
            Dictionary<string, Image[]> ___levelDimmers)
        {
            if (!___initializationReady)
            {
                return false;
            }

            // Clean up clones from the previous loading screen before rebuilding the new one
            foreach (GameObject clone in createdUIBiomesClones)
            {
                if (clone != null)
                {
                    Object.Destroy(clone);
                }
            }
            createdUIBiomesClones.Clear();

            // Run original private method to set loading screen board layout variables
            Traverse.Create(__instance).Method("SetModeVars").GetValue();
            __instance.HideRegularStages();

            ___curTotalStageCount = ___curMaxTierCount * ___curMaxStagesPerTier + 2;
            ___curFinalStageIndex = ___curTotalStageCount - 1;

            ___stagesInOrder[___curFinalStageIndex] = __instance.finalBossCircle;
            ___dimmersInOrder[___curFinalStageIndex] = __instance.finalBossDimmer;

            // Duplicate tracking: base game assumes each biome appears only once 
            // but if player have less than 3 Biome Keys, it can appear more than once
            HashSet<string> seenBiomes = new HashSet<string>();

            for (int i = 1; i < ___curTotalStageCount - 1; i += ___curMaxStagesPerTier)
            {
                int num = (i - 1) / ___curMaxStagesPerTier;
                string biomeName = NextLevelLoader.GetCurrentTierScene(num);

                if (string.IsNullOrEmpty(biomeName) || !___bossCircles.ContainsKey(biomeName))
                {
                    continue;
                }

                RectTransform bossCircle = ___bossCircles[biomeName];
                Image bossDimmer = ___bossDimmers[biomeName];
                RectTransform[] markers = ___levelMarkers[biomeName];
                Image[] dimmers = ___levelDimmers[biomeName];

                if (seenBiomes.Contains(biomeName))
                {
                    // Clone the full container so repeated biomes get their own Transform in the scene
                    RectTransform originalBossCircle = bossCircle;
                    bossCircle = Object.Instantiate(originalBossCircle, originalBossCircle.parent);
                    createdUIBiomesClones.Add(bossCircle.gameObject);

                    // Rebind cloned child image and stage markers inside the new parent
                    Image origDimmer = bossDimmer;
                    Transform clonedDimmerTr = FindChildRecursive(bossCircle, origDimmer.gameObject.name);
                    bossDimmer = clonedDimmerTr != null ? clonedDimmerTr.GetComponent<Image>() : bossCircle.GetComponent<Image>();

                    RectTransform[] clonedMarkers = new RectTransform[markers.Length];
                    Image[] clonedDimmers = new Image[dimmers.Length];

                    for (int m = 0; m < markers.Length; m++)
                    {
                        if (markers[m] != null)
                        {
                            Transform markerTr = FindChildRecursive(bossCircle, markers[m].gameObject.name);
                            if (markerTr != null)
                            {
                                clonedMarkers[m] = markerTr.GetComponent<RectTransform>();
                                clonedDimmers[m] = markerTr.GetComponent<Image>();
                            }
                        }
                    }

                    markers = clonedMarkers;
                    dimmers = clonedDimmers;
                }
                else
                {
                    seenBiomes.Add(biomeName);
                }

                if (__instance.visible)
                {
                    bossCircle.gameObject.SetActive(true);
                }
                bossCircle.anchoredPosition = ___curStagePositions[num];

                int num2 = dimmers.Length - 1;
                int num3 = ___curMaxStagesPerTier - 2;

                while (num2 >= 0)
                {
                    if (num3 >= 0)
                    {
                        ___dimmersInOrder[i + num3] = dimmers[num2];
                        ___stagesInOrder[i + num3] = markers[num2];
                        if (dimmers[num2] != null) dimmers[num2].gameObject.SetActive(true);
                        if (markers[num2] != null) markers[num2].gameObject.SetActive(true);
                    }
                    else
                    {
                        if (dimmers[num2] != null) dimmers[num2].gameObject.SetActive(false);
                        if (markers[num2] != null) markers[num2].gameObject.SetActive(false);
                    }
                    num2--;
                    num3--;
                }

                ___dimmersInOrder[i + ___curMaxStagesPerTier - 1] = bossDimmer;
                ___stagesInOrder[i + ___curMaxStagesPerTier - 1] = bossCircle;
                ___bossesInOrder[num] = bossCircle;
            }

            // Force UI layout update before moving player piece (prevents snaps to 0,0,0)
            Canvas.ForceUpdateCanvases();
            return false;
        }

        // Recursive search required because stage markers are nested under the boss circle in Canvas hierarchy
        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(LoadingScreen), nameof(LoadingScreen.Enable))]
    public static class LoadingScreenEnablePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            BiomesHandler.OrganizeLevelList();
        }
    }
}