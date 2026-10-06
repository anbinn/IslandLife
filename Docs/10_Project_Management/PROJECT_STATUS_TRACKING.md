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

**004S status: `FREEFORM RAISED VISUAL PROJECTION SHIPPED` · `RAISED ERASE AVAILABLE` ·
`CONNECTED OUTLINES CLOSED` · `T-JUNCTION INNER CORNERS FIXED` ·
`RAISED ERASE VISUAL REBUILD FIXED` · `JUNCTION GRAMMAR MEASURED AND LOCKED`.**
Grammar baseline locked by R11; freeform coverage by R12; erase and the exterior-boundary fix by R13;
the T-junction inner corner by R14; the erase visual rebuild by R15; the junction and concave corner
grammar audited and locked by R16B.

- **Junction grammar (R16B): measured, and left unchanged on purpose.** All **256 neighbourhoods** of a
  Raised cell were swept in two column contexts (512 cases) and every junction class was rendered and
  inspected. The result is that the grammar is already unified and local: the front run always
  terminates on the author's own `r2c0` / `r2c2` terminal pieces, in all four branch orientations and
  all four L orientations, with no shape name anywhere in the resolver.
  One alternative was implemented, measured and **rejected**: treating ground the shape wraps around as
  a "pocket" and pulling the front wall up into its own cell. It is perfectly symmetric across all four
  orientations and passes the entire topology matrix, but it turns a south-facing platform front into
  dirt windows punched into the top surface. The reasoning is recorded in `RaisedTopologyState.RoleFor`
  so it is not re-attempted.
  An earlier claim that the L orientations were broken was **wrong**: it came from a mis-framed render
  that cut off the top row. With correct framing all four L orientations are clean.
- **What the author art can and cannot express (measured).** All 99 `Hills` slices are exactly
  `16 x 16 px` on one `176 x 144` atlas, so "narrow" is an art *variant*, not a smaller cell. Rows
  r0..r3 columns c0..c3 are the proven grammar. Everything else on the sheet is decorative: soft fade
  gradients, empty cells, and two 2 px silhouette slivers at `r2c9` / `r2c10`. There is **no east/west
  soil face and no corner primitive anywhere in the sheet**. An automated edge scan appeared to find 46
  corner primitives; all 46 were false positives on blend cells and the sheet's outer edge, confirmed by
  rendering each candidate.
- **Two measured families, pinned rather than "fixed".** 32 of 512 neighbourhoods put a displaced cliff
  beside a cap row on the same row; all 32 are cells touching only **diagonally**, i.e. two separate
  masses meeting at a corner, never a connected junction. 192 of 512 mix the narrow `c3` stack with the
  wide `c0..c2` stack inside one author column wherever a column's width changes with height; renders of
  every L and every branch orientation show no visible seam. Both counts are asserted in the committed
  harness so any future change is visible.
- **`ILW004SR16CRealJunctionDiagnostic` reads the real map, not fixtures.** It loads the live
  `FirstIsland_TerrainData`, takes a `CreateGridData` snapshot and never writes: the harness scans its
  own source, with string literals and comments stripped, for `SaveAssets`, `SaveAssetAssets`,
  `Undo.Record`, `SetDirty` and for `SetData`/`SetElevations` on the loaded asset, and SHA256, size,
  mtime and the Raised count are all re-read afterwards. For every Raised cell it reports the raw mask
  in hex and bits, the normalised mask, `RaisedTopologyState`, role, slot, the in-mask versus displaced
  cliff decision and every emitted `HillVisualTile`. Categories are **local mask only** — `CONCAVE_WRAP`,
  `JUNCTION_MEET`, `DIAGONAL_ONLY_TOUCH`, `TERMINAL_TRANSITION`, `DISPLACED_CLIFF_BESIDE_CAP` — with no
  region walk, no span and no shape name.
