from typing import Dict, NamedTuple, Optional
from BaseClasses import Item, ItemClassification

class WoLItem(Item):
    game = "Wizard of Legend"

class WoLItemData(NamedTuple):
    category: str
    code: Optional[int] = None
    classification: ItemClassification = ItemClassification.filler
    max_quantity: int = 1

def get_items_by_category(category: str) -> Dict[str, WoLItemData]:
    item_dict: Dict[str, WoLItemData] = {}
    for name, data in item_table.items():
        if data.category == category:
            item_dict.setdefault(name, data)

    return item_dict

item_table: Dict[str, WoLItemData] = {
    # -------------------------------------------------------------------------
    # Progression Items
    # -------------------------------------------------------------------------
    "Chaos Fragment":           WoLItemData("Progression", 871120001, ItemClassification.progression),
    "Boss Key":                 WoLItemData("Progression", 871120002, ItemClassification.progression, max_quantity=3),
    "Shop Upgrade":             WoLItemData("Progression", 871120003, ItemClassification.progression, max_quantity=4),

    # -------------------------------------------------------------------------
    # Biome Keys
    # -------------------------------------------------------------------------
    "Fire Biome Key":            WoLItemData("Biome Keys", 871120010, ItemClassification.progression),
    "Water Biome Key":           WoLItemData("Biome Keys", 871120011, ItemClassification.progression),
    "Earth Biome Key":           WoLItemData("Biome Keys", 871120012, ItemClassification.progression),
    "Air Biome Key":             WoLItemData("Biome Keys", 871120013, ItemClassification.progression),
    "Lightning Biome Key":       WoLItemData("Biome Keys", 871120014, ItemClassification.progression),

    # -------------------------------------------------------------------------
    # Outfits
    # -------------------------------------------------------------------------
    "Outfit: Hope":             WoLItemData("Outfits", 871121001, ItemClassification.useful),
    "Outfit: Patience":         WoLItemData("Outfits", 871121002, ItemClassification.useful),
    "Outfit: Vigor":            WoLItemData("Outfits", 871121003, ItemClassification.useful),
    "Outfit: Grit":             WoLItemData("Outfits", 871121004, ItemClassification.useful),
    "Outfit: Greed":            WoLItemData("Outfits", 871121005, ItemClassification.useful),
    "Outfit: Pink":             WoLItemData("Outfits", 871121006, ItemClassification.useful),
    "Outfit: Pace":             WoLItemData("Outfits", 871121007, ItemClassification.useful),
    "Outfit: Tempo":            WoLItemData("Outfits", 871121008, ItemClassification.useful),
    "Outfit: Switch":           WoLItemData("Outfits", 871121009, ItemClassification.useful),
    "Outfit: Awe":              WoLItemData("Outfits", 871121010, ItemClassification.useful),
    "Outfit: Fury":             WoLItemData("Outfits", 871121011, ItemClassification.useful),
    "Outfit: Rule":             WoLItemData("Outfits", 871121012, ItemClassification.useful),
    "Outfit: Level":            WoLItemData("Outfits", 871121013, ItemClassification.useful),
    "Outfit: Venture":          WoLItemData("Outfits", 871121014, ItemClassification.useful),
    "Outfit: Fall":             WoLItemData("Outfits", 871121015, ItemClassification.useful),
    "Outfit: Pride":            WoLItemData("Outfits", 871121016, ItemClassification.useful),

    # -------------------------------------------------------------------------
    # Arcana Slots & Licenses
    # -------------------------------------------------------------------------
    "Standard Arcana Slot":     WoLItemData("Licenses", 871122001, ItemClassification.progression),
    "Signature Arcana Slot":    WoLItemData("Licenses", 871122002, ItemClassification.progression),
    "Bonus Arcana Slot 1":      WoLItemData("Licenses", 871122003, ItemClassification.progression),
    "Bonus Arcana Slot 2":      WoLItemData("Licenses", 871122004, ItemClassification.progression),

    "Fire Element License":      WoLItemData("Licenses", 871122010, ItemClassification.progression),
    "Water Element License":     WoLItemData("Licenses", 871122011, ItemClassification.progression),
    "Earth Element License":     WoLItemData("Licenses", 871122012, ItemClassification.progression),
    "Air Element License":       WoLItemData("Licenses", 871122013, ItemClassification.progression),
    "Lightning Element License": WoLItemData("Licenses", 871122014, ItemClassification.progression),

    # -------------------------------------------------------------------------
    # Upgrades
    # -------------------------------------------------------------------------
    "Max HP Boost":             WoLItemData("Upgrades", 871123001, ItemClassification.useful, max_quantity=10),
    "Relic Slot Upgrade":       WoLItemData("Upgrades", 871123002, ItemClassification.progression, max_quantity=11),

    # -------------------------------------------------------------------------
    # Fillers
    # -------------------------------------------------------------------------
    "Heal Orb":                 WoLItemData("Fillers", 871123003, ItemClassification.filler),
    "Gold Pack":                WoLItemData("Fillers", 871123004, ItemClassification.filler),
    "Chaos Gems Pack":          WoLItemData("Fillers", 871123005, ItemClassification.filler),

    # -------------------------------------------------------------------------
    # Traps
    # -------------------------------------------------------------------------
    "Cursed Trap":              WoLItemData("Traps", 871124001, ItemClassification.trap, max_quantity=5),
    "Mob Trap":                 WoLItemData("Traps", 871124002, ItemClassification.trap, max_quantity=5),
    "Heal Trap":                WoLItemData("Traps", 871124003, ItemClassification.trap, max_quantity=5),
    "Gold Trap":                WoLItemData("Traps", 871124004, ItemClassification.trap, max_quantity=5),

    # -------------------------------------------------------------------------
    # Generic Relics
    # -------------------------------------------------------------------------
    "Relic Bundle Tier 1":             WoLItemData("Relics", 871125001, max_quantity=13),
    "Relic Bundle Tier 2":             WoLItemData("Relics", 871125002, max_quantity=18),
    "Relic Bundle Tier 3":             WoLItemData("Relics", 871125003, max_quantity=20),
    "Relic Bundle Tier 4":             WoLItemData("Relics", 871125004, ItemClassification.useful, max_quantity=16),
    "Relic Bundle Tier 5":             WoLItemData("Relics", 871125005, ItemClassification.useful, max_quantity=39),
    
    # -------------------------------------------------------------------------
    # NPC Relics
    # -------------------------------------------------------------------------   
    "TokenBanker":              WoLItemData("Relics", 871125006, ItemClassification.progression),
    "TokenShuffler":            WoLItemData("Relics", 871125007, ItemClassification.progression),
    "TokenDoctor":              WoLItemData("Relics", 871125008, ItemClassification.progression),
    "TokenCursed":              WoLItemData("Relics", 871125009, ItemClassification.progression),
    "TokenTailor":              WoLItemData("Relics", 871125010, ItemClassification.progression),
    "TokenCollector":           WoLItemData("Relics", 871125011, ItemClassification.progression),
    "TokenPinata":              WoLItemData("Relics", 871125012, ItemClassification.progression),


    # -------------------------------------------------------------------------
    # Generic Arcanas
    # -------------------------------------------------------------------------
    "Arcana Bundle Tier 1":            WoLItemData("Arcanas", 871126001, max_quantity=9),
    "Arcana Bundle Tier 2":            WoLItemData("Arcanas", 871126002, max_quantity=15),
    "Arcana Bundle Tier 3":            WoLItemData("Arcanas", 871126003, max_quantity=15),
    "Arcana Bundle Tier 4":            WoLItemData("Arcanas", 871126004, ItemClassification.useful, max_quantity=20),
    "Arcana Bundle Tier 5":            WoLItemData("Arcanas", 871126005, ItemClassification.useful, max_quantity=32),
}