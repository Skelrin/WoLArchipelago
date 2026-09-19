from worlds.generic.Rules import set_rule

def set_rules(world, player):
    options = world.options

    req_fragments = options.chaos_fragments_required.value
    
    set_rule(
        world.get_entrance("Stage 1 -> Stage 2"),
        lambda state: state.has("Boss Key", player, 1)
    )

    set_rule(
        world.get_entrance("Stage 2 -> Stage 3"),
        lambda state: state.has("Boss Key", player, 2)
    )

    set_rule(
        world.get_entrance("Stage 3 -> Final Boss"),
        lambda state: state.has("Boss Key", player, 3) and state.has("Chaos Fragment", player, req_fragments)
    )

    shop_tiers = [
        (range(17, 33), 1),
        (range(33, 49), 2),
        (range(49, 65), 3),
    ]

    for shop_type in ["Relic Shop Slot", "Arcana Shop Slot"]:
        for slot_range, level in shop_tiers:
            for i in slot_range:
                set_rule(
                    world.get_location(f"{shop_type} {i}"),
                    lambda state, l=level: state.has("Shop Upgrade", player, l)
                )

    elemental_chests = {
        "Elemental Chest Slot 1": "Fire Element License",
        "Elemental Chest Slot 2": "Water Element License",
        "Elemental Chest Slot 3": "Earth Element License",
        "Elemental Chest Slot 4": "Air Element License",
        "Elemental Chest Slot 5": "Lightning Element License",
    }

    if options.element_licenses_mode:
        for chest_name, license_name in elemental_chests.items():
            set_rule(
                world.get_location(chest_name),
                lambda state, l=license_name: state.has(l, player)
            )

    world.multiworld.completion_condition[player] = lambda state: state.can_reach_location("FinalBoss Defeated", player)