# World Space Rules

Version: T0.1
Owner: PM

## 1. Core Design Concept

Reference:
- Stardew Valley

Inherited:
- Grid based world management.
- Spatial planning.
- Free management.
- Player driven environment transformation.

Not copied:
- Pixel presentation.
- Map layout.
- Specific assets.

## 2. Three Layer Space Model

### Footprint

Definition:
The Tile space an object actually occupies.

Usage:
- Placement validation.
- Save data.
- Collision.
- Build detection.

Examples:

Small chest:

- 1x1

Large chest:

- 2x1

### Block Area

Definition:
The area around an object where other objects cannot be placed.

Usage:
Prevents:
- Clipping.
- Overlap.
- Operation conflicts.

Example:

Large tree:

- Footprint: 1x1
- Block Area: 3x3

### Visual Area

Definition:
The range the object is actually displayed in.

Allowed:
- Tree canopy expansion.
- Rich building appearance.
- Decoration effects.

Limit:
Visual expansion must not violate the space rules.

## 3. Asset Specification

Every interactive asset must define:

- Footprint.
- Block Area.
- Visual Size.
- Whether it can be moved.
- Whether it can be rotated.

Example:

Large Tree:

- Footprint: 1x1
- Block Area: 3x3
- Visual: 5x5

## 4. Building System Direction

The building system is not Minecraft style free block building.

Approach:
Modular buildings placed on the grid.

Flow:

Resource gathering
↓
Select building
↓
Map preview
↓
Space validation
↓
Confirm placement
↓
Construction phase
↓
Building complete

## 5. Asset Consistency Rules

Forbidden:
- Visual size conflicting with occupancy rules.
- Large models occupying small space.
- Direct import of external assets that violate these rules.