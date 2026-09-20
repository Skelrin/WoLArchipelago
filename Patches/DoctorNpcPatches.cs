using HarmonyLib;

namespace WoLArchipelago.Patches
{
    [HarmonyPatch(typeof(DoctorNpc))]
    public class DoctorNpcPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("WritePrescription")]
        public static bool WritePrescriptionPrefix(DoctorNpc __instance, bool useToken)
        {
            Traverse traverse = Traverse.Create(__instance);

            traverse.Field("prescriptionWritten").SetValue(true);

            DialogManager.dialogDict[__instance.myDialogID].CurrentIndex = 4;

            Player player = __instance.player;

            int healthDropCount = traverse.Field<int>("healthDropCount").Value;

            if (useToken)
            {
                player.inventory.RemoveItem("TokenDoctor", forceOverride: false, showNotice: true);
                LootManager.DropHealth(__instance.transform.position, healthDropCount + 2);
            }
            else
            {
                player.RemoveSkill(player.GetRandomStandardSkill(allowEmp: false) ?? player.GetRandomStandardSkill());
                LootManager.DropHealth(__instance.transform.position, healthDropCount);
            }

            RunData.miscNurseRoomFound = true;

            string locName = $"Doctor Song Slot {0}";

            Services.CheckHandler.SendNpcCheck(locName, 10);

            DialogManager.dialogDict[__instance.myDialogID].CurrentIndex = 5;

            return false;
        }
    }
}