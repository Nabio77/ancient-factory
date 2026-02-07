# Ancient Factory - Complete Game Design Overview

## Game Concept

Roguelike factory automation game where players build production chains across hexagonal tiles to fulfill demands from a central core and surrounding settlements. Players must balance industrial expansion with divine favor - angering the gods through excessive production triggers disasters that destroy your factory.

---

## Core Systems

### Database System
Central registry for all game data. Provides instant lookup of items and blueprints by ID.
- Manages `ItemDefinition` and `BlueprintDefinition` assets
- Auto-populates from project assets in the editor

### Factory System
The main production loop that processes all factory tiles each tick (default: 1 second).
- Iterates through all factory tiles and their production nodes
- Checks workforce availability before allowing production
- Distributes produced items to connected settlements
- Example: A Smelter blueprint processes Iron Ore → Iron Ingot each tick if workers are available

### Workforce System
Manages worker allocation from settlements to factories.
- Settlements provide base workforce (50 workers each)
- Housing tiles connected to settlements add residents as additional workforce
- Factories require workers matching their blueprint's `WorkforceRequirement`
- Uses BFS from settlements to determine which factories are within commute radius
- No workers = no production
- Example: A Steel Foundry requiring 10 workers won't produce if the nearest settlement only provides 5 reachable workers

### Inventory System
Tracks the global player inventory and per-tile inventories.
- Central inventory for collected/banked items
- Each tile has its own local inventory for buffering
- Supports batch operations and change events for UI updates

### Tech Tree System
Manages blueprint unlocking and progression.
- Items are auto-unlocked as starting points
- Blueprints require tech points to unlock
- Tech points are earned by fulfilling core demands
- Validates prerequisites before allowing unlocks
- Example: Unlock "Advanced Smelter" for 50 tech points after unlocking "Basic Smelter"

### Divine Displeasure System
The consequence system - replaces traditional pollution/carbon mechanics with divine judgment.
- Production generates displeasure (especially from slave labor blueprints)
- Nature tiles and temples generate divine favor to offset displeasure
- Divine states progress: Blessed → Favored → Tolerated → Displeased → Wrathful → Forsaken

**Disaster Thresholds:**
| Displeasure | State | Consequences |
|-------------|-------|--------------|
| 0-49 | Blessed/Favored | No disasters |
| 50-99 | Tolerated | Minor warnings |
| 100-199 | Displeased | Floods, Desert expansion begin |
| 200-349 | Wrathful | Plague added |
| 350+ | Forsaken | All disaster types active |

**Disaster Types:**
- **Flooding** - Spreads from water/edges, blocks tile function
- **Plague** - Reduces production efficiency, spreads slowly
- **Desert Expansion** - Converts tiles to wasteland
- **Cursed Ground** - Divine curse, hardest to remove
- **Slave Revolt** - Workers rebel, removes workforce from area

### Settlement System
Generates and tracks demands for each settlement on the map.
- Each settlement generates 2-4 random item demands
- Demand tier scales with distance from core (far = T1-T2, near = T4-T5)
- Fulfilling demands grants rewards (tech points, population growth)
- Population growth increases workforce available
- Example: A distant settlement demands 10 Bread (T2), while one near the core demands 5 Steel Beams (T4)

### Ritual System
Allows players to reduce divine displeasure through offerings.
- **Gold Offering:** -20 displeasure per gold unit
- **Food Offering:** -5 displeasure per food unit
- **Wine Festival:** -50 displeasure (costs 5 wine, 30 tick cooldown)
- **Grand Feast:** -100 displeasure (costs 3 feast items)

### Interface System
Manages UI state transitions between game modes.
- **States:** Gameplay, FactoryEditor, TechTree, Menu
- Locks/unlocks input when switching modes
- Saves/restores camera position between states

### Notification System
Event-driven notification broadcasting for UI feedback.
- Types: Info, Warning, Error
- Example: "Settlement demand fulfilled!" or "Divine wrath approaches!"

### Tile Event System
Batches tile change notifications for efficient UI updates.
- Change types: InventoryChanged, GraphUpdated, WorkforceChanged, Replaced
- Prevents UI thrashing by batching updates per frame

---

## Production Systems

### Factory State Machine
Orchestrates individual blueprint node production cycles within factory tiles.
- **States:** Idle → Producing → OutputReady
- Validates input availability before starting production
- Tracks elapsed time and production progress
- Transfers outputs to buffers or directly to core
- Example: Smelter in Idle state checks for Iron Ore input → starts Producing for 2 seconds → enters OutputReady with Iron Ingot

