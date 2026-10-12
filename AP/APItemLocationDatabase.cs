using System.Collections.Generic;

namespace WoLArchipelago
{
    /// <summary>
    /// Manages Archipelago Items and Locations names and ids to be synchronized with Items.py and Locations.py
    /// </summary>
    public static class APItemLocationDatabase
    {

        public static readonly Dictionary<long, string> ItemIdToName = [];
        public static readonly Dictionary<string, long> ItemNameToId = [];

        public static readonly Dictionary<long, string> LocationIdToName = [];
        public static readonly Dictionary<string, long> LocationNameToId = [];

        private static long baseLocationCode = 871130000;

        static APItemLocationDatabase()
        {
            InitializeItems();
            InitializeLocations();
        }

        public static string GetItemName(long id)
        {
            string name;
            return ItemIdToName.TryGetValue(id, out name) ? name : "Unknown Item";
        }

        public static long GetItemId(string name)
        {
            long id;
            return ItemNameToId.TryGetValue(name, out id) ? id : -1;
        }

        public static long GetLocationId(string name)
        {
            long id;
            return LocationNameToId.TryGetValue(name, out id) ? id : -1;
        }

        private static void AddItem(string name, long id)
        {
            ItemNameToId[name] = id;
            ItemIdToName[id] = name;
        }

        private static void AddLocation(string name)
        {
            LocationNameToId[name] = baseLocationCode;
            LocationIdToName[baseLocationCode] = name;
            baseLocationCode++;
        }

        private static void AddMultipleLocations(string baseName, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                AddLocation(baseName + " " + i);
            }
        }

        private static void InitializeItems()
        {
            // Progression
            AddItem("Chaos Fragment", 871120001);
            AddItem("Boss Key", 871120002);
            AddItem("Shop Upgrade", 871120003);

            // Biome Keys
            AddItem("Fire Biome Key", 871120010);
            AddItem("Water Biome Key", 871120011);
            AddItem("Earth Biome Key", 871120012);
            AddItem("Air Biome Key", 871120013);
            AddItem("Lightning Biome Key", 871120014);

            // Outfits
            AddItem("Outfit: Hope", 871121001);
            AddItem("Outfit: Patience", 871121002);
            AddItem("Outfit: Vigor", 871121003);
            AddItem("Outfit: Grit", 871121004);
            AddItem("Outfit: Greed", 871121005);
            AddItem("Outfit: Pink", 871121006);
            AddItem("Outfit: Pace", 871121007);
            AddItem("Outfit: Tempo", 871121008);
            AddItem("Outfit: Switch", 871121009);
            AddItem("Outfit: Awe", 871121010);
            AddItem("Outfit: Fury", 871121011);
            AddItem("Outfit: Rule", 871121012);
            AddItem("Outfit: Level", 871121013);
            AddItem("Outfit: Venture", 871121014);
            AddItem("Outfit: Fall", 871121015);
            AddItem("Outfit: Pride", 871121016);

            // Licenses & Slots
            AddItem("Standard Arcana Slot", 871122001);
            AddItem("Signature Arcana Slot", 871122002);
            AddItem("Bonus Arcana Slot 1", 871122003);
            AddItem("Bonus Arcana Slot 2", 871122004);

            AddItem("Fire Element License", 871122010);
            AddItem("Water Element License", 871122011);
            AddItem("Earth Element License", 871122012);
            AddItem("Air Element License", 871122013);
            AddItem("Lightning Element License", 871122014);

            // Upgrades
            AddItem("Max HP Boost", 871123001);
            AddItem("Relic Slot Upgrade", 871123002);

            // Fillers
            AddItem("Heal Orb", 871123003);
            AddItem("Gold Pack", 871123004);
            AddItem("Chaos Gems Pack", 871123005);

            // Traps
            AddItem("Cursed Trap", 871124001);
            AddItem("Mob Trap", 871124002);
            AddItem("Heal Trap", 871124003);
            AddItem("Gold Trap", 871124004);

            // Generic Relics
            AddItem("Relic Bundle Tier 1", 871125001);
            AddItem("Relic Bundle Tier 2", 871125002);
            AddItem("Relic Bundle Tier 3", 871125003);
            AddItem("Relic Bundle Tier 4", 871125004);
            AddItem("Relic Bundle Tier 5", 871125005);

            // NPC Relics
            AddItem("TokenBanker", 871125006);
            AddItem("TokenShuffler", 871125007);
            AddItem("TokenDoctor", 871125008);
            AddItem("TokenCursed", 871125009);
            AddItem("TokenTailor", 871125010);
            AddItem("TokenCollector", 871125011);
            AddItem("TokenPinata", 871125012);

            // Generic Arcanas
            AddItem("Arcana Bundle Tier 1", 871126001);
            AddItem("Arcana Bundle Tier 2", 871126002);
            AddItem("Arcana Bundle Tier 3", 871126003);
            AddItem("Arcana Bundle Tier 4", 871126004);
            AddItem("Arcana Bundle Tier 5", 871126005);
        }

