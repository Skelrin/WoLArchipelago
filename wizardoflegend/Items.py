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

    # TODO : Add Relics Slots

    # -------------------------------------------------------------------------
    # Stats & Upgrades
    # -------------------------------------------------------------------------
    "Max HP Boost":             WoLItemData("Upgrades", 871123001, ItemClassification.useful, max_quantity=10),
    "Gold Pack":                WoLItemData("Upgrades", 871123002, ItemClassification.useful, max_quantity=10),
    "Chaos Gems Pack":          WoLItemData("Upgrades", 871123003, ItemClassification.useful, max_quantity=10),
    "Relic Slot Upgrade":       WoLItemData("Upgrades", 871123004, ItemClassification.progression, max_quantity=5),


    # -------------------------------------------------------------------------
    # Traps
    # -------------------------------------------------------------------------
    "Cursed Trap":              WoLItemData("Traps", 871124001, ItemClassification.trap, max_quantity=10),

    # -------------------------------------------------------------------------
    # Generic Relics
    # -------------------------------------------------------------------------
    "Relic Tier 1":             WoLItemData("Relics", 871125001, max_quantity=25),
    "Relic Tier 2":             WoLItemData("Relics", 871125002, max_quantity=36),
    "Relic Tier 3":             WoLItemData("Relics", 871125003, max_quantity=41),
    "Relic Tier 4":             WoLItemData("Relics", 871125004, ItemClassification.useful, max_quantity=36),
    "Relic Tier 5":             WoLItemData("Relics", 871125005, ItemClassification.useful, max_quantity=77),

    # -------------------------------------------------------------------------
    # Doctor Relics
    # -------------------------------------------------------------------------
    "DoctorPrescription":       WoLItemData("Doctor Relics", 871125006),
    "DoctorPlacebo":            WoLItemData("Doctor Relics", 871125007),
    "DoctorDiscount":           WoLItemData("Doctor Relics", 871125008),
    "DoctorVial":               WoLItemData("Doctor Relics", 871125009),
    "CritHealChanceUp":         WoLItemData("Doctor Relics", 871125010),
    "HealRestock":              WoLItemData("Doctor Relics", 871125011),
    "DoctorHpDamage":           WoLItemData("Doctor Relics", 871125012),

    # -------------------------------------------------------------------------
    # Generic Arcanas
    # -------------------------------------------------------------------------
    "Arcana Tier 1":            WoLItemData("Arcanas", 871126001, max_quantity=17),
    "Arcana Tier 2":            WoLItemData("Arcanas", 871126002, max_quantity=30),
    "Arcana Tier 3":            WoLItemData("Arcanas", 871126003, max_quantity=30),
    "Arcana Tier 4":            WoLItemData("Arcanas", 871126004, ItemClassification.useful, max_quantity=40),
    "Arcana Tier 5":            WoLItemData("Arcanas", 871126005, ItemClassification.useful, max_quantity=63),
}