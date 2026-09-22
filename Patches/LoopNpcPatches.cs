using System;
using HarmonyLib;

namespace WoLArchipelago.Patches
{
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

            Services.CheckHandler.SendNpcCheck(locName, 5);
        }
    }
}