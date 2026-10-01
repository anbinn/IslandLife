# First Island Visual Prototype

Version: T0.1
Owner: PM

## 1. First Island Positioning

Definition:
The first island is the player's initial home island, entered right after the game starts.

Design goal:
- Starting point for island survival and life simulation.
- Player gradually transforms the island.
- Provides development space for exploration, gathering, building, and farming.

Visual direction references Stardew Valley:
- 45-degree top-down view.
- Warm natural atmosphere.
- Small world growth feel.

Not copied:
- Pixel assets.
- Map layout.
- Specific assets.

## 2. World Shape Rules

The first island must read as a real island.

Requirements:
- Surrounded by sea.
- No square map presentation.
- Naturally irregular coastline.
- Contains beach, rocky shore, and shallow sea transition.

Logic:
The world uses a Tile Grid.
The map boundary is not the island boundary.
The island is one node inside an ocean world.

## 3. First Island Region Division

### A. Spawn Beach Area

Location:
The player's initial landing area.

Must contain:
- Beach.
- Shipwreck debris.
- Driftwood.
- Initial supply crate.
- A small number of palm trees.

Purpose:
Tutorial area.
Provides starting resources and a survival entry point.

### B. Living And Building Area

Purpose:
The player's first home area.

Requirements:
- Flat terrain.
- Reserved building space.
- No excessive natural obstacles.

Future use:
- Player house.
- Farmland.
- Workbench.
- Storage.
- Campfire.

### C. Forest Area

Purpose:
Basic resource area.

Requirements:
- Natural tree distribution.
- Preserved player traversal space.
- Regular arrangement is forbidden.

Resources:
- Wood.
- Tree seeds.
- Basic gathering resources.

### D. Lake Area

Purpose:
Natural landscape and gameplay area.

Requirements:
- Lake position is fixed.
- The main lake is not randomly generated.
- Natural vegetation transition around the shore.

Future use:
- Fishing.
- Water resource.
- Special ecology.

### E. Slope / Highland Area

Reference:
Height difference presentation of Stardew Valley.

Requirements:
- Not a large scale 3D mountain.
- Uses slope, rock wall, and layered height.

Purpose:
- Increases map layering.
- Provides an exploration area.

### F. Cave Entrance

Rules:
Caves exist attached to the highland area.

Presentation:
- Rock wall.
- Dark entrance.
- Loose rocks around the entrance.

Purpose:
Entry point for future underground exploration.

## 4. Resource Distribution Principles

Approach:
Fixed map skeleton plus semi random resources.

Fixed:
- Lake.
- Caves.
- Special landscapes.
- Major regions.

Generatable:
- Ordinary trees.
- Grass.
- Small stones.
- Decorations.

Forbidden:
Generating the entire island fully at random.

## 5. Tree Rules V1

Decision:
Initial trees on the map are placed by design.

Player chopping a tree:
Gains:
- Wood.
- Tree seed x1.

After chopping:
- The tree disappears.
- The tile is released.

The player can then plant the seed.

Not considered in V1:
- Tree stumps.
- Automatic random tree respawn.
- Complex ecology simulation.

## 6. Unity Editability Principles

Future map implementation must support:
- Direct inspection in the Unity Editor.
- Prefab adjustment.
- Mouse drag repositioning.
- Saving of manual adjustments.

Forbidden:
Generating all map elements with hardcoded coordinates in code.

Goal:
Designers can directly adjust:
- Tree positions.
- Decoration positions.
- Building areas.
- Landscape layout.

## 7. Stage Limitations

This task establishes the design specification only.

Not modified:
- Unity project.
- Game code.
- Scene files.

Not implemented:
- Building system.
- Farming system.
- Inventory system.
- Player flow.