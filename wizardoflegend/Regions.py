from BaseClasses import Region
from .Locations import WoLLocation, location_table

def create_regions(world, player):
    regions = {
        "Menu": Region("Menu", player, world.multiworld),
        "Plaza": Region("Plaza", player, world.multiworld),
        "Stage 1": Region("Stage 1", player, world.multiworld),
        "Stage 2": Region("Stage 2", player, world.multiworld),
        "Stage 3": Region("Stage 3", player, world.multiworld),
        "Final Boss": Region("Final Boss", player, world.multiworld),
    }

    for loc_name, loc_data in location_table.items():
        category = loc_data.category

        if category == "Spawn Shops":
            target_region = regions["Plaza"]

        elif "Strange Time Keeper" in loc_name or loc_name == "FinalBoss Defeated":
            target_region = regions["Final Boss"]

        elif "Stage 3" in loc_name or loc_name == "Third Council Member Defeated":
            target_region = regions["Stage 3"]
            
        elif "Stage 2" in loc_name or loc_name == "Second Council Member Defeated":
            target_region = regions["Stage 2"]

        else:
            target_region = regions["Stage 1"]

        location = WoLLocation(player, loc_name, loc_data.code, target_region)
        target_region.locations.append(location)

    regions["Menu"].connect(regions["Plaza"])
    regions["Plaza"].connect(regions["Stage 1"], "Plaza -> Stage 1")
    regions["Stage 1"].connect(regions["Stage 2"], "Stage 1 -> Stage 2")
    regions["Stage 2"].connect(regions["Stage 3"], "Stage 2 -> Stage 3")
    regions["Stage 3"].connect(regions["Final Boss"], "Stage 3 -> Final Boss")

    for region in regions.values():
        world.multiworld.regions.append(region)