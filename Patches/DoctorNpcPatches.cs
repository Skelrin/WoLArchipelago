using HarmonyLib;

namespace WoLArchipelago.Patches
{
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