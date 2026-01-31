# Carbon World - Complete Game Design Overview

## Game Concept

Roguelike factory automation game where players build production chains across hexagonal tiles to fulfill demands from a central core while managing carbon emissions and environmental consequences.

---

## Map Structure

### Central Core

- Located at map center
- Generates random T6 (final tier) material demands
- UI-only interface (cannot enter)
- Displays: active demands, progress bars
- Victory condition: fulfill demands

### Hexagonal Tile Grid

- 150+ hexagonal tiles arranged in 5-6 rings around core
- Adjacent tiles share borders (6 neighbors per hex)
- Materials, power, and resources flow only between adjacent tiles
- Random generation each run

---

## Tile Types

### Fixed Tiles (Randomly Placed Each Run)

**Resource Tiles:**

- Provide specific raw materials (coal, iron, water, stone, etc.)
- Automatically supply to adjacent tiles
- Cannot be entered or interacted with
- Visual marker showing resource type

**Settlement Tiles:**

- Places of interest containing inhabitants with specific needs
- Fulfill item demands to unlock new blueprints
- "Enhancement" of the run through discovery
- Once demands are met, rewards are granted

**Production Tiles:**

- Empty hexes where players build factories
- Enter tile to access current building gameplay (grid-based factory building)
- Place blueprints, set up production chains
- Process materials through any tier (T1→T6)
- Must receive power from adjacent power tile to operate
- Send produced materials to adjacent tiles automatically
- Show total carbon emissions on world map view

**Food Tiles:**

- Specialized production zones for food processing
- Use "Food Processor" blueprints
- Essential for certain supply chains (food logic)
- Input/Output handling similar to production tiles

### Player Tile Conversions

Players can convert production tiles into specialized types (limited by meta-progression slots):

**Power Tiles:**

- Enter and build power generation facilities
- Place "Power" type blueprints
- Powers all 6 adjacent production tiles
- Power output determines effective radius
- Input: Accepts fuel resources from adjacent tiles

**Transport Tiles:**

- Extends specific resource tile's range across map
- Relays that resource to adjacent tiles
- Enables accessing distant resources

**Nature Tiles:**

- Passive carbon absorption
- No resource consumption
- Slower carbon reduction
- Environmental restoration

### Disaster Tiles (Environmental Consequences)

When carbon levels get too high, tiles can transform into disaster zones, blocking their original function:

**Flooded Tiles:**
- Result of rising sea levels (or equivalent high-carbon effect)
- Blocks construction and interaction
- Permanent until carbon is managed?

**Heatwave Tiles:**
- Extreme heat zones
- Blocks normal operation

**Dead Zone Tiles:**
- Complete ecological collapse
- No production or habitation possible

**Refugee Camp Tiles:**
- Spawn due to displacement
- May have special interactions or demands

---

## Material & Production System

### Material Tiers

- **T1:** Raw resources (wood, iron ore, coal, stone, water, fruit)
- **T2:** Basic processing (planks, iron ingots, bricks)
- **T3:** Intermediate materials (steel, refined components)
- **T4:** Advanced components (complex parts)
- **T5:** Pre-final assembly (nearly complete products)
- **T6:** Final products (electronics, machinery, luxury goods - what core demands)

### Blueprint System

**Blueprint Types:**

- **Basic:** Smelter, Furnace, Constructor
- **Advanced:** Assembler
- **Food:** Food Processor
- **Logistics:** Splitter, Merger
- **Power:** Power Generator

**Blueprint Logic:**
- Blueprints act as cards/building instructions
- `IsProducer`: Requires inputs, produces output (Production types)
- `IsPowerGenerator`: Produces power, may consume fuel
- `CarbonEmission`: Most blueprints emit carbon during operation

---

## Carbon System

### Carbon Generation

- **Blueprint-level:** Individual buildings emit carbon when producing materials
- **Power tiles:** Emit based on power generation method (if fuel-based)
- **Global accumulation:** All emissions combine into atmospheric carbon level

### Carbon Reduction

**Nature Tiles:**
- Passive carbon absorption

**Clean Power:**
- Using low-carbon power generation blueprints

---

## Weather System

### Disasters (Tile Transformation)

Instead of temporary events, high global carbon triggers tile transformations into Disaster Tiles:

- **Flooded**
- **Dead Zone**
- **Heatwave**
- **Refugee Camp**

These tiles replace the previous tile function, effectively shrinking the playable map area and breaking supply chains.

---

## Roguelike Structure

### Run Setup

- Map generates with random resource tile placement
- Random settlement tile locations
- Production tiles start empty
- Core generates random sequence of T6 demands
- Apply starting bonuses from meta-progression

### Meta-Progression (Between Runs)

**Material Bank:**
- All produced materials are banked
- Spent on permanent unlocks

**Unlocks:**
- New blueprints
- Blueprint upgrades
- Tile conversion slots
- Starting bonuses

---

## Technical Requirements

### World Map View

- Hexagonal grid display
- Show tile types (icons/colors)
- Display carbon emissions per tile
- Show disaster tiles clearly
- Material flow visualization

### Production/Food/Power Tile View

- Grid-based building placement
- Blueprint selection and placement
- Connection logic (Input -> Machine -> Output)
- Carbon emission particles

---

## Core Gameplay Loop

1. **Map Analysis**: Locate Resources and Settlements.
2. **Expansion**: Convert tiles to Power/Transport to reach resources.
3. **Production**: Build factories in Production/Food tiles.
4. **Supply**: Feed materials to Core for demands or Settlements for unlocks.
5. **Balance**: Manage Carbon to prevent Disaster tile spread.
6. **Progression**: Bank materials for next run.