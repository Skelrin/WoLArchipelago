from dataclasses import dataclass
from Options import Choice, Range, PerGameCommonOptions

class ChaosFragmentsRequired(Range):
    """Number of chaos fragment needed to unlock access to Master Sura."""
    display_name = "Chaos Fragments Required"
    range_start = 1
    range_end = 10
    default = 7

class ChaosFragmentsTotal(Range):
    """Number of chaos fragment available."""
    display_name = "Chaos Fragments Available"
    range_start = 1
    range_end = 15
    default = 10

class StartingArcanaMode(Choice):
    """Set starting Arcanas
    - Vanilla : Default basic and dash arcana at start.
    - Random Element : Random basic and dash arcana of the same element.
    """
    display_name = "Starting Arcana Mode"
    option_vanilla = 0
    option_random_element = 1
    default = 1

class ElementLicensesMode(Choice):
    """
    - Disabled : You can use every element at the start of the game.
    - Required : You need to get element license to be able to use arcana of the associated element.
    """
    display_name = "Element Licenses Mode"
    option_disabled = 0
    option_required = 1
    default = 1


@dataclass
class WoLOptions(PerGameCommonOptions):
    chaos_fragments_required: ChaosFragmentsRequired
    chaos_fragments_total: ChaosFragmentsTotal
    starting_arcana_mode: StartingArcanaMode
    element_licenses_mode: ElementLicensesMode