- **R16C measurement result: the live map currently holds ZERO Raised cells**, so there is no real
  junction topology present to diagnose. This is recorded as a fact, not as a reproduction. Searched
  and confirmed absent: any backup or undo copy of the asset, any other asset in the project carrying
  an `elevations` payload, and any committed revision of the asset with hills (both committed revisions
  are 19-line stubs with no `elevations` field). **The production renderer was not modified in R16C and
  the visual problem is NOT solved.**
- **Projection ownership is structurally safe.** Across all 512 neighbourhoods the maximum number of
  owners reaching for a single visual coordinate is **1**. `AuthorHillsLocalResolver` claims through a
  dictionary under a fixed north-to-south then west-to-east traversal, so a second claimant is refused
  and reported as `VISUAL_CONFLICT` rather than silently overwriting, and no `OutsideLogicalMask` tile
  ever lands on a logical Raised cell. This means a "complex real map differs from a small fixture
  because of write order" explanation is ruled out by measurement, not merely by argument.

Current production model: **Author Hills Composition System, per cell.**
- `Raised` logical data supports **arbitrary authored masks**. Any shape the user paints is stored.
- The Hills visual renderer projects **per logical cell**, from that cell's own 8-neighbour Raised
  topology. `RaisedRegionAnalyzer` is retained for reporting only and **no longer gates rendering**:
  a connected region is never refused as a whole.
- Pipeline: logical mask → `RaisedNeighborResolver` (8-neighbour, read-only) → the shared
  `TerrainMaskNormalizer` → `RaisedTopologyState` (structural role, no art) →
  `AuthorHillsLocalResolver` (author slices) → `HillVisualTile`.
- Logical and visual footprints **may differ**. A front cliff may be drawn one visual row south of
  the logical mask and that cell stays `ElevationLevel.Normal` (`HillVisualTile.OutsideLogicalMask`).
- Only four diagnostics can ever be reported, and all four are renderer faults, never shape verdicts:
  `MISSING_AUTHOR_PRIMITIVE`, `MISSING_CANONICAL_TOPOLOGY`, `VISUAL_CONFLICT`,
  `INVALID_ASSET_REFERENCE`. There is no `NON_RECTANGULAR_REGION` any more.
- **IL-WORLD-004S-R10 direct Hills 47-state mapping: REJECTED.** No mirroring, rotation, stretching,
  generated cliff, nearest match or fallback is permitted.

Proven and shipped: rectangle width ≥ 3 at any height · height 1 / 2 / 3 unchanged and byte-identical
to the R6/R9 oracle · narrow column width 1 height 1–4 · south/front cliff including the author's
second front row · outside-logical-mask cliff projection · **two-wide runs** · **freeform L, T, U,
notch, staircase, zigzag, cross, blob and irregular masses** · **plateaus 4 cells and taller** ·
**rings and holes, with the hole left unfilled**.

- **Inner corners (R14).** A front cliff is emitted for every Raised cell whose south is open, so where
  a vertical branch meets a horizontal front boundary the branch cell has no cliff and the horizontal
  run simply stops. The end of that run is the inner corner. It is drawn with the author's own
  terminal cliff piece — `Hills_r2c2` where the run stops on its right, `Hills_r2c0` where it stops on
  its left — because those are the only author slices whose soil band actually pulls back at that edge
  (measured: bottom-row soil width 16 px for `r2c1` versus 14 px for `r2c0` and `r2c2`). Nothing is
  drawn, mirrored, rotated or stretched, and no composition slot was added.
  The rule walks neighbours through the shared cliff-run extent and contains **no shape name**, so U,
  notch, staircase, zigzag and any concave freeform outline get the same treatment for free. A branch
  at a platform end, or one that splits the front into runs too short to turn, correctly has no inner
  corner at all.
- Raised has a **Paint** and an **Erase** mode, toggled in the Island Map Authoring window and always
  shown in the UI and in the Scene View status line. Erase is a mode of the existing High Ground
  brush, not a second brush system. It writes `ElevationLevel.Normal` only: `TerrainType` is never
  rewritten, world objects and decoration are never touched, erasing an already-Normal cell is a
  no-op, and a drag erase is a single undo step.
