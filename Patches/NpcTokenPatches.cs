using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WoLArchipelago.Services;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patches to lock interaction with NPCs in special rooms inside the dungeon
    /// if the player doesn't have the required token.
    /// </summary>
    [HarmonyPatch(typeof(Npc))]
    public static class NpcTokenGatePatches
    {
        private const float MessageCooldown = 2f;

        private static readonly Dictionary<Type,  string> RequiredTokens = new()
        {
            { typeof(BankerNpc),    "TokenBanker" },
            { typeof(ShufflerNpc),  "TokenShuffler" },
            { typeof(DoctorNpc),    "TokenDoctor" },
            { typeof(TailorNpc),    "TokenTailor" },
            { typeof(CollectorNpc), "TokenCollector" },
        };

        private static float _nextMessageTime;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Npc.Interacted))]
        public static bool InteractedPrefix(Npc __instance)
        {
            if (!RequiredTokens.TryGetValue(__instance.GetType(), out string itemID))
                return true;

            if (ItemHandler.GetItemCountByName(itemID) > 0)
                return true;

            float now = Time.realtimeSinceStartup;
            if (now >= _nextMessageTime)
            {
                _nextMessageTime = now + MessageCooldown;
                GameUI.BroadcastNoticeMessage($"You haven't unlocked the {itemID} yet.");
            }

            return false;
        }
    }
}