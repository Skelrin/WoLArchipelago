namespace WoLArchipelago.Services
{
    public class CheckHandler
    {
        public static void SendNpcCheck(string locName, int maxSlots)
        {
            for (int i = 1; i <= maxSlots; i++)
            {
                string candidateName = string.Format(locName, i);

                long locId = APItemLocationDatabase.GetLocationId(candidateName);

                if (locId != -1 && !Plugin.AP.GetCheckedLocation().Contains(locId))
                {
                    Plugin.AP.SendLocationCheck(locId);
                    SoundManager.PlayAudio("MenuBuy");
                    break;
                }
            }
        }
    }
}