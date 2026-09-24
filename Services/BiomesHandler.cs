using System.Collections.Generic;
using System.Linq;

namespace WoLArchipelago.Services
{
    public static class BiomesHandler
    {
        private static readonly Dictionary<string, string> BiomeToItemNameMap = new Dictionary<string, string>
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

        public static readonly List<string> AllBiomes = BiomeKeyMap.Keys.ToList();

        // Prevents re-running generation multiple times during the same tier transition
        private static int lastOrganizedTier = -1;

        public static List<string> GetUnlockedBiomes() => [.. BiomeKeyMap.Where(kvp => ItemHandler.IsItemUnlocked(kvp.Value)).Select(kvp => kvp.Key)];

        // Reset state tracking when returning to the Hub or starting a new run
        public static void ResetRun() => lastOrganizedTier = -1;

        public static void OrganizeLevelList()
        {
            int currentTier = GameController.tierCount;

            if (currentTier < 0 || currentTier >= GameController.maxBaseTierCount || lastOrganizedTier == currentTier)
            {
                return;
            }

            List<string> unlockedBiomes = GetUnlockedBiomes();

            if (unlockedBiomes.Count == 0) unlockedBiomes = [.. AllBiomes];

            if (currentTier == 0)
            {
                List<string> fullList = GenerateRunBiomes(unlockedBiomes);
                GameController.levelNameList = fullList;
            }
            else
            {
                // Preserve cleared tiers and re-roll remaining future slots with updated unlocked keys
                // In case of player receiving a new key during a tier
                List<string> updatedList = GameController.levelNameList.Take(currentTier).ToList();

                for (int slot = currentTier; slot < GameController.maxBaseTierCount; slot++)
                {
                    List<string> candidateBiomes = GetCandidates(unlockedBiomes, updatedList);
                    ShuffleList(candidateBiomes);
                    updatedList.Add(candidateBiomes[0]);
                }

                // Append unselected biomes to satisfy base game array bounds and lookup logic
                foreach (string biome in AllBiomes)
                {
                    if (!updatedList.Contains(biome))
                    {
                        updatedList.Add(biome);
                    }
                }

                GameController.levelNameList = updatedList;
            }

            lastOrganizedTier = currentTier;

            // Immediately rebuild UI board to mirror updated biome order
            if (GameController.loadingScreen != null && GameController.loadingScreen.gameProgressBoard != null)
            {
                GameController.loadingScreen.gameProgressBoard.InitializeTierOrders();
            }
        }

        private static List<string> GenerateRunBiomes(List<string> unlockedBiomes)
        {
            List<string> selectedBiomes = new List<string>();

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

        // Prioritizes unvisited unlocked biomes before allowing duplicate selections
        private static List<string> GetCandidates(List<string> unlockedBiomes, List<string> alreadySelected)
        {
            List<string> unused = unlockedBiomes.Where(b => !alreadySelected.Contains(b)).ToList();

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