### Factory Input System
Resolves input requirements by tracing connections to source tiles.
- Pulls items from adjacent resource tiles or factory buffers
- Validates sufficient resources before production starts
- Supports simulation mode for factory planning

### Settlement Supply
Auto-distributes factory outputs to settlements that need them.
- Transfers outputs matching settlement demands
- Handles transport tile relay for distant deliveries
- Prevents over-delivery beyond demand quantities

---

## World Systems

### World Map
Central world state manager and tile container.
- Loads/saves tile data for persistence
- Provides tile lookup by position and neighbor queries
- Coordinates with World Map Generator for procedural generation
- Fires events on map changes for UI synchronization

### World Map Generator
Procedurally generates the hexagonal world each run.
- Creates tile layout based on generation profile
- Randomly places resource tiles (coal, iron, water, stone, etc.)
- Distributes settlement tiles at varying distances from core
- Initializes the central core tile
- Seeds housing and disaster potential

### World Map Visualizer
Renders the hex grid and tile visuals.
- Manages tilemap sprite rendering
- Updates tile appearances based on type and state
- Shows resource overlays and disaster effects

### Tile Graph System
Visualizes production connections between tiles.
- Draws arrows showing item flow direction
- Color-codes connections by source type
- Shows potential supply chains for planning

---

## Tile Types

### Core Tiles
| Tile | Function |
|------|----------|
| **Core** | Central demand hub. Generates T6 demands. Accumulates tech points when demands are fulfilled. Victory condition. |
| **Factory** | Production grid where players place blueprints. Requires workforce to operate. |
| **Resource** | Provides raw materials (coal, iron, water, stone, wood). Auto-supplies to adjacent tiles. |
| **Settlement** | Population center with item demands. Provides base workforce. Grants rewards when demands are met. |
| **Transport** | Extends resource tile reach across the map. Relays specific resources to adjacent tiles. |
| **Nature** | Environmental restoration. Generates divine favor. Reduces displeasure passively. |
| **Housing** | Provides additional workforce when connected to a settlement. |
| **Temple** | Religious building. Generates divine favor to offset production displeasure. |

### Disaster Tiles
| Tile | Cause | Effect |
|------|-------|--------|
| **Flooded** | High displeasure + water proximity | Blocks all tile function |
| **Plague** | High displeasure | Reduces production efficiency, spreads |
| **Desert Expansion** | High displeasure | Converts to wasteland, spreads slowly |
| **Cursed Ground** | Extreme displeasure | Divine curse, very hard to remove |
| **Slave Revolt** | Slave labor + high displeasure | Removes workforce from area |

---

## Data Models

### ItemDefinition
Defines all items in the game.
- **Tier** (1-6): Determines complexity and when it's demanded
- **ProductionTime**: How long to craft
- **IsFood**: Whether it satisfies food demands
- **TechPoints**: Points granted when delivered to core
- **DivineDispleasure**: How much the gods dislike this item being made

### BlueprintDefinition
Defines buildings/recipes that can be placed in factories.
- **Inputs/Outputs**: Required materials and produced items
- **WorkforceRequirement**: Workers needed to operate
- **IsProducer**: Whether it transforms inputs to outputs
- **IsPowerGenerator**: Whether it generates power (legacy)
- **DivineDispleasure**: Displeasure generated per production cycle
- **UnlockCost**: Tech points required to unlock
- **IsStarterCard**: Whether it's available from the start

### BlueprintGraph
Production layout within a factory tile.
- Contains blueprint nodes and their connections
- Manages input/output nodes for tile-to-tile flow
- Validates production chains

---

## Core Gameplay Loop

1. **Analyze Map**: Locate resources, settlements, and plan routes to core
2. **Build Workforce**: Ensure settlements and housing provide enough workers
3. **Establish Production**: Place blueprints in factory tiles to process materials T1→T6
4. **Supply Settlements**: Fulfill settlement demands to unlock new blueprints and earn rewards
5. **Feed the Core**: Deliver final products to core to earn tech points
6. **Manage Divine Favor**: Build temples, nature tiles, or perform rituals to prevent disasters
7. **Unlock Technology**: Spend tech points to unlock advanced blueprints
8. **Survive Disasters**: React to divine punishment by removing disaster tiles or reducing displeasure

---

## Victory & Failure

**Victory:** Fulfill all core demands before being overwhelmed by disasters

**Failure:** Divine displeasure reaches maximum (Forsaken state) and disasters consume critical infrastructure

---

## Meta-Progression (Between Runs)

- **Material Bank**: Produced materials persist between runs
- **Permanent Unlocks**: New blueprints, upgrades, starting bonuses
- **Tile Conversion Slots**: Ability to convert more tiles to specialized types
