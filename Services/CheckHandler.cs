namespace WoLArchipelago.Services
{
    public class CheckHandler
    {
        public static void SendNpcCheck(string locNameFormat, int maxSlots)
        {
            var checkedLocations = Plugin.AP.GetCheckedLocation();

            for (int i = 1; i <= maxSlots; i++)
            {
                string candidateName = string.Format(locNameFormat, i);
                long locId = APItemLocationDatabase.GetLocationId(candidateName);

                if (!checkedLocations.Contains(locId) && CheckLocation(candidateName))
                {
                    SoundManager.PlayAudio("MenuBuy");
                    return;
                }
            }
        }

        public static bool CheckLocation(string locationName)
        {
            long locId = APItemLocationDatabase.GetLocationId(locationName);
            if (locId != -1)
            {
                Plugin.AP.SendLocationCheck(locId);
                return true;
            }
            return false;
        }
    }
}