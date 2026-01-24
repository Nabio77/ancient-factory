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

**Enhancement Tiles:**

- Combine any blueprint + any materials = enhanced blueprint
- Free experimentation system
- One enhancement per tile (permanently consumed)
- Enhanced blueprints have improved stats (speed, efficiency, carbon reduction)

**Production Tiles:**

- Empty hexes where players build factories
- Enter tile to access current building gameplay (grid-based factory building)
- Place blueprints, set up production chains
- Process materials through any tier (T1→T6)
- Must receive power from adjacent power tile to operate
- Send produced materials to adjacent tiles automatically
- Show total carbon emissions on world map view

### Player Tile Conversions

Players can convert production tiles into specialized types (limited by meta-progression slots):

**Power Tiles:**

- Enter and build power generation facilities
- Types: Coal (high output, high carbon), Solar (low output, zero carbon), Hydro (medium output, zero carbon, requires water), Nuclear (very high output, moderate carbon)
- Powers all 6 adjacent production tiles
- Tech tree unlocks advanced power types during run

**Transport Tiles:**

- Extends specific resource tile's range across map
- Relays that resource to adjacent tiles
- Enables accessing distant resources

**Nature Tiles:**

- Passive carbon absorption
- No resource consumption
- Slower carbon reduction
- Environmental restoration

**Scrubber Tiles:**

- Active carbon removal
- Consumes resources (power/materials)
- Faster carbon reduction than nature tiles
- Enter and build scrubber infrastructure

---

## Material & Production System

### Material Tiers

- **T1:** Raw resources (wood, iron ore, coal, stone, water, fruit)
- **T2:** Basic processing (planks, iron ingots, bricks)
- **T3:** Intermediate materials (steel, refined components)
- **T4:** Advanced components (complex parts)
- **T5:** Pre-final assembly (nearly complete products)
- **T6:** Final products (electronics, machinery, luxury goods - what core demands)

### Production Flow

- Each tier requires inputs from lower tiers
- Players trace recipes backward from T6 to determine production chain needs
- Materials automatically flow between adjacent tiles
- Any production tile can produce any tier (flexible placement)
- Tiles must be powered to operate

### Blueprint System

**Base Blueprints:**

- Blueprint is like a card
- Always available to all players
- Basic versions of all building types
- Sufficient to complete any production chain
- Can have 1-4 inputs but only one output
- Can connect to other blueprints with input / output
- Divided into blueprint types that handle different tiers of items

**Enhanced Blueprints:**

- Created at enhancement tiles
- Combine base/unlocked blueprint + materials
- Better stats based on material combinations
- Discovery/experimentation element
- Single-use like all blueprints

---

## Point & Tech Tree System

### Point Generation

- Production tiles earn points when completing production cycles
- **Base rate:** 1 point per completion
- **Adjacency bonus:** 2 points if ANY adjacent tile is also producing (binary, not stacking)
- Encourages clustering production chains

### Tech Tree

- Spend points to unlock advanced blueprints during run
- Organized by building type or production tier
- Strategic choices: unlock early vs save points
- Unlocks permanent for that run only
- Resets each run

---

## Carbon System

### Carbon Generation

- **Blueprint-level:** Individual buildings emit carbon when producing materials
- **Different buildings:** Different carbon costs per production cycle
- **Power tiles:** Emit based on type (coal = high, solar/hydro = zero, nuclear = moderate)
- **Tile-level view:** Production tiles show total emissions (sum of all blueprints inside)
- **Global accumulation:** All emissions combine into atmospheric carbon level

### Carbon Reduction (During Run)

**Nature Tiles (conversion):**

- Passive carbon absorption
- Free operation, slower reduction

**Scrubber Tiles (conversion):**

- Active carbon removal
- Consumes resources
- Faster reduction

**Clean Power (tech tree):**

- Unlock solar/hydro/nuclear during run
- Zero or reduced emissions at source

**Enhanced Blueprints (enhancement):**

- Reduce specific blueprint's carbon emissions
- Same production output, less pollution

---

## Weather System

### Carbon Thresholds

- **0-25%:** Clear (no events)
- **25-50%:** Minor events begin
- **50-75%:** Moderate events
- **75-100%:** Severe events
- **100%+:** Catastrophic environmental collapse

### Weather Events (Tile Blocking)

Event types: Storms, refugee camps, ice/freezing, floods, drought, heat waves

**Event Behavior:**

