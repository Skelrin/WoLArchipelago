using HarmonyLib;

namespace WoLArchipelago.Patches
{
    /// <summary>
    /// Patch to detect when player successfully give an arcana to Doctor Song to send an Archipelago check.
    /// </summary>
    [HarmonyPatch(typeof(DoctorNpc))]
    public class DoctorNpcPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("WritePrescription")]
        public static void WritePrescriptionPostfix()
        {
            string locName = "Doctor Song Slot {0}";
            Services.CheckHandler.SendNpcCheck(locName, 10);
        }
    }
}