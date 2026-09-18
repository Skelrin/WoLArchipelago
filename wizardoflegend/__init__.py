from worlds.AutoWorld import World
from .Items import item_table, WoLItem
from .Locations import location_table
from .Regions import create_regions
from .Rules import set_rules
from .Options import WoLOptions

class WoLWorld(World):
    game = "Wizard of Legend"
    options_dataclass = WoLOptions
    options: WoLOptions

    item_name_to_id = {name: data.code for name, data in item_table.items() if data.code is not None}
    location_name_to_id = {name: data.code for name, data in location_table.items() if data.code is not None}

    def generate_early(self):
        if self.options.chaos_fragments_required.value > self.options.chaos_fragments_total.value:
            self.options.chaos_fragments_required.value = self.options.chaos_fragments_total.value

        self.starting_element = None
        if self.options.starting_arcana_mode.value == self.options.starting_arcana_mode.option_random_element:
            elements = ["Fire", "Water", "Earth", "Air", "Lightning"]
            self.starting_element = self.random.choice(elements)
            
            if self.options.element_licenses_mode.value == self.options.element_licenses_mode.option_required:
                license_name = f"{self.starting_element} Element License"
                self.multiworld.push_precollected(self.create_item(license_name))

    def create_regions(self):
        create_regions(self, self.player)

    def set_rules(self):
        set_rules(self, self.player)

    def create_items(self):
        item_pool = []

        for _ in range(self.options.chaos_fragments_total.value):
            item_pool.append(self.create_item("Chaos Fragment"))

        if self.options.element_licenses_mode:
            licenses = [
                "Fire Element License", 
                "Water Element License",
                "Earth Element License", 
                "Air Element License", 
                "Lightning Element License"
            ]
            for lic in licenses:
                if not self._is_precollected(lic):
                    item_pool.append(self.create_item(lic))

        for item_name, item_data in item_table.items():
            if item_name == "Chaos Fragment" or "Element License" in item_name:
                continue
            
            for _ in range(item_data.max_quantity):
                item_pool.append(self.create_item(item_name))

        total_locations = len(self.multiworld.get_unfilled_locations(self.player))
        
        if len(item_pool) < total_locations:
            while len(item_pool) < total_locations:
                item_pool.append(self.create_item("Gold Pack"))
        elif len(item_pool) > total_locations:
            removable_item_names = ["Gold Pack", "Relic Tier 1", "Arcana Tier 1"]
            
            for target_name in removable_item_names:
                for i in range(len(item_pool) - 1, -1, -1):
                    if len(item_pool) <= total_locations:
                        break
                    
                    if item_pool[i].name == target_name:
                        item_pool.pop(i)
                
                if len(item_pool) <= total_locations:
                    break
                    
            if len(item_pool) > total_locations:
                raise Exception(f"[Wizard of Legend] Too much item ({len(item_pool)}) for location number ({total_locations}). Cannot remove any more filler items.")

        self.multiworld.itempool += item_pool

    def fill_slot_data(self) -> dict:
        slot_data = {
            "starting_arcana_mode": self.options.starting_arcana_mode.value,
            "element_licenses_mode": self.options.element_licenses_mode.value,
            "chaos_fragments_required": self.options.chaos_fragments_required.value,
        }
        
        if getattr(self, "starting_element", None):
            slot_data["starting_element"] = self.starting_element
            
        return slot_data

    def _is_precollected(self, item_name: str) -> bool:
        return any(item.name == item_name for item in self.multiworld.precollected_items[self.player])

    def create_item(self, name: str) -> WoLItem:
        data = item_table[name]
        return WoLItem(name, data.classification, data.code, self.player)