from worlds.generic.Rules import set_rule

def set_rules(world, player):
    options = world.options

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
        lambda state: state.has("Boss Key", player, 3) and state.has("Chaos Fragment", player, options.chaos_fragments_required.value)
    )

    for i in range(1, 21):
        set_rule(
            world.get_location(f"Boss Chest Slot {i}"),
            lambda state: state.has("Boss Key", player, 1)
        )

    elements = {
        "Fire": "Fire Biome Key",
        "Water": "Water Biome Key",
        "Earth": "Earth Biome Key",
        "Air": "Air Biome Key",
        "Lightning": "Lightning Biome Key"
    }

    for elem, biome_key in elements.items():
        for i in range(1, 21):
            set_rule(
                world.get_location(f"{elem} Chest Slot {i}"),
                lambda state, bk=biome_key: state.has(bk, player)
            )
        for count in [25, 50, 75]:
            set_rule(
                world.get_location(f"{elem} Enemies Defeated {count} times"),
                lambda state, bk=biome_key: state.has(bk, player)
            )

    boss_mapping = {
        "AirBoss": "Air",
        "EarthBoss": "Earth",
        "FireBoss": "Fire",
        "IceBoss": "Water",
        "LightningBoss": "Lightning"
    }

    for boss, elem in boss_mapping.items():
        biome_key = elements[elem]
        for suffix in ["Defeated", "Defeated 5 times"]:
            set_rule(
                world.get_location(f"{boss} {suffix}"),
                lambda state, bk=biome_key: state.has("Boss Key", player, 1) and state.has(bk, player)
            )

    shop_tiers = [
        (range(1, 17), 1),
        (range(17, 33), 2),
        (range(33, 49), 3),
        (range(49, 65), 4),
    ]

    for shop_type in ["Relic Shop Slot", "Arcana Shop Slot"]:
        for slot_range, level in shop_tiers:
            for i in slot_range:
                set_rule(
                    world.get_location(f"{shop_type} {i}"),
                    lambda state, l=level: state.has("Shop Upgrade", player, l)
                )

    for i in range(1, 17):
        set_rule(
            world.get_location(f"Outfit Shop Slot {i}"),
            lambda state: state.has("Shop Upgrade", player, 1)
        )

    biome_keys = list(elements.values())
    for req_keys, count in enumerate([10, 25, 50, 75, 100], start=1):
        set_rule(
            world.get_location(f"Open {count} Total Chests"), 
            lambda state, r=req_keys: sum(state.has(k, player) for k in biome_keys) >= r
        )

    world.multiworld.completion_condition[player] = lambda state: state.can_reach_location("FinalBoss Defeated", player)