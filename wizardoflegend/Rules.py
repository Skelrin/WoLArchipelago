from worlds.generic.Rules import set_rule
from .Locations import location_table

ELEMENT_KEYS = {
    "Fire": "Fire Biome Key",
    "Water": "Water Biome Key",
    "Earth": "Earth Biome Key",
    "Air": "Air Biome Key",
    "Lightning": "Lightning Biome Key",
}

BOSS_ELEMENTS = {
    "AirBoss": "Air",
    "EarthBoss": "Earth",
    "FireBoss": "Fire",
    "IceBoss": "Water",
    "LightningBoss": "Lightning",
}

COUNCIL_MEMBERS = (
    "First Council Member Defeated",
    "Second Council Member Defeated",
    "Third Council Member Defeated",
)

SHOP_SLOTS_PER_LEVEL = {
    "Outfit Shop Slot": 16,
    "Relic Shop Slot": 8,
    "Arcana Shop Slot": 8,
}

CHEST_MILESTONES = (10, 25, 50, 75, 100)

NPC_TOKENS = {
    "Doctor Song Slot": "TokenDoctor",
    "Savile the Tailor Slot": "TokenTailor",
    "Nox the Unfortunate Slot": "TokenCursed",
    "Nocturne the Cardist Slot": "TokenShuffler",
    "Cremire the Collector Slot": "TokenCollector",
    "Doki the Banker Slot": "TokenBanker",
}

def _numbered(prefix: str):
    return [
        (name, int(name.rsplit(" ", 1)[1]))
        for name in location_table
        if name.startswith(prefix + " ")
    ]


def set_rules(world, player):
    options = world.options

    def boss_keys(count: int):
        return lambda state: state.has("Boss Key", player, count)

    def has_item(name: str, count: int = 1):
        return lambda state: state.has(name, player, count)

    def boss_and_biome(biome_key: str):
        return lambda state: state.has("Boss Key", player, 1) and state.has(biome_key, player)

    def biome_keys_owned(count: int):
        return lambda state: sum(state.has(key, player) for key in ELEMENT_KEYS.values()) >= count

    def location_rule(name: str, rule):
        set_rule(world.get_location(name), rule)

    set_rule(world.get_entrance("Stage 1 -> Stage 2"), boss_keys(1))
    set_rule(world.get_entrance("Stage 2 -> Stage 3"), boss_keys(2))
    set_rule(
        world.get_entrance("Stage 3 -> Final Boss"),
        lambda state: state.has("Boss Key", player, 3)
        and state.has("Chaos Fragment", player, options.chaos_fragments_required.value),
    )

    for keys_needed, name in enumerate(COUNCIL_MEMBERS, start=1):
        location_rule(name, boss_keys(keys_needed))

    for name, _ in _numbered("Boss Chest Slot"):
        location_rule(name, boss_keys(1))

    for elem, biome_key in ELEMENT_KEYS.items():
        for name, _ in _numbered(f"{elem} Chest Slot"):
            location_rule(name, has_item(biome_key))
        for count in (25, 50):
            location_rule(f"{elem} Enemies Defeated {count} times", has_item(biome_key))

    for boss, elem in BOSS_ELEMENTS.items():
        for name in (f"{boss} Defeated", f"Perfect {boss}"):
            location_rule(name, boss_and_biome(ELEMENT_KEYS[elem]))

    for shop_prefix, per_level in SHOP_SLOTS_PER_LEVEL.items():
        for name, slot in _numbered(shop_prefix):
            level = (slot - 1) // per_level + 1
            location_rule(name, has_item("Shop Upgrade", level))

    for npc_prefix, token in NPC_TOKENS.items():
        for name, _ in _numbered(npc_prefix):
            location_rule(name, has_item(token))

    location_rule("Pinata Defeated", has_item("TokenPinata"))

    final_boss_region = world.multiworld.get_region("Final Boss", player)
    for location in final_boss_region.locations:
        orig_rule = location.item_rule
        location.item_rule = lambda item, orig=orig_rule: orig(item) and not item.name.startswith("Token")

    for keys_needed, total in enumerate(CHEST_MILESTONES, start=1):
        location_rule(f"Open {total} Total Chests", biome_keys_owned(keys_needed))

    world.multiworld.completion_condition[player] = (
        lambda state: state.can_reach_location("FinalBoss Defeated", player)
    )