- Spawn on tiles when carbon crosses thresholds
- Completely block affected tiles (cannot use/enter)
- **Permanent until carbon level changes**
- Events clear when carbon drops below triggering threshold
- More tiles affected as carbon rises
- No player interaction needed - purely carbon-based

---

## Roguelike Structure

### Run Setup

- Map generates with random resource tile placement
- Random enhancement tile locations
- Production tiles start empty
- Core generates random sequence of T6 demands
- Apply starting bonuses from meta-progression

### Win Conditions

- Fulfill X number of demands
- Grow core to maximum level
- Achieve specific production milestones
- Maintain carbon balance while producing

### Failure States

- Miss too many timed demands
- Carbon levels reach catastrophic (100%+)
- Core growth stalls from lack of production

### Run Variety

- Different resource distributions require different strategies
- Random demand order forces adaptation
- Enhancement tile locations affect upgrade paths
- Each run plays differently

---

## Meta-Progression (Between Runs)

### Material Bank

- **ALL materials produced during runs saved permanently**
- Includes T1 raw materials through T6 final products
- Accumulated across all runs
- Spent on permanent unlocks in meta-progression menu

### Meta-Progression Unlocks (Spend Banked Materials)

**Tile Conversion Capacity:**

- Power tile slots: Start 1, unlock up to 5-10
- Transport tile slots: Start 0, unlock up to 3-5
- Nature tile slots: Start 0, unlock up to 3-5
- Scrubber tile slots: Start 0, unlock up to 3-5

**Power Source Unlocks:**

- Start with coal power only
- Unlock solar (low carbon, moderate output)
- Unlock hydro (zero carbon, requires water resource)
- Unlock nuclear (very high output, moderate carbon)

**Blueprint Library:**

- Unlock permanent base blueprints (more building variety)
- Pre-unlock tech tree branches (start runs with advanced blueprints)
- Unlock enhanced blueprint recipes

**Production Efficiency:**

- Permanent production speed multipliers
- Carbon emission reduction across all buildings
- Power generation efficiency boosts
- Material processing bonuses

**Starting Advantages:**

- Begin runs with pre-converted tiles already placed
- Start with partial core growth level
- Begin with starter materials in inventory
- Start with bonus points for tech tree

**Carbon Management:**

- Increase nature/scrubber tile carbon absorption rates
- Reduce blueprint carbon emissions globally
- Raise weather event thresholds (events trigger at higher carbon)

**Map Knowledge:**

- Reveal resource tile locations at run start
- Show enhancement tile positions
- Preview first T6 demand before run begins

**Challenge Modes:**

- Unlock harder difficulty modifiers
- Special constraints (limited power, scarce resources, timed runs)
- Reward bonus materials for completing challenges
- Achievement system granting permanent bonuses

---

## Core Gameplay Loop

### Per-Run Loop

1. Core displays T6 demand (e.g., "500 Advanced Electronics")
2. Check recipe requirements, trace back through tiers (T6→T5→T4→T3→T2→T1)
3. Identify needed raw resources, locate resource tiles on map
4. Convert tiles strategically:
    - Place power tiles near resources
    - Place transport tiles to extend resource range
    - Place nature/scrubber tiles for carbon management
5. Build production tiles processing materials through tiers
6. Enter production tiles, place blueprints, set up factories
7. Production completes cycles, earn points (1 base, 2 with adjacency)
8. Spend points unlocking tech tree blueprints
9. Optionally enhance blueprints at enhancement tiles
10. Materials auto-flow through adjacent tiles toward core
11. Monitor carbon levels, manage weather events
12. Fulfill demand → core grows → new T6 demand spawns
13. Balance production expansion vs carbon management
14. Run ends (victory/failure)
15. ALL materials produced saved to material bank

### Between-Run Loop

16. Spend banked materials on meta-progression unlocks
17. Permanent upgrades carry into next run
18. Start new run with improved capabilities
19. Repeat with increasing mastery

---

## Key Design Principles

### Strategic Tension

- **Production vs Carbon:** More production = more carbon = more blocked tiles
- **Expansion vs Sustainability:** Grow fast or build sustainably
- **Points allocation:** Unlock blueprints now or save for better unlocks later
- **Tile conversion:** Limited slots force strategic choices

### Adjacency Matters

- Materials only flow between adjacent tiles
- Power only reaches adjacent tiles
- Adjacency bonus for point generation
- Clustering creates efficiency but limits flexibility

### Discovery & Experimentation

- Enhancement tiles encourage experimentation
- Tech tree unlocking provides progression
- Different strategies each run
- Learn optimal production chains

### Long-Term Progression

