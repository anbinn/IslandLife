# IslandLife Project Status Tracking

Version: T0.1
Owner: PM

## Purpose

Define how project progress, risks, decisions, and milestones are tracked.

## Status Categories

- Planned
- In Progress
- Review
- Accepted
- Blocked

## Tracking Rules

- Every major task must have a clear owner.
- Every completed task must reference GitHub changes.
- Important decisions must be recorded in documents.
- Blocked issues must be identified early.

## PM Review Points

PM reviews:

- Scope completion.
- Code changes.
- Asset changes.
- Performance impact.
- Alignment with product direction.


## Current Execution Status

### IL-WORLD-001 — First Island Production Scene Skeleton
- Status: Accepted
- Implementation commit: `d98cf630d76a2074ef8336154b39393d9938228a`
- PM verification: GitHub diff reviewed; two Allowed Scope files only.
- Result: first-island editable scene skeleton established.

### IL-WORLD-002 — Sprout Lands Terrain Pipeline Validation
- Status: Accepted
- Final implementation commit: `33d29c7b64e2cde71536dfad70cfb391f0673981`
- Merge commit on `main`: `66fe06091979d3c6af14ce73b1bdc807582d06ea`
- PM verification: GitHub diff and Unity Editor result reviewed.
- Result: Sprout Lands import, sprite slicing, Grass RuleTile auto-tiling, 4-frame Animated Tile water, Tilemap terrain, and independent prop placement were validated.
- Scope note: this was a technical pipeline experiment, not the approved final first-island visual target.

### Current Environment Gate
- Formal workspace synchronized to accepted `main`.
- Formal Unity project opened successfully with Unity `6000.3.25f1`.
- Unity MCP `v10.2.0` is running through HTTP Local at `127.0.0.1:8080`.
- Kilo → Unity MCP read-only connectivity verified against `Assets/Scenes/FirstIsland_Prototype.unity`.


## PM Memory Update — 2026-10-05

### Current accepted baseline
- Active branch: `IL-WORLD-003A`.
- Accepted HEAD before the current authoring work: `37585fd51142e3f57282abf6b4946284d2135db6`.
- Asset expansion is paused. Current accepted assets are sufficient for the landscaping stage; additional assets such as beehive/frog are activated only when product need appears.
- `new Wooden Furniture items.png.meta` remains a local HOLD item and must not be committed or overwritten by unrelated work.
- `ProjectSettings/ShaderGraphSettings.asset` remains an excluded local artifact unless explicitly tasked.

### IL-WORLD-004 — superseded experiment
- Status: Stopped / Uncommitted / Superseded.
- No commit and no push were made.
- Experimental local files:
  - `Assets/_Project/Editor/IslandLifeTerrainBrush.cs` (+ meta)
  - `Assets/_Project/Editor/IslandLifeTerrainEditWindow.cs` (+ meta)
  - local modification to `TerrainMapRuntimeLoader.cs`
- These files are candidate implementation material only. They are not accepted project architecture and may be reused, rewritten, or deleted by the replacement task.
- The experiment confirmed that the current first-island terrain is data-driven and that saved scene Tilemaps are not the authoritative terrain source.
- It also exposed an invalid silhouette-freeze validation tied to the current Water/Grass counts. Future validation must verify data/render consistency, not freeze the island to a specific cell count.

### IL-WORLD-004R — Island Map Authoring Foundation
- Status: Planned / replacement for IL-WORLD-004.
- Product direction: map production must use a normal visual Unity workflow: **paint terrain + place scene objects**, not write code or coordinates to author a map.
- Terrain authoritative source remains the existing `TerrainData / TerrainGridData` model.
- Scene Tilemaps are visualization/editing output and must not become a second authoritative terrain database.
- Edit Mode authoring must allow the map to be visible and editable without entering Play Mode.
- Terrain painting must reuse the existing Terrain renderer/resolver/adjacency/AutoTile chain rather than duplicate terrain rules.
- Initial production painting only exposes terrain types whose rendering pipeline is actually production-ready (at minimum current Grass and Water). Enum presence alone does not mean a terrain type is ready for authoring.
- Initial environment props such as Trees, Bushes, and Rocks remain normal Unity Prefab instances in the Scene for authoring: drag/place/move/copy/delete/Undo/Save.
- Do not build a custom object-painting/database system merely for editor landscaping at this stage.

### IL-WORLD-004S — Author Hills Composition System

**004S status: `AUTHOR-GRAMMAR BASELINE LOCKED` — `FREEFORM VISUAL COVERAGE NOT COMPLETE`.**

Locked by IL-WORLD-004S-R11 at commit `759b96e` + R11. The system is finished as a *system*; the
grammar it can draw is deliberately not finished, and that is a recorded fact, not an oversight.

Current production model: **Author Hills Composition System**.
- `Raised` logical data supports **arbitrary authored masks**. Any shape the user paints is stored.
- The Hills visual renderer **only emits author-proven compositions**. It is a visual interpreter,
  never a data editor.
