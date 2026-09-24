from BaseClasses import Region
from .Locations import WoLLocation, location_table

STAGE_3_LOCS = {"Third Council Member Defeated", "Party Chest Slot 2", "Pinata Defeated 10 times"}
STAGE_2_LOCS = {"Second Council Member Defeated", "Party Chest Slot 1", "Dash 500 times", "Break 100 Paintings", "Pinata Defeated 5 times"}
NPCS = ("Doctor", "Savile", "Nocturne", "Cremire", "Doki")
ELEMENTS = ("Fire", "Water", "Earth", "Air", "Lightning")

def _get_slot_num(loc_name: str) -> int:
    return int(loc_name.rsplit(" ", 1)[-1])

def _get_target_region(loc_name: str, loc_data, regions: dict) -> Region:
    if loc_data.category == "Spawn Shops":
        return regions["Plaza"]

    if "Strange Time Keeper" in loc_name or loc_name == "FinalBoss Defeated":
        return regions["Final Boss"]

    if "Stage 3" in loc_name or loc_name in STAGE_3_LOCS or "Defeated 75 times" in loc_name or ("Super" in loc_name and "Defeated 10 times" in loc_name):
        return regions["Stage 3"]

    if "Stage 2" in loc_name or loc_name in STAGE_2_LOCS or "Defeated 50 times" in loc_name or ("Super" in loc_name and "Defeated 5 times" in loc_name):
        return regions["Stage 2"]

    slot_value = -1
    thresholds = None

    if "MimicEnemy" in loc_name:
        slot_value = int(loc_name.rsplit(" ")[-2])
        thresholds = (9, 19)
    else:
        if "MiniBoss Chest Slot" in loc_name or "Nox the Unfortunate Slot" in loc_name:
            thresholds = (14, 28)
        elif any(npc in loc_name for npc in NPCS):
            thresholds = (3, 6)
        elif "Boss Chest Slot" in loc_name or any(f"{elem} Chest Slot" in loc_name for elem in ELEMENTS):
            thresholds = (7, 14)
        
        if thresholds:
            slot_value = _get_slot_num(loc_name)

    if thresholds:
        if slot_value > thresholds[1]:
            return regions["Stage 3"]
        if slot_value > thresholds[0]:
            return regions["Stage 2"]

    return regions["Stage 1"]

def create_regions(world, player):
    region_names = ["Menu", "Plaza", "Stage 1", "Stage 2", "Stage 3", "Final Boss"]
    regions = {name: Region(name, player, world.multiworld) for name in region_names}

    for loc_name, loc_data in location_table.items():
        target_region = _get_target_region(loc_name, loc_data, regions)
        location = WoLLocation(player, loc_name, loc_data.code, target_region)
        target_region.locations.append(location)

    connections = [
        ("Menu", "Plaza", None),
        ("Plaza", "Stage 1", "Plaza -> Stage 1"),
        ("Stage 1", "Stage 2", "Stage 1 -> Stage 2"),
        ("Stage 2", "Stage 3", "Stage 2 -> Stage 3"),
        ("Stage 3", "Final Boss", "Stage 3 -> Final Boss"),
    ]

    for source, target, rule_name in connections:
        regions[source].connect(regions[target], rule_name)

    for region in regions.values():
        world.multiworld.regions.append(region)