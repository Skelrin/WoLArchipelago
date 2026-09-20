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
            string locName = $"Strange Time Keeper Slot {0}";

            Services.CheckHandler.SendNpcCheck(locName, 5);
        }

        private static void SendLoopCheck()
        {
            for (int i = 1; i <= 5; i++)
            {
                string locName = $"Strange Time Keeper Slot {i}";
                long locId = APItemLocationDatabase.GetLocationId(locName);

                if (locId != -1 && !Plugin.AP.GetCheckedLocation().Contains(locId))
                {
                    Plugin.AP.SendLocationCheck(locId);
                    SoundManager.PlayAudio("MenuBuy");
                    GameUI.BroadcastNoticeMessage($"AP Check Sent: {locName}!");
                    break;
                }
            }
        }
    }
}