- **Stale-visual root cause and fix (R15).** `RenderRaisedVisuals` is the incremental path that
  `RefreshCell` calls into, so it is what every paint and every erase goes through. It used to write
  the new projection with `SetTile` **without ever clearing the previous one**, so every hill tile
  that dropped out of the plan survived on the map forever: erasing produced pillars of old cliff,
  severed front faces, corners belonging to a topology that no longer existed, and regions that
  stayed joined by a shared edge after the row between them was gone. A full `RenderAll` cleared the
  layer first and was always correct, which is exactly why the fault only appeared while editing.
  The Raised visual layer is a pure cache derived entirely from the logical elevation, so it is now
  cleared and rebuilt in full on every refresh — measured at **41.6 ms for a complete rebuild over
  the live 65×45 map**, an upper bound on what a drag stroke pays, so the narrow dirty range was not
  worth keeping. Region splits are handled automatically: topology is recomputed from the new mask,
  never carried over.
- **Hard invariant now asserted.** `Render(A) → erase → rebuild` must be **identical** to
  `fresh render of the final logical mask`, compared on the live Tilemap by sprite identity and
  visual cell coordinate, not by tile count. R13 reported "neighbor rebuild YES / ghost visual NONE"
  while this was broken, because the logical mask and the plan were always correct and only the map
  was stale. Re-running the R15 probe against the unfixed renderer fails **22 assertions** and still
  passes every erase-input, logical-mask and asset-protection check, which is the proof that the new
  tests cover the fault the old ones missed.
- **Exterior-boundary invariant (R13).** For every Raised cell, a side whose neighbour is not Raised
  MUST be drawn, and only a genuinely Raised neighbour may remove an edge. Being in the same
  connected region, or having stopped being isolated, may never remove an edge. This is asserted
  per cell on every fixture and on the live map.
- `Raised` erase re-runs the per-cell production projection, so neighbours are recomputed from the
  new logical mask. That claim was correct about the *plan* but wrong about the *map* until R15:
  the plan was always recomputed while the previously drawn tiles were never withdrawn. Read the
  R15 bullet above, not this sentence, for the guarantee.

Verified on the live user map: every Raised cell closes on all four sides and carries an author hill
tile, with zero renderer faults. The map itself is user-owned and was not modified.

| metric | R11 baseline | R12 | R13 | R14 | R15 |
|---|---|---|---|---|---|
| Raised cells | 283 | 283 | user-owned, now 364 | user-owned | user-owned, now 373 |
| cells drawn with author Hills | 48 | 283 | **all of them** | **all of them** | **all of them** |
| cells with no hill art | 235 | 0 | 0 | 0 | 0 |
| `HEIGHT_NOT_PROVEN` (8×4 plateau) | 32 refused | 0, all drawn | 0 | 0 | 0 |
| cells with a broken exterior boundary | not measured | not measured | **0** | **0** | **0** |
| stale hill tiles left behind by an erase | not measured | not measured | **not measured** | not measured | **0** |
| erase result equals fresh render of the final mask | not measured | not measured | **not asserted** | not asserted | **exact, every case** |

Current user map baseline (read-only, re-read at the start of every card):
- `FirstIsland_TerrainData.asset` SHA256 `084C402E02E6A3C95F0A1C6F035C8541CCC0A902D917E0ECB408E550D3CDF421`
- **373 Raised cells**. Earlier 004S figures (44, then 283, 364, 375) are historical; the user keeps
  painting. **The live file is the only authority.** Worker automated tests never modify it.

Extending the grammar is **evidence-first**, in this order and no other:
```
current user shape demand -> author asset evidence -> exact composition proof -> tests -> production support
```
Semantics must never be inferred from how something looks.

### IL-WORLD-005
- Status: **HOLD**.
- Do not start 005 while 004S is still accumulating Hills coverage evidence.

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