- Logical and visual footprints **may differ**. A front cliff may be drawn one visual row south of
  the logical mask and that cell stays `ElevationLevel.Normal` (`HillVisualTile.OutsideLogicalMask`).
  This is a proven capability and must not be removed in the name of tidiness.
- Shapes with no proven composition are reported as `UNRESOLVED_HILLS_COMPOSITION`: **Raised data
  valid, visual composition unresolved**. Never "invalid", never "illegal", never auto-snapped,
  auto-filled, auto-deleted or auto-rejected.
- **IL-WORLD-004S-R10 direct Hills 47-state mapping: REJECTED.** R10 measured that the Sprout Lands
  47-state coordinate chain resolves 47/47 with 47 unique coordinates, but only **13 of 47** of the
  resulting Hills cells contain any cliff art at all; the sheet has **0** east- or west-facing soil
  faces. No mirroring, rotation, stretching, generated cliff, nearest match or fallback is permitted
  to work around this.

Proven capabilities (all pixel-verified, all in production):
| capability | status |
|---|---|
| rectangle width ≥ 3 | PROVEN |
| rectangle height 1 / 2 / 3 | PROVEN |
| narrow column, width 1, height 1–4 | PROVEN |
| south / front cliff | PROVEN |
| outside-logical-mask cliff projection | PROVEN |

Unresolved (`NOT_PROVEN` — **not** "the author forbade it"): width 2 · rectangle height ≥ 4 ·
convex turn · concave turn · arbitrary L · arbitrary T · U/notch · hole/ring.

Current user map baseline (read-only, IL-WORLD-004S-R11):
- `FirstIsland_TerrainData.asset` SHA256 `BD27211ACDD639F513F3C36F27C2B3530AA5AF7148B5F483BC597D0A5370A4C8`
- **283 Raised cells** in 15 connected regions → **9 proven (48 cells)** / **6 unresolved (235 cells)**
- current renderer output: 75 `HillVisualTile`, 27 outside their own logical mask
- The 44-cell figure from earlier 004S cards is **historical**, not the current baseline.
- Cheapest next win: a **solid 8×4 rectangle, 32 cells**, blocked only by `HEIGHT_NOT_PROVEN`.

Extending the grammar is **evidence-first**, in this order and no other:
```
current user shape demand -> author asset evidence -> exact composition proof -> tests -> production support
```
Semantics must never be inferred from how something looks.

### IL-WORLD-005
- Status: **HOLD**.
- Do not start 005 while 004S freeform visual coverage is incomplete, and do not treat an
  unresolved Raised shape as a blocker for unrelated work.

### Core world-grid / player placement principle
IslandLife is a grid-based world. The same grid concept must support both authored initial scenery and later player modification of the island.

Product rule:
- Except for cells/areas explicitly forbidden by placement rules, the player is intended to be able to place permitted objects on usable cells.
- A crafted chest is a placeable world object and can be placed on a legal free cell.
- A standing tree occupies its required cell(s). After the tree is chopped/removed, its occupancy is released.
- A released legal cell can later be used again, including planting a tree when planting rules allow it.
- Water, protected/special terrain, entrances, occupied cells, or other rule-defined areas may reject placement.
- Placement legality is a gameplay rule and must not be inferred solely from visual appearance.

Architecture consequence:
- **Terrain type** and **object occupancy / placement permission** are separate concepts.
- The editor's initial landscaping and the player's runtime placement should ultimately share the same grid coordinates and core placement/occupancy rules, rather than becoming unrelated systems.
- A designer-placed tree and a player-planted tree should ultimately participate in the same world occupancy semantics.
- A future formal Grid Occupancy / Placement System will be required for runtime gameplay (place chest, plant tree, remove/chop object and release cells, furniture placement, etc.).
- Do **not** prematurely fold that runtime placement system into IL-WORLD-004R. IL-WORLD-004R establishes the map-authoring foundation first; runtime occupancy/placement is a subsequent system task.
- Do not treat the current instruction to avoid a custom `ObjectPlacementData/ObjectGridDatabase` during editor landscaping as a permanent rejection of runtime occupancy data. It is only a scope boundary for the current authoring task.

### Authoring vs runtime responsibilities
- Unity Editor authoring: paint initial terrain and place the initial scenery/prefabs.
- Runtime gameplay: player chops/removes/plants/places permitted objects according to placement and occupancy rules.
- Code: terrain rules, rendering, validation, occupancy/placement legality, and gameplay behavior.
- Map creation must not require the user to edit C# files, serialized text, or manually enter object coordinates.

### Stage order
1. Complete and validate the Island Map Authoring foundation.
2. User manually refines the First Island coastline and then performs/approves landscaping.
3. Define and implement the formal runtime Grid Occupancy / Placement rules before chest placement, tree planting, furniture placement, and similar player construction systems depend on them.
4. Expand assets only on demand during landscaping/gameplay development.

