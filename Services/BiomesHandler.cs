using System.Collections.Generic;
using System.Linq;

namespace WoLArchipelago.Services
{
    /// <summary>
    /// Manages biome unlocking and shuffling based on biome keys unlocked.
    /// </summary>
    public class BiomesHandler
    {
        private static readonly Dictionary<string, string> BiomeToItemNameMap = new()
        {
            { "Fire", "Fire Biome Key" },
            { "Ice", "Water Biome Key" },
            { "Earth", "Earth Biome Key" },
            { "Air", "Air Biome Key" },
            { "Lightning", "Lightning Biome Key" }
        };

        public static readonly Dictionary<string, long> BiomeKeyMap = BiomeToItemNameMap.ToDictionary(
            kvp => kvp.Key,
            kvp => APItemLocationDatabase.GetItemId(kvp.Value)
        );

        public static readonly List<string> AllBiomes = [.. BiomeKeyMap.Keys];

        private static int _lastOrganizedTier = -1;

        public static List<string> GetUnlockedBiomes()
        {
            List<string> unlocked = [];
            foreach (KeyValuePair<string, long> kvp in BiomeKeyMap)
            {
                if (ItemHandler.IsItemUnlocked(kvp.Value))
                {
                    unlocked.Add(kvp.Key);
                }
            }
            return unlocked;
        }

        public static void ResetRun()
        {
            _lastOrganizedTier = -1;
        }

        /// <summary>
        /// Generates and assigns a randomized biome progression order for the current run based on unlocked keys.
        /// </summary>
        public static void OrganizeLevelList()
        {
            int currentTier = GameController.tierCount;

            if (currentTier < 0 || currentTier >= GameController.maxBaseTierCount || _lastOrganizedTier != -1)
            {
                return;
            }

            List<string> unlockedBiomes = GetUnlockedBiomes();
            if (unlockedBiomes.Count == 0)
            {
                unlockedBiomes = [.. AllBiomes];
            }

            List<string> fullList = GenerateRunBiomes(unlockedBiomes);
            GameController.levelNameList = fullList;

            _lastOrganizedTier = currentTier;

            if (GameController.loadingScreen != null && GameController.loadingScreen.gameProgressBoard != null)
            {
                GameController.loadingScreen.gameProgressBoard.InitializeTierOrders();
            }
        }

        private static List<string> GenerateRunBiomes(List<string> unlockedBiomes)
        {
            List<string> selectedBiomes = [];

            for (int slot = 0; slot < GameController.maxBaseTierCount; slot++)
            {
                List<string> candidates = GetCandidates(unlockedBiomes, selectedBiomes);
                ShuffleList(candidates);
                selectedBiomes.Add(candidates[0]);
            }

            foreach (string biome in AllBiomes)
            {
                if (!selectedBiomes.Contains(biome))
                {
                    selectedBiomes.Add(biome);
                }
            }

            return selectedBiomes;
        }

        // Prioritizes unvisited unlocked biomes before allowing duplicate selections in a same dungeon run
        private static List<string> GetCandidates(List<string> unlockedBiomes, List<string> alreadySelected)
        {
            List<string> unused = [];
            for (int i = 0; i < unlockedBiomes.Count; i++)
            {
                string biome = unlockedBiomes[i];
                if (!alreadySelected.Contains(biome))
                {
                    unused.Add(biome);
                }
            }

            return unused.Count > 0 ? unused : [.. unlockedBiomes];
        }

        private static void ShuffleList(List<string> listToShuffle)
        {
            for (int currentIndex = listToShuffle.Count - 1; currentIndex > 0; currentIndex--)
            {
                int randomIndexToSwap = UnityEngine.Random.Range(0, currentIndex + 1);

                string temporaryElement = listToShuffle[randomIndexToSwap];
                listToShuffle[randomIndexToSwap] = listToShuffle[currentIndex];
                listToShuffle[currentIndex] = temporaryElement;
            }
        }
    }
}