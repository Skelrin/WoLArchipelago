from typing import Dict, NamedTuple, Optional
from BaseClasses import Location

class WoLLocation(Location):
    game = "Wizard of Legend"

class WoLLocationData(NamedTuple):
    category: str
    code: Optional[int] = None

location_table: Dict[str, WoLLocationData] = {}

base_location_code = 871130000

def _add_location(name: str, category: str):
    global base_location_code
    location_table[name] = WoLLocationData(category, base_location_code)
    base_location_code += 1

def _add_multiple_locations(base_name: str, count: int, category: str):
    for i in range(1, count + 1):
        _add_location(f"{base_name} {i}", category)


# 1. BOSSES
bosses = [
    "AirBoss",
    "EarthBoss",
    "FireBoss",
    "IceBoss",
    "LightningBoss"
]
for b in bosses:
    _add_location(f"{b} Defeated", "Boss")
    _add_location(f"{b} Defeated 5 times", "Boss")

_add_location("FinalBoss Defeated", "Boss")

# 2. MINIBOSSES
minibosses = [
    "SuperArcher", 
    "SuperCoffin", 
    "SuperKnight", 
    "SuperLancer",
    "SuperMage", 
    "SuperRogue", 
    "SuperSummoner"
]
for m in minibosses:
    _add_location(f"{m} Defeated", "Miniboss")
    _add_location(f"{m} Defeated 5 times", "Miniboss")
    _add_location(f"{m} Defeated 10 times", "Miniboss")


# 3. ENEMIES
enemies = [
    "Blob", 
    "BlobRoller", 
    "Ghoul",
    "EnemyTurret", 
    "Archer", 
    "Knight", 
    "Mage", 
    "Rogue", 
    "Lancer", 
    "Summoner"
]
for e in enemies:
    _add_location(f"{e} Defeated 50 times", "Enemies")
    _add_location(f"{e} Defeated 100 times", "Enemies")

_add_location("MimicEnemy Defeated 20 times", "Enemies")

_add_location("Pinata Defeated", "Dungeon")
_add_location("Pinata Defeated 5 times", "Dungeon")
_add_location("Pinata Defeated 10 times", "Dungeon")

# 4. SPAWN SHOPS
_add_multiple_locations("Outfit Shop Slot", 16, "Spawn Shops")
_add_multiple_locations("Relic Shop Slot", 64, "Spawn Shops")
_add_multiple_locations("Arcana Shop Slot", 64, "Spawn Shops")


# 5. DUNGEON NPCS & EVENTS
_add_multiple_locations("Doctor Song Slot", 10, "Dungeon")
_add_multiple_locations("Savile the Tailor Slot", 10, "Dungeon")
_add_multiple_locations("Nox the Unfortunate Slot", 40, "Dungeon")
_add_multiple_locations("Nocturne the Cardist Slot", 10, "Dungeon")
_add_multiple_locations("Iris the Painter Slot", 5, "Dungeon")
_add_multiple_locations("Jade the Gem Merchant Slot", 5, "Dungeon")
_add_multiple_locations("Andres the Cartographer Slot", 5, "Dungeon")
_add_multiple_locations("Petala the Herbalist Slot", 5, "Dungeon")

# 6. CHESTS
_add_multiple_locations("Standard Chest Slot", 40, "Chests")
_add_multiple_locations("Mini Chest Slot", 40, "Chests")
_add_multiple_locations("MiniBoss Chest Slot", 40, "Chests")
_add_multiple_locations("Boss Chest Slot", 40, "Chests")
_add_multiple_locations("Party Chest Slot", 5, "Chests")
_add_multiple_locations("Elemental Chest Slot", 5, "Chests")


# 7. PROGRESSION & MILESTONES
_add_location("Stage 1-1 Cleared", "Progression")
_add_location("Stage 1-2 Cleared", "Progression")
_add_location("Stage 2-1 Cleared", "Progression")
_add_location("Stage 2-2 Cleared", "Progression")
_add_location("Stage 3-1 Cleared", "Progression")
_add_location("Stage 3-2 Cleared", "Progression")

_add_location("First Council Member Defeated", "Progression")
_add_location("Second Council Member Defeated", "Progression")
_add_location("Third Council Member Defeated", "Progression")

_add_location("Break 50 Paintings", "Milestones")
_add_location("Break 100 Paintings", "Milestones")

_add_location("Die 10 times", "Milestones")
_add_location("Fall 50 times", "Milestones")

_add_location("Dash 100 times", "Milestones")
_add_location("Dash 500 times", "Milestones")

def get_locations_by_category(category: str) -> Dict[str, WoLLocationData]:
    loc_dict: Dict[str, WoLLocationData] = {}
    for name, data in location_table.items():
        if data.category == category:
            loc_dict.setdefault(name, data)
    return loc_dict