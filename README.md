# Wizard of Legend - Archipelago MultiWorld Mod

[![Archipelago](https://img.shields.io/badge/Archipelago-MultiWorld-blue)](https://archipelago.gg/)
[![BepInEx 5](https://img.shields.io/badge/Modding-BepInEx%205-green)](https://github.com/BepInEx/BepInEx)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

An **Archipelago MultiWorld** integration mod for **Wizard of Legend**. This mod randomizes items, spells, relics, outfits, and progress checks throughout the Chaos Trials, connecting your game to the Archipelago network.

## Features

* **Locked Skill Slots**: Standard, Signature, and Bonus Arcana slots are locked by default and can be unlocked via MultiWorld items.
* **Element Licenses**: Option to lock elements (Fire, Water, Earth, Air, Lightning), requiring an Element License to equip/use corresponding spells.
* **488 Location Checks**: Includes bosses, minibosses, chest milestones, elemental kills, NPC interactions, spawn shop inventory, and gameplay milestones.
* **Dynamic UI**: In-game HUD, SpellBook, and shop interfaces dynamically update based on unlocked slots and permissions.

## Setup & Installation

For full installation and setup instructions, please consult the official setup guide:

**[Read the Setup Guide (setup_en.md)](wizardoflegend/docs/setup_en.md)**

Quick summary:
1. Install **BepInEx 5.x** into your Wizard of Legend game directory.
2. Download the latest mod release from [GitHub Releases](https://github.com/Skelrin/WoLArchipelago/releases) or Thunderstore.
3. Extract the contents into `BepInEx/plugins/`.
4. Launch the game and click **CONNECT TO ARCHIPELAGO** on the title screen.

## Archipelago World & YAML Options

The repository contains the Python `.apworld` implementation inside the `wizardoflegend/` directory.

### Key Options

* **`chaos_fragments_required`**: Number of Chaos Fragments needed to unlock the final portal to Master Sura (default: `7`).
* **`chaos_fragments_total`**: Total number of Chaos Fragments placed in the item pool (default: `10`).
* **`starting_arcana_mode`**: Choose whether starting Arcana slots are vanilla or randomized (`0` = Vanilla, `1` = Randomize).
* **`element_licenses_mode`**: Enable or disable Element License requirements (`0` = Disabled, `1` = Required).

For full details on options and logic, check **[en_WizardOfLegend.md](wizardoflegend/docs/en_WizardOfLegend.md)**.

---

## Repository Structure

```text
.
├── AP/                 # Core Archipelago client integration & state mapping
│   ├── APItemLocationDatabase.cs  # ID maps for items and locations
│   ├── APManager.cs               # Connection handling & MultiClient.Net wrapper
│   ├── APShopSlot.cs              # MultiWorld shop slot logic
│   └── APSpriteManager.cs         # Icon and sprite loading for AP items
│
├── Patches/            # Harmony patches hooking into game systems
│   ├── AchievementsPatches.cs     # Progression & milestone triggers
│   ├── InventoryPatches.cs        # Relic & item management hooks
│   ├── LevelPatches.cs            # Stage generation & clear conditions
│   ├── SkillSlotsPatches.cs       # Skill slot locking and verification
│   ├── TitleScreenPatches.cs      # Custom AP connection button injection
│   └── ...                        # NPC, shop, chest, and gameplay hooks
│
├── Services/           # Business logic and gameplay state management
│   ├── BiomesHandler.cs           # Biome key logic & restriction enforcement
│   ├── CheckHandler.cs            # Location check detection & dispatching
│   ├── ItemHandler.cs             # Inbound AP item processing & queuing
│   ├── ShopService.cs             # AP shop slot registration & refreshing
│   └── SlotManager.cs             # Arcana & relic slot lock state manager
│
├── UI/                 # Custom UI components
│   ├── ArchipelagoUI.cs           # Connection popup window (F1 menu)
│   └── Sprites/                   # AP branded graphical assets
│
├── Utilities/          # Helper utilities
│   └── TlsProxyServer.cs          # Network TLS workaround for Unity/AP client
│
├── wizardoflegend/     # Archipelago Python World (.apworld)
│   ├── docs/                      # Documentation for Archipelago
│   ├── __init__.py                # World implementation entry point
│   ├── Items.py                   # Item table and classifications
│   ├── Locations.py               # Location table and definitions
│   ├── Options.py                 # YAML options definition
│   ├── Regions.py                 # World graph & connectivity
│   └── Rules.py                   # Logic rules and access requirements
│
├── Plugin.cs           # BepInEx plugin entry point
└── WoLArchipelago.csproj # C# project file
```

---

## Development & Building (How to Mod)

### Prerequisites
* **Visual Studio 2022** or **.NET SDK** supporting C# 9+.
* **Wizard of Legend** installed on PC.
* **BepInEx 5.x** installed in the game directory.

### Building the C# Mod (`.dll`)

1. Clone the repository:
   ```bash
   git clone [https://github.com/Skelrin/WoLArchipelago.git](https://github.com/Skelrin/WoLArchipelago.git)
   ```
2. Create a `libs/` folder at the root of the project.
3. Copy the following assembly references into `libs/`:
   * From `Wizard of Legend/WizardOfLegend_Data/Managed/`:
     * `Assembly-CSharp.dll`
     * `UnityEngine.dll`
     * `UnityEngine.UI.dll`
   * From `BepInEx/core/`:
     * `0Harmony.dll`
     * `BepInEx.dll`
4. Open `WoLArchipelago.csproj` in Visual Studio and build the solution using :
   ```bash
   dotnet build
   ```

### Building the `.apworld`

To build the Archipelago world file:
1. Compress the contents of the `wizardoflegend/` directory into a `.zip` archive.
2. Rename the extension from `.zip` to `.apworld` (e.g., `wizardoflegend.apworld`).
3. Place `wizardoflegend.apworld` into your Archipelago installation's `custom_worlds/` folder.

---

## License

This project is licensed under the GPL v3 License — see the [LICENSE](LICENSE) file for details.