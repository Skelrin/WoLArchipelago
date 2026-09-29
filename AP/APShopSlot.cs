using UnityEngine;

namespace WoLArchipelago
{
    /// <summary>
    /// Represent an Archipelago location.
    /// </summary>
    public class APShopSlot : MonoBehaviour
    {
        public string LocationName { get; private set; }
        public long LocationId { get; private set; }

        public void Initialize(string locationName)
        {
            LocationName = locationName;
            LocationId = APItemLocationDatabase.GetLocationId(locationName);
        }
    }
}