using System;
using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to detect when player successfully trigger loop from Strange Time Keeper to send an Archipelago check.
    /// </summary>
    [HarmonyPatch(typeof(LoopNpc))]
    public class LoopNpcPatches
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Npc), "Start")]
        public static void CallBaseStart(Npc instance)
        {
            throw new NotImplementedException();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        public static bool StartPrefix(LoopNpc __instance)
        {
            CallBaseStart(__instance);

            return false; 
        }
        
        [HarmonyPrefix]
        [HarmonyPatch("InitiateLoop")]
        public static void InitiateLoopPrefix()
        {
            string locName = "Strange Time Keeper Slot {0}";

            Services.CheckHandler.SendNpcCheck(locName, 2);
        }
    }
}