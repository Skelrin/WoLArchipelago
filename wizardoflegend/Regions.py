import re

from BaseClasses import Region
from .Locations import WoLLocation, location_table, bosses

STAGE_1_LOCS = {"First Council Member Defeated"}
STAGE_2_LOCS = {"Second Council Member Defeated", "Break 100 Paintings"}
STAGE_3_LOCS = {"Third Council Member Defeated", "Have 1000 gold"}

PERFECT_BOSS_NAMES = {f"Perfect {b}" for b in bosses}
NPCS = ("Doctor", "Savile", "Nocturne", "Cremire", "Doki")
ELEMENTS = ("Fire", "Water", "Earth", "Air", "Lightning")
ELEMENT_CHESTS = tuple(f"{elem} Chest Slot" for elem in ELEMENTS)

SLOT_PREFIXES = (
    ("Boss Chest Slot", "MiniBoss Chest Slot", "Nox the Unfortunate Slot")
    + NPCS
    + ELEMENT_CHESTS
)
SLOT_TOTALS = {
    prefix: sum(1 for name in location_table if name.startswith(prefix))
    for prefix in SLOT_PREFIXES
}

COUNT_THRESHOLDS = (
    ("MimicEnemy", (4, 9)),
    ("MovingStatue", (9, 19)),
)


def _get_number(loc_name: str) -> int:
    return int(re.findall(r"\d+", loc_name)[-1])


def _stage_for(regions: dict, number: int, stage_1_max: int, stage_2_max: int) -> Region:
    if number > stage_2_max:
        return regions["Stage 3"]
    if number > stage_1_max:
        return regions["Stage 2"]
    return regions["Stage 1"]


def _get_target_region(loc_name: str, loc_data, regions: dict) -> Region:
    if loc_data.category == "Spawn Shops":
        return regions["Plaza"]

    if "Strange Time Keeper" in loc_name or loc_name == "FinalBoss Defeated":
        return regions["Final Boss"]

    if loc_name in STAGE_1_LOCS:
        return regions["Stage 1"]

    if (
        "Stage 2" in loc_name
        or loc_name in STAGE_2_LOCS
        or "Defeated 50 times" in loc_name
        or (loc_name.startswith("Super") and loc_name.endswith("Defeated 5 times"))
    ):
        return regions["Stage 2"]

    if "Stage 3" in loc_name or loc_name in STAGE_3_LOCS:
        return regions["Stage 3"]

    for prefix in SLOT_PREFIXES:
        if loc_name.startswith(prefix):
            total = SLOT_TOTALS[prefix]
            return _stage_for(regions, _get_number(loc_name), total // 3, 2 * total // 3)

    for prefix, (stage_1_max, stage_2_max) in COUNT_THRESHOLDS:
        if loc_name.startswith(prefix):
            return _stage_for(regions, _get_number(loc_name), stage_1_max, stage_2_max)

    return regions["Stage 1"]


def create_regions(world, player):
    region_names = ["Menu", "Plaza", "Stage 1", "Stage 2", "Stage 3", "Final Boss"]
    regions = {name: Region(name, player, world.multiworld) for name in region_names}

    for loc_name, loc_data in location_table.items():
        if loc_name in PERFECT_BOSS_NAMES and not world.options.perfect_boss_locations:
            continue
        target_region = _get_target_region(loc_name, loc_data, regions)
        target_region.locations.append(
            WoLLocation(player, loc_name, loc_data.code, target_region)
        )

    connections = [
        ("Menu", "Plaza", None),
        ("Plaza", "Stage 1", "Plaza -> Stage 1"),
        ("Stage 1", "Stage 2", "Stage 1 -> Stage 2"),
        ("Stage 2", "Stage 3", "Stage 2 -> Stage 3"),
        ("Stage 3", "Final Boss", "Stage 3 -> Final Boss"),
    ]

    for source, target, rule_name in connections:
        regions[source].connect(regions[target], rule_name)

    world.multiworld.regions.extend(regions.values())