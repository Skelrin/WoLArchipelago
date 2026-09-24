from BaseClasses import Region
from .Locations import WoLLocation, location_table

def _get_slot_num(loc_name: str) -> int:
    return int(loc_name.rsplit(" ", 1)[-1])

def _get_target_region(loc_name: str, loc_data, regions: dict) -> Region:
    category = loc_data.category

    if category == "Spawn Shops":
        return regions["Plaza"]

    if "Strange Time Keeper" in loc_name or loc_name == "FinalBoss Defeated":
        return regions["Final Boss"]

    if "Stage 3" in loc_name or loc_name in ("Third Council Member Defeated", "Party Chest Slot 2", "Pinata Defeated 10 times"):
        return regions["Stage 3"]

    if "Stage 2" in loc_name or loc_name in ("Second Council Member Defeated", "Party Chest Slot 1", "Dash 500 times", "Break 100 Paintings", "Pinata Defeated 5 times"):
        return regions["Stage 2"]

    if "Defeated 75 times" in loc_name:
        return regions["Stage 3"]
        
    if "Defeated 50 times" in loc_name:
        return regions["Stage 2"]

    if "Super" in loc_name and "Defeated 10 times" in loc_name:
            return regions["Stage 3"]

    if "Super" in loc_name and "Defeated 5 times" in loc_name:
        return regions["Stage 2"]

    if "MimicEnemy" in loc_name:
        time_killed = int(loc_name.rsplit(" ")[-2])
        if time_killed > 19:
            return regions["Stage 3"]
        if time_killed > 9:
            return regions["Stage 2"]
        return regions["Stage 1"]

    if "MiniBoss Chest Slot" in loc_name or "Nox the Unfortunate Slot" in loc_name:
        slot = _get_slot_num(loc_name)
        if slot > 28:
            return regions["Stage 3"]
        if slot > 14:
            return regions["Stage 2"]
        return regions["Stage 1"]

    if any(f"{npc}" in loc_name for npc in ["Doctor", "Savile", "Nocturne", "Cremire", "Doki"]):
        slot = _get_slot_num(loc_name)
        if slot > 6:
            return regions["Stage 3"]
        if slot > 3:
            return regions["Stage 2"]
        return regions["Stage 1"]

    if "Boss Chest Slot" in loc_name or any(f"{elem} Chest Slot" in loc_name for elem in ["Fire", "Water", "Earth", "Air", "Lightning"]):
        slot = _get_slot_num(loc_name)
        if slot > 14:
            return regions["Stage 3"]
        if slot > 7:
            return regions["Stage 2"]
        return regions["Stage 1"]

    return regions["Stage 1"]


def create_regions(world, player):
    regions = {
        name: Region(name, player, world.multiworld)
        for name in ["Menu","Plaza", "Stage 1", "Stage 2", "Stage 3", "Final Boss"]
    }

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