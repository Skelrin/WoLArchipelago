using HarmonyLib;

namespace WoLArchipelago
{
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static class PlayerInitPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance == null) return;

            if (DebugController.GodModeActive && __instance.health != null)
            {
                __instance.health.healthStat.AddMod(new NumVarStatMod("GodModeHP", 99999f, 10, VarStatModType.Override, fillToNewMax: true));
                Player.goldWallet?.Deposit(9999);
                Player.platWallet?.Deposit(999);
            }
        }
    }

    [HarmonyPatch(typeof(Health), nameof(Health.TakeDamage))]
    public static class InstantKillPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Health __instance, ref AttackInfo givenAttackInfo)
        {
            if (DebugController.GodModeActive && givenAttackInfo != null && givenAttackInfo.entity is Player)
            {
                givenAttackInfo.damage = 999999;
            }
        }
    }
}