- Material bank creates permanent growth
- Meta-unlocks provide increasing power
- Each run builds toward future runs
- Roguelike permadeath balanced by progression

### Environmental Theme

- Carbon management is core mechanic, not optional
- Weather events provide real consequences
- Multiple paths to sustainability
- Balance economy and environment

---

## Technical Requirements

### World Map View

- Hexagonal grid display
- Show tile types (resource icons, production status, conversions)
- Display carbon emissions per tile
- Show weather events blocking tiles
- Material flow visualization between tiles
- Carbon level UI with thresholds
- Core demand panel
- Tile conversion interface

### Production Tile View (Current Gameplay)

- Grid-based building placement
- Blueprint selection and placement
- Production chain setup
- Material flow within tile
- Carbon emission display per blueprint
- Return to world map

### Tech Tree Interface

- Display available and locked blueprints
- Show unlock costs in points
- Preview blueprint stats
- Unlock confirmation

### Enhancement Tile Interface

- Select blueprint to enhance
- Select materials to combine
- Preview/experiment with combinations
- Confirm enhancement

### Meta-Progression Menu

- Display material bank inventory
- Show available unlocks
- Display unlock costs
- Purchase confirmation
- Track achievements/challenges

### UI/UX Requirements

- Clear tile type identification
- Material flow visualization
- Carbon level monitoring
- Demand tracking
- Point accumulation display
- Conversion slot availability
- Weather event warnings

---

## Visual Design

### World Map

- Isometric or top-down hexagonal grid
- Clear tile type differentiation (color coding, icons)
- Resource tiles: Natural aesthetics (trees, ore veins, water)
- Production tiles: Industrial structures visible from map
- Power tiles: Distinct power plant visuals
- Nature tiles: Forest/greenery
- Scrubber tiles: Carbon capture facilities
- Weather effects: Visual overlay on blocked tiles
- Carbon visualization: Global atmospheric effect

### Production Tile Interior

- Current factory building gameplay maintained
- Grid-based placement system
- Blueprint ghosts for placement
- Material flow indicators
- Carbon emission particles/effects
- Power connection status

### UI Elements

- Clean, readable fonts
- Color-coded materials
- Carbon level with threshold markers
- Demand cards with progress
- Tech tree node visualization
- Material bank inventory grid

---

## Audio Design

### Ambient

- Natural sounds (outer rings): Wind, water, wildlife
- Industrial sounds (production areas): Machinery, processing
- Environmental feedback: Storm sounds during weather events

### Feedback

- Production completion chimes
- Blueprint unlock notifications
- Demand fulfillment celebration
- Warning sounds for high carbon
- Weather event alerts
- Material flow audio cues

---

## Development Priorities

### Phase 1: Core Systems

1. Hexagonal tile map generation
2. Basic tile types (resource, production, power)
3. Material tier system (T1-T6)
4. Adjacency-based flow
5. Simple production tile building (existing gameplay)
6. Core demand system
7. Basic UI (map view, tile view)

### Phase 2: Blueprint & Progression

1. Tech tree system
2. Point generation from production
3. Blueprint unlock system
4. Enhancement tile system
5. Blueprint enhancement mechanics

### Phase 3: Carbon & Weather

1. Carbon emission from blueprints
2. Global carbon tracking
3. Weather event system
4. Tile blocking mechanics
5. Nature and scrubber tiles
6. Carbon reduction systems

### Phase 4: Meta-Progression

1. Material bank system
2. Between-run menu
3. Meta-unlock system
4. Conversion slot limits
5. Permanent upgrade system

### Phase 5: Polish & Content

1. Additional material types
2. More blueprint varieties
3. Enhanced visual feedback
4. Audio implementation
5. Tutorial/onboarding
6. Challenge modes
7. Balance tuning

---

## Success Metrics

### Engaging Core Loop

- Players understand tile adjacency strategy
- Clear feedback on production chains
- Satisfying demand fulfillment
- Meaningful carbon management decisions

### Roguelike Replayability

- Each run feels different
- Meta-progression provides tangible improvement
- Failure teaches without frustrating
- Victory feels earned

### Strategic Depth

- Multiple viable strategies
- Interesting tile conversion choices
- Tech tree unlocking creates builds diversity
- Carbon management adds meaningful constraint

### Long-Term Engagement

- Material bank encourages continued play
- Meta-unlocks provide long-term goals
- Challenge modes extend replayability
- Discovery of enhancement combinations

---

This document serves as the complete design specification for Carbon World. All systems interconnect to create a strategic factory automation roguelike with environmental consequences at its core.