        private static void InitializeLocations()
        {
            // BOSSES
            string[] bosses =
            [
                "AirBoss",
                "EarthBoss",
                "FireBoss",
                "IceBoss",
                "LightningBoss"
            ];
            foreach (string boss in bosses)
            {
                AddLocation(boss + " Defeated");
            }
            
            // Completion Goal
            AddLocation("FinalBoss Defeated");

            // MINIBOSSES
            string[] minibosses =
            {
                "SuperArcher", 
                "SuperCoffin", 
                "SuperKnight", 
                "SuperLancer",
                "SuperMage", 
                "SuperRogue", 
                "SuperSummoner"
            };

            foreach (string miniBoss in minibosses)
            {
                AddLocation(miniBoss + " Defeated");
                AddLocation(miniBoss + " Defeated 5 times");
            }

            // BASE ENEMIES
            string[] enemies =
            {
                "Blob",
                "BlobRoller",
                "BlobSpitter",
                "Ghoul",
                "GhoulLarge",
                "Coffin",
                "EnemyTurret",
                "Archer",
                "Knight",
                "Mage",
                "Rogue",
                "Lancer",
                "Summoner"
            };
            foreach (string enemy in enemies)
            {
                AddLocation(enemy + " Defeated 25 times");
                AddLocation(enemy + " Defeated 50 times");
            }

            AddLocation("MovingStatue Defeated 10 times");
            AddLocation("MimicEnemy Defeated 5 times");
            AddLocation("Pinata Defeated");

            // ELEMENTAL ENEMIES
            string[] elements = ["Fire", "Water", "Earth", "Air", "Lightning"];
            foreach (string element in elements)
            {
                AddLocation(element + " Enemies Defeated 25 times");
                AddLocation(element + " Enemies Defeated 50 times");
            }

            // SPAWN SHOPS
            AddMultipleLocations("Outfit Shop Slot", 16);
            AddMultipleLocations("Relic Shop Slot", 32);
            AddMultipleLocations("Arcana Shop Slot", 32);

            // DUNGEON NPCS & EVENTS
            AddMultipleLocations("Doctor Song Slot", 6);
            AddMultipleLocations("Savile the Tailor Slot", 6);
            AddMultipleLocations("Nox the Unfortunate Slot", 20);
            AddMultipleLocations("Nocturne the Cardist Slot", 6);
            AddMultipleLocations("Cremire the Collector Slot", 6);
            AddMultipleLocations("Doki the Banker Slot", 6);
            AddMultipleLocations("Strange Time Keeper Slot", 2);

            // CHEST MILESTONES
            foreach (string element in elements)
            {
                AddMultipleLocations(element + " Chest Slot", 10);
            }
            
            AddMultipleLocations("MiniBoss Chest Slot", 30);
            AddMultipleLocations("Boss Chest Slot", 15);

            int[] milestones = [10, 25, 50, 75, 100];
            foreach (int count in milestones)
            {
                AddLocation($"Open {count} Total Chests");
            }

            // PROGRESSION & MILESTONES
            AddLocation("Stage 1-1 Cleared");
            AddLocation("Stage 1-2 Cleared");
            AddLocation("Stage 2-1 Cleared");
            AddLocation("Stage 2-2 Cleared");
            AddLocation("Stage 3-1 Cleared");
            AddLocation("Stage 3-2 Cleared");

            AddLocation("First Council Member Defeated");
            AddLocation("Second Council Member Defeated");
            AddLocation("Third Council Member Defeated");

            AddLocation("Break 100 Paintings");

            AddLocation("Have 1000 gold");

            foreach (string boss in bosses)
            {
                AddLocation("Perfect " + boss);
            }

            foreach (string miniBoss in minibosses)
            {
                AddLocation("Perfect " + miniBoss);
            }
        }
    }
}