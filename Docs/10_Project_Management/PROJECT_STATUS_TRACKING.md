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
`RAISED ERASE VISUAL REBUILD FIXED` · `JUNCTION GRAMMAR MEASURED AND LOCKED` ·
`BASIC STRAIGHT GRAMMAR CORRECTED` · `R18 = UNITY_PASS` · `LEFT/RIGHT CORNER GRAMMAR SHIPPED` ·
`R19 = UNITY_PASS` · `TOP CORNER GRAMMAR SHIPPED`.**
Grammar baseline locked by R11; freeform coverage by R12; erase and the exterior-boundary fix by R13;
the T-junction inner corner by R14; the erase visual rebuild by R15; the junction and concave corner
grammar audited and locked by R16B; the basic straight grammar corrected by R18 (**user-confirmed
UNITY_PASS**); the author left and right front corners by R19 (**user-confirmed UNITY_PASS**); the
author left and right **top** corners by R20.

- **P-junction: ROOT CAUSE MEASURED, implementation BLOCKED pending PM.** PM confirmed the P topology is
  **not** a missing-asset problem and locked three author anchors for it: user cell 1 = `Hills_r2c4`,
  cell 2 = `Hills_r2c0`, cell 3 = `Hills_r2c5`. All three slices exist on the sheet as real author art
  (`r2c0` rect (0,96) already proven; `r2c4` rect (64,96); `r2c5` rect (80,96)). **Measured: `r2c4` and
  `r2c5` are NOT referenced by the production `AuthorHillsCompositionSet`, so nothing in the projection
  can emit them today.**
  - **Root cause, pinned to the branch and measured, not argued.**
    `RaisedTopologyState.cs` / `RaisedTopologyState` / **`RoleFor`, the `if (runDepth == 2)` branch,
    returning `MIDDLE_SURFACE` for `offsetFromRunBottom == 0` when `frontInMask == false`.**
    Sweeping all 256 neighbourhoods in two column contexts, **128 of 512 neighbourhoods emit a displaced
    tile and 128 of 128 of them come from that one branch, all with `runDepth = 2, isNarrow = False`**.
    Example owner `(5,4)` raw `0x1A`, `runDepth 2`, `offset 0`, slot `BODY`, role `MIDDLE_SURFACE`,
    emitting an extra tile at `(5,3)`.
    Why it is wrong: `RaisedTopologyState.DrawsFrontCliffBelow` is defined as
    `ExposedSouth && Role < FRONT_CLIFF`, and `AuthorHillsLocalResolver.Resolve` under
    `if (topology.DrawsFrontCliffBelow)` is the **only** place in the whole projection that emits a
    `HillVisualTile` at a coordinate other than its own logical cell. Painting one cell can raise a
    column from depth 1 to depth 2 and make it two-wide at the front, at which point the author's wall no
    longer fits that cell under the old depth test and the wall is pushed one row south instead.
  - **Reproduced end to end on real fixtures.** L `XXX / X..` resolves to 4 visual tiles and **0 outside
    the mask**, which matches "the L is visually correct". Painting the fifth cell to give
    `XXX / XX.` produces **5 logical Raised cells but 7 visual tiles and 2 outside the mask** —
    `(4,3) = Hills_r2c0` emitted by `(4,4)` and `(5,3) = Hills_r2c2` emitted by `(5,4)` — which is
    exactly the reported "extra Hills block generated downward out of nowhere".
  - **Why the implementation is BLOCKED and was not guessed.** Implementing the anchors requires two
    things the card does not carry: the numbered fixture diagram that maps PM's user cell ids 1..5 onto
    logical coordinates, and the *semantic* of `Hills_r2c4` and `Hills_r2c5` — i.e. which local adjacency
    means the west corner-wall and which means the east one. The card states only the fixture-level
    mapping ("cell 1 is r2c4"), and section 6 forbids deriving a production rule from the fixture, while
    section 22 forbids Worker guessing an un-locked sprite semantic. Any rule written from the fixture
    alone would be the hard-coding the card forbids. Committed evidence harness:
    `ILW004SPJunctionAudit` (read-only, no production change).
- **R23A2 raw-vs-canonical collapse audit — the 9 collisions are LEGACY_RUN_OVERRIDE, all 9. Answer C,
  and R23A's STOP is REFINED rather than confirmed.**
  - **Classification tally, measured over 512 probes / 47 canonical states:** `RAW_DISTINGUISHABLE = 0`,
    `CANONICAL_COLLAPSE = 0`, **`TRUE_LOCAL_AMBIGUITY = 0`**, `LEGACY_RUN_OVERRIDE = 9`.
  - **So it is NOT A and NOT B.** The author grammar is **not** proven to need more than 3x3, because
    card section 5's licensing condition requires a `TRUE_LOCAL_AMBIGUITY` and there are none. And the
    normalizer is **not** destroying author information, because no collision is a canonical collapse.
  - **Every one of the 9 is the same raw 3x3 producing different Sprites**, and the only thing that
    differs is how far the run walk reached. Current production output is explicitly not accepted as
    proof that the difference is correct, which is exactly the card's section 5 rule.
  - **The witness grid is now self-proving, and that fix matters.** The mask is read back OUT of the
    constructed grid and compared with the requested mask, asserted per record:
    `Encode(grid) == requested raw mask`, **0/512 disagreements**. R23A printed its grids from a
    confused row index; those grids should no longer be read as evidence.
  - **The locked straight grammar in RAW 3x3 terms, which is the load-bearing result.** Each piece is
    distinguishable from raw alone, through **cardinal** bits:
    - horizontal LEFT   `. . . | . C . | . X X`   (E only, no W)
    - horizontal BODY   `. . . | . C . | X X X`   (E and W, both diagonals open)
    - horizontal RIGHT  `. . . | . C . | X X .`   (W only, no E)
    - vertical TOP      `. . . | . C . | . X .`   (S only)
    - vertical BODY     `. X . | . C . | . X .`   (N and S)
    - vertical BOTTOM   `. X . | . C . | . . .`   (N only)
    The E-only / E+W / W-only and S-only / N+S / N-only patterns are **different cardinal patterns, so
    raw 3x3 carries the terminal-versus-body signal.**
  - **This CORRECTS R23A's own conclusion.** R23A asserted that slot is irreducibly a run property and
    that a pure 3x3 Layer A therefore cannot work. The evidence says otherwise: the signal is present in
    raw, and the loss happens LATER, in `SlotFor`, `CliffSlotFor`, `RunContinuesThrough` and the run
    walk, which re-derive the slot from an unbounded outward read instead of reading the cardinal the
    cell already has. So a 3x3-only Layer A is **not proven impossible**; it is proven un-attempted.
  - **Consequence for R23B:** the blocker is no longer architectural. It is that the current slot
    derivation must be replaced by a raw-cardinal derivation, and that replacement has to be proven
    against the R18/R19/R20 oracles one piece at a time. Nothing was implemented this round, per
    section 7.- **R26 STOPPED as `BLOCKED_PM_DECISION` again, at COMMIT A. NO production file was changed.** The R26
  blocker from last card was not addressed by the card, and stage A established two further facts that
  make it unaddressable by inference. Nothing was implemented, no role was named, no slice was wired.
  - **THE LIVE MAP CHANGED AGAIN, by the user, mid-card.** R25 card end `6986B2C4…`, 53 Raised. R26 start
    read `757A0A44AB3379E23FB1F21AFDA63BBD099A90A34634CD37C5723AE82275A607`, **41 Raised**, mtime 05:24.
    The user removed several block corners and added a new cross/stair structure at x -20..-16,
    y -7..-3. Treated as authority, never written, restored or reverted.
  - **FINDING 1, AND IT IS NEW PROGRESS: THE R25 `0xD0` COLLISION IS NOT RESOLVED - BUT IT IS SEPARABLE
    AT 5x5. Those are different statements and the difference matters.**
    - **Not resolved:** the user's composition site `(0,-5)` and the R11-locked 3x3 rectangle's top-left
      still have the **IDENTICAL raw 3x3 `0xD0`**. Deleting the four other corners removed the visible
      copies, not the conflict, so the collision was **hidden rather than fixed** and a `0xD0` rule would
      still break every 3x3 rectangle.
    - **Separable at 5x5, measured:** the two 5x5 stamps genuinely differ -
      user site `..... / ..... / ..### / ..##. / ..#..` versus rectangle `..... / ..... / ..### / ..### / ..###`.
      The whole difference is that the user's mass **steps away to the south-east** where the rectangle
      stays solid. That is a finite, named, explainable predicate inside 5x5, and R26 section 4 permits
      exactly that radius. **So R25's blocker is real but soluble - at 5x5, not at 3x3.**
    - **R25 ALSO HAD A BUG, now fixed.** Its `RECTANGLE_TOP_LEFT_IS_NOT_IDENTICAL_TO_FAMILY_C` check
      passed for the wrong reason: it encoded the rectangle's **origin row instead of its top row**, so it
      compared the wrong cell and reported "they differ" when they are in fact identical. R26A recomputes
      it correctly and the correct answer is `0xD0 = 0xD0`.
  - **FINDING 2, AND IT IS THE HARD BLOCKER: THE CARD SUPPLIES NO COORDINATE FOR 9 OF ITS 11 COMPONENTS,
    and no screenshot reached the session.** Only `r2c4` is located anywhere - live `(0,-10)`, raw `0xD2`,
    already reachable through R24's `JUNCTION_VERTICAL_CONTINUATION`. For `r2c7 r3c5 r3c6 r0c5 r0c6
    r0c8 r1c4 r1c6 r3c8 r4c8` the card says only "the user pointed at this in a screenshot". The live
    map proves which topologies EXIST; it cannot prove which cell was MEANT. Writing 11 rules would be
    inventing all 11 mappings.
  - **THE LIVE MAP ALSO CANNOT SUPPLY THEM ALL, WHICH IS INDEPENDENT OF THE MISSING COORDINATES.** It
    contains **exactly ONE cross topology** - raw `0x5A` at `(-18,-5)`, all four cardinals Raised with all
    four diagonals open - while the card requires **three** distinct junction components `r0c8 r3c8
    r4c8` to be REACHABLE. One topology can select one Sprite, so at most one of the three could be
    located from the live map even with a perfect coordinate. Similarly no cell on the map has the shape
    my "flanked" search looked for (0 flanking candidates), so there is no obvious host for the four
    "upper" components `r1c4 r0c5 r0c6 r1c6`.
  - **The 15 composition-shaped live topologies, measured, so the next card starts from facts:** 15
    handover/cross sites, raw `0x08 0x0B 0x10 0x16 0x2F 0x4B 0x56 0x5A 0x68 0x6A 0x97 0xD0 0xD2 0xE9 0xF4`.
    `0xD2 (0,-10)` is the locked r2c4. `0x6A (8,-10)` and `0xD0 (0,-5)` / `0x68 (8,-5)` are the R25
    candidates for r2c7 / r3c5 / r3c6 and are the natural mirror-and-drop-N families, but assigning them
    is still an inference and was NOT made.
  - **The cross, measured, because the user called it the lightning/stair shape:** `(-18,-5)` raw `0x5A`
    is where the 5-cell horizontal bar at y=-5 meets the 1-wide column at x=-18 which continues from
    y=-3 to y=-7. It currently draws **`Hills_r1c1`** as MIDDLE_SURFACE/BODY, which is plainly a body row
    in the middle of a cross. Its two flanking column cells are `(-18,-4)` raw `0xE2` and `(-18,-6)`
    raw `0x47`, both currently `Hills_r1c3`. **A cross body row is the clearest remaining defect the user
    has actually reported, and it is the one site on the map whose coordinates need no guessing.**
  - **No `STAIR_COMPOSITION_STABILITY` oracle could be written.** Its three states - baseline, extend
    down, extend up - require knowing which cell must hold steady across the extension. On the live map
    the cross is already extended BOTH ways, so there is no baseline instance of it to compare against,
    and inventing one would mean choosing the author's stair myself.
  - **Nothing was touched.** 0 production files modified, the composition-set asset unmodified, no slice
    wired, no role added, `Hills.png` byte-identical, scene byte-identical, the 4 user-owned files
    unstaged. Both new files are read-only diagnostics that build an in-memory grid and never write.- **R25 STOPPED as `BLOCKED_PM_DECISION`. NO production file was changed.** The four-component joint
  comparison RESOLVED, and in the process it proved that the natural rule for `r3c5` would over-capture.
  Reporting the resolution and the blocker; not guessing past either.
  - **THE LIVE MAP CHANGED UNDER THIS CARD, and that is the user's own doing.** Card start SHA
    `5281BA5186CA8A73C4786C48366EC180782EA3FB9F97AAAED2A6FE674F26647F`, 50 Raised, mtime 04:17.
    At the first read of this card it is SHA `6986B2C40D722A7F6BC53AD4B65C6E5D190B5BA4502BACB46EE0082526733506`,
    **53 Raised**, mtime 05:00. The user edited the map while the card was being worked - which the
    standing rule explicitly allows and expects. **The live file is the authority, so the 53-cell map is
    treated as the current baseline and was never written to, restored or reverted.**
  - **THE JOINT COMPARISON IS A COMPLETE AND CLEAN RESULT. Four components, two independent axes, and
    each axis is a small set of bits. All four core masks, in production bit order:**
    ```
    A  r2c4  0xD2  NW- N+ NE- | W- C E+ | SW- S+ SE+
    B  r2c7  0x6A  NW- N+ NE- | W+ C E- | SW+ S+ SE-
    C  r3c5  0xD0  NW- N- NE- | W- C E+ | SW- S+ SE+
    D  r3c6  0x68  NW- N- NE- | W+ C E- | SW+ S+ SE-
    ```
    - **LEFT vs RIGHT differs in exactly four bits** - `W`, `E`, `SW`, `SE` - all of them swapped.
      **A/B and C/D are strict occupancy mirrors** (measured `True`/`True` by an explicit eight-bit
      equivalence, not by comparing two numbers). This is a statement about logical occupancy only; **no
      Sprite is mirrored anywhere.**
    - **r2 vs r3 differs in exactly ONE bit: `N`.** A/C and B/D each differ by `N` alone.
    - So the four components are **two axes, not four rules**: which side the boundary is on
      (4 bits) and whether it continues north (1 bit). That is the honest structure of the grammar.
  - **A = r2c4 IS CONFIRMED AND STILL LOCAL.** Live (0,-10), raw `0xD2`, role
    JUNCTION_VERTICAL_CONTINUATION, slot LEFT_TERMINAL, sprite `Hills_r2c4`, reachable. Family `0xD2 0xD3
    0xF2 0xF3`, exactly 1 live cell. **No regression: R24's result is intact.**
  - **B = r2c7 HAS A UNIQUE LOCAL CANDIDATE.** Mirror of A = `0x6A`, family `0x6A 0x6E 0xEA 0xEE`,
    and it matches **exactly ONE** live cell: **(8,-10)**, raw `0x6A`, currently `Hills_r1c2` as
    MIDDLE_SURFACE/RIGHT_TERMINAL. **There is no over-capture and no ambiguity for B.**
  - **THE BLOCKER IS C AND D, AND IT IS MEASURED, NOT SUSPECTED.** Family C (`0xD0`) matches **5** live
    cells - `(-5,-10) (0,-5) (13,-5) (13,-1) (16,-1)` - and **all five currently draw the author's r0c0**.
    Family D (`0x68`/`0xE8`) matches **5** - `(-3,-10) (8,-5) (14,-5) (14,-1) (17,-1)` - and **all five
    currently draw the author's r0c2**.
    The five `0xD0` cells are **genuinely different shapes** - four are the plain top-left corner of a
    small 2 or 3 deep block, one (`(0,-5)`) is the big ring's own top-left corner - and they share a
    **byte-identical 8-neighbour raw `0xD0`**. Their 5x5 printouts are in the audit and the difference is
    plainly outside the 3x3. **So a `0xD0 -> r3c5` rule would change all five, four of which are ordinary
    rectangle corners whose r0c0 is R11-locked and which card section 9 explicitly protects.** That is
    card **section 21 STOP condition 2** (identical full RAW 3x3, two different required Sprites) and
    also **section 9 over-capture**, and it is not avoidable by narrowing the predicate: all five have
    the same eight bits.
  - **WHAT IS MISSING IS NOT A RULE, IT IS A COORDINATE.** The card fixes four sprites and says the user
    pointed at four positions, but **gives no coordinate for B, C or D**, and no screenshot reached this
    session. The live map proves which topologies EXIST; it cannot prove which cell was MEANT. With no
    coordinate I would have to choose between "break four R11-locked corners" and "silently do nothing",
    and both are forbidden. **PM decision required: the coordinates of B, C and D.**
  - **NO SEMANTIC ROLE WAS NAMED, deliberately.** The card forbids deriving a name from a sprite's sheet
    row or column, and refuses to pre-assume `r2 = upper` / `r3 = lower`. The two axes are described by
    their BITS (`N` occupancy, and the W/E/SW/SE side), which is what the evidence supports.
  - **REACHABILITY AUDIT OF ALL 21 ALREADY-LOCKED COMPONENTS.** 13 are emitted by at least one live cell.
    **8 are emitted by NO live cell** - `r3c3 r0c3 r3c0 r1c1 r3c4 r3c7 r0c4 r0c7` - all of which are
    reachable in R18/R19/R20/R11 fixtures but simply do not occur on the 53-cell live map. **This is NOT a
    reachability defect and must not be reported as one**: e.g. `r3c4`/`r3c7` need an L-shaped one-wide
    corner and `r0c4`/`r0c7` need a top corner, none of which the current live shape contains. A
    live-map scan cannot establish reachability for fixtures the live map does not contain.
    `r2c7` and `r3c6` additionally have **no accessor and no wiring at all** yet (slices exist as author
    assets, guids `2950afe7…` and `ff98ff8e…`), which is expected given the STOP.
  - **Other facts recorded:** `UNCHANGED_RAW_3X3_SPRITE_MUTATIONS: 0` on the live map - no live raw draws
    two Sprites; `OUTSIDE_MASK_PROJECTION: 0`; `ONE live raw splits two sprites: 0`; the 3x3 rectangle's
    top-left raw is **not** `0xD0`, so the R11 rectangle oracle is not itself the colliding case - the
    collision is between the ring corner and four separate small-block corners.
  - **Two harness defects fixed on the way, both mine:** the R25 audit's tuple literal carried an unused
    third element that shadowed the enclosing loop variable, and its `IsMirror` was rewritten as an
    explicit eight-bit equivalence so the mirror claim cannot be an artefact of two numbers matching.
  - **FirstIsland READ ONLY throughout** - no paint, no undo, no scene, no `AssetDatabase` write; both
    new files only create an in-memory grid from the asset.- **R24: the author's r2c4 junction is restored from LOCAL topology, and it is now a LOCKED_USER_VISUAL_ORACLE.
  The live cell the user's screenshot marked draws `Hills_r2c4` instead of `Hills_r1c0`, from a six-bit
  predicate over its own 3x3.**
  - **TARGET_RAW_3x3 = 0xD2** (production bit order: NW 0x01, N 0x02, NE 0x04, W 0x08, E 0x10, SW 0x20,
    S 0x40, SE 0x80) = **N, E, S, SE raised; NW, NE, W, SW open**.
    **TARGET_CANONICAL = 0xD2.** Live cell **(0,-10)**, 7-deep ring wall, on the user's map.
    R21 recorded this same junction at raw 0xD2 independently, and the live asset produced it
    independently this round, so the number was confirmed by two routes rather than chosen from a table.
  - **BEFORE: role MIDDLE_SURFACE, slot LEFT_TERMINAL, sprite Hills_r1c0.** The junction cell has N and S
    both Raised, so the R23B cardinal ladder classified it as an interior body row and emitted the plain
    body. That is the whole defect, and it is why a length-free grammar could not have found it: the
    ladder was right about the topology and wrong about the art, because the art needed the handover fact.
  - **AFTER: role JUNCTION_VERTICAL_CONTINUATION, sprite Hills_r2c4**, tagged `FRONT_CLIFF` because r2c4
    is a row r2 slice. **R2C4_STATUS: LOCKED_USER_VISUAL_ORACLE.** No longer HISTORICAL, no longer
    UNRESOLVED. Exactly **1** live cell draws r2c4, and the other live r1c0 cell (16,-2) still draws r1c0.
  - **USES_ONLY_LOCAL_3X3: YES. RUN_DEPTH_SEMANTICS: NO. X2_Y2_LOOKAHEAD: NO.** The rule is six bits:
    W open, N raised, S raised, E raised, SE raised, NE open. No depth, no offset, no walk, no
    width/height, no shape name, no x+/-2, no y+/-2. The old R21 predicate needed (x,y-2), (x+2,y) and
    (x+2,y-1) and was **not** restored.
  - **THE DISCRIMINATOR IS ONE BIT, MEASURED. Sixteen raw masks draw the author's r1c0 body. Four are the
    junction - 0xD2, 0xD3, 0xF2, 0xF3, which are only the NW and SW variants of each other - and all
    four now emit r2c4. The other twelve stay r1c0 and split cleanly:**
    `0xD6 0xD7 0xF6 0xF7` **NE raised** = a wide plateau edge, the body row is right;
    `0x52 0x53 0x56 0x57 0x72 0x73 0x76 0x77` **SE open** = the top of the boundary, nothing to hand over to.
    **So "NE open together with SE raised" IS the handover, and it is a single neighbour one cell away.**
    That is exactly why the rule can be 3x3-only, and it is also why the vertical BODY is not disturbed:
    only the handover leaves r1c0.
    NW and SW are deliberately left unconstrained, so the four variants are one topology and one rule
    names it. Pinning them would assert the junction only exists where the boundary happens to be one
    cell wide, which is a shape assumption about the surroundings.
  - **R18 41/0 · R19 33/0 · R20 59/0 · R11 172/0/1 SKIP · R15 PASS (R18 live Paint/Erase/Undo/Redo) ·
    R16C 13/0 · R23B 108/0 · R24 audit 21/0.** All frozen oracles run unchanged in their own processes.
    Plain vertical bodies are re-asserted at widths 2, 3 and 5 and keep the exact r0c0/r1c0/r2c0 stack;
    width 2 is included deliberately because it has no interior column and is the case most likely to be
    over-captured.
  - **LENGTH_1_TO_100: PASS.** The junction neighbourhood is read OFF THE LIVE MAP and rebuilt with every
    arm 1, 2, 3, 4, 5, 8, 16, 32, 64, 100 cells long. The raw 3x3 stays 0xD2 at all ten reaches and the
    sprite stays r2c4 as JUNCTION_VERTICAL_CONTINUATION at all ten. It never reverts to r1c0.
  - **A BIT-ORDER DEFECT WAS FOUND IN MY OWN R23B HARNESS AND FIXED, AND IT HAD BEEN MASKING TWO BAD
    FIXTURES.** `ILW004SR23BLocalCore` numbered its eight neighbours N-first while production numbers them
    NW-first, so a production mask such as 0xD2 decoded as a completely different topology. Two
    consequences, both real and both now fixed rather than papered over:
      - the junction sweep tested NE,S,SW,NW and the cell *correctly* resolved to the author's r0c7 top
        corner, which looked like the rule failing when the rule had never been tested;
      - `EXTEND_ONE_ARM_STABLE` was **passing for the wrong reason**: `0x01` meant NW, so the arm ran
        north-west and the cell answered SECOND_FRONT_CLIFF / r3c3, matching the assertion by accident.
        With the mask stated in production order it is a genuine one-wide column bottom and answers
        r2c3, which is what R18 locks at any length.
    **The lesson recorded: a fixture can pass by testing the wrong topology.** Every raw mask in that
    harness is now decoded in production's order, the identity literals were converted to named shifts,
    and the junction fixtures are discovered from data by the predicate rather than hard-coded by
    coordinate.
  - **Two R24 audit assertions were also re-baselined, because they were asserting the PRE-FIX world.**
    One required every selected mask to "draw r1c0 today" and the other located the junction by searching
    for the r1c0 sprite - so both reported the successful fix as a failure. **Asserting a pre-fix sprite
    snapshot is how a harness ends up defending a bug.** Both now locate and judge the junction by its
    topology and role.
  - **A silent arm-truncation bug was caught by the read-back assertion in the R24 harness**: sizing a
    probe grid from the target cell instead of the origin let south arms run off the bottom, clamping
    18240 arm cells away while the mask read-back still looked perfect. Fixed by insetting the target on
    its west AND south extents; 0 truncated cells over all 2048 probes.
  - **r3c5 is untouched, as the card requires.** `CORNER_NO_UPPER_CONTINUATION` stays unreachable, r3c5
    stays wired and unwired-from-any-rule, and nothing was inferred from r2c4's confirmation.
  - **FirstIsland READ ONLY, no paint session started.** SHA256
    `5281BA5186CA8A73C4786C48366EC180782EA3FB9F97AAAED2A6FE674F26647F`, 12248b, Raised 50 - identical at
    card start and card end. `FirstIsland_Prototype.unity` byte-identical. `Hills.png` byte-identical, and
    so is every Sprite rect, pivot and PPU: the composition-set **asset** was not modified at all, only
    the C# accessor that names the role.- **R23B local topology semantic core (`a12a8e9` -> this card). Author semantics now derive from ONE cell's
  own 3x3. The run walk, the runDepth ladder, MaxFrontWalk and the R21 junction test are GONE from the
  semantic path; Layer A / Layer B / Layer C are separated and the role is now a pure function of the raw
  3x3.**
  - **The straight ladder is four cases over N and S, and that is the whole grammar:**
    `N open, S solid -> TOP_SURFACE (r0)` · `N solid, S open -> FRONT_CLIFF (r2)` ·
    `N solid, S solid -> MIDDLE_SURFACE (r1)` · `N open, S open -> SECOND_FRONT_CLIFF (r3)`.
    "One deep", "two deep", "three deep" and "four deep" are all GONE as concepts: a cell is a top row,
    a front row, an interior row, or its own top and front at once. **No length appears anywhere.**
  - **Slot is Layer A too, and it is the card's own worked example, asserted as an identity over all 8
    reaches:** E only -> LEFT_TERMINAL, W+E -> BODY, W only -> RIGHT_TERMINAL, both open -> NARROW.
    A one-cell hole is no longer admitted as the open side of a slot, so a void cannot put the author's
    narrow c3 stack on the inside of a ring.
  - **`LEGACY_RUN_OVERRIDE: 9 -> 0`, the card's headline target.** 256 raw masks x 8 reaches (1,2,3,4,8,16,
    64,100) = 2048 probes, and 0 emit more than one Sprite. `TRUE_LOCAL_AMBIGUITY: 0`.
    **R23A2 re-run under the new core reports the 9 down to 4 residual canonical collisions, and all 4
    are `LEGACY_RUN_OVERRIDE` by that harness's own rule - but each one is a genuine RAW distinction
    (0x14 vs 0x34, 0x05 vs 0x85, 0x41 vs 0x43, 0x50 vs 0x58), i.e. the r0c0-vs-r0c4 and r2c0-vs-r3c4 pairs
    that R19/R20 locked as corners. They are NOT the same raw giving two sprites; R23A2's classifier still
    buckets any canonical with >1 sprite as a collision, and the corner roles make that unavoidable while
    they exist.** The card's own metric (`LEGACY_RUN_OVERRIDE_ZERO`, one Sprite per raw) is 0.
  - **The witness grid defect R23A2 fixed is now load-bearing rather than cosmetic: it caught a real bug
    in my R23B probe.** Sizing a probe grid relative to the target cell silently truncated south and
    south-west arms, producing 224 read-back mismatches; with the grid sized from the origin the same
    assertion reports 0/2048. Every probe reads its mask back OUT of the grid, so a probe cannot mislabel
    its own topology.
  - **A local replacement for `MaxFrontWalk` was written, MEASURED, and WITHDRAWN.** The R16C rule let a
    front wall step down to join a thick neighbour's row, which is inherently a two-or-more-cell read. The
    local one-cell version broke the frozen R19 oracle in 8 of 33 fixtures by turning the cell beside a
    front corner into an r2 terminal - the corner itself never moved, the NEIGHBOUR did, which is exactly
    the kind of silent coupling the walk used to hide. **So `MaxFrontWalk` is removed with no replacement
    and the R16C behaviour is NOT reproduced.** Recorded as a real loss, not papered over.
  - **Deep wide masses render r0/r1/r1/r2 where they used to render r0/r1/r2/r3. This is forced, not
    chosen.** The bottom row of a 3-deep mass and the bottom row of a 4-deep mass have byte-identical raw
    3x3 (N, NE, NW, E, W raised; S, SE, SW open) - the difference is at (x, y+2), outside 3x3 by
    construction. R11 locks the 3-deep case to r2 and has never failed, so r2 is the only answer a purely
    local Layer A can give. No oracle covers the 4-deep wide case, so **no oracle breaks; what changes is
    the VISUAL of deep wide masses, and that is PM / Scene View judgement, reported UNITY_VISUAL_PENDING
    rather than asserted.**
  - **Frozen oracles, all run unchanged in their own processes: R18 41 PASS / 0 FAIL, R19 33 / 0,
    R20 59 / 0, R11 172 PASS / 0 FAIL / 1 SKIP (2_DEEP_WIDE_LEGACY_DISPLACED_ROW still
    DEFERRED_LEGACY_EXPECTATION, not re-pinned).** R15 erase is covered by the R18 live
    Paint/Erase/Undo/Redo block, which passes. R23B itself 86 PASS / 0 FAIL.
  - **The ladder is also re-asserted at lengths R18 does not carry: horizontal and vertical 1,2,3,4,5,8,16,
    32,64,100 all PASS with the exact expected sprite at every cell, and the vertical expectation
    contains no length** - `r0c3` + `(n-2) x r1c3` + `r2c3` is the same three rules for 2 through 100.
  - **`DrawsFrontCliffBelow` is now unreachable, so the outside-mask projection is a MEASURED 0** on every
    fixture and on the live 50-cell map, not an assumption: a role below FRONT_CLIFF is only returned for a
    cell whose SOUTH is solid, and such a cell cannot be south exposed. The code path is kept so the
    invariant stays checkable.
  - **R21_SUPERSEDED = YES.** `IsAuthorCornerJunction` required (x, y-2), (x+2, y) and (x+2, y-1), all
    provably outside 3x3, and the card forbids exactly those reads. The predicate is deleted; the two R21
    roles and their slices `Hills_r3c5` / `Hills_r2c4` stay wired in the enum, the composition set and the
    asset so nothing is silently missing, but **no rule produces them.** The R21 harness now correctly
    FAILS its junction lock (0/5 heights) - that is the superseded oracle reporting honestly, and the
    harness itself was not modified. **The junction topology is UNRESOLVED; no new junction Sprite was
    guessed.**
  - **Live FirstIsland, read only, no paint session started:** 50 Raised cells, 50 visual tiles, all 50
    author Hills slices, 0 outside the mask, 0 undrawn, 0 renderer diagnostics. SHA256
    `5281BA5186CA8A73C4786C48366EC180782EA3FB9F97AAAED2A6FE674F26647F`, 12248b, Raised 50 - unchanged
    before and after every harness in this card.
  - **Remaining runDepth usage, itemised as the card requires.** `MeasureRunDepth` and `MeasureRunBottom`
    are called from exactly ONE place, `RaisedTopologyState.Resolve`, and only to populate
    `RunDepth` / `OffsetFromRunBottom`. Both are asserted to take part in NO role, NO slot and NO sprite:
    a reflective check proves `RoleForLocal`, `SlotFor` and all four corner predicates take no integer
    parameter other than their own coordinates, so a length cannot even be passed to them.
    `OffsetFromRunBottom` is therefore **reported only**; Layer C may read it for repetition, and no
    Layer B code touches it.- **R23A topology matrix — TESTS ONLY. The refactor is BLOCKED, and the blocker is architectural.**
  Card section 22 mandates the matrix before any production edit, so this round produced the matrix and
  **no production change at all**.
  - **Measured: 512 raw probes reduce to 47 canonical states, and 9 of those canonical states CURRENTLY
    emit MORE THAN ONE author Sprite.** That is card STOP condition 1 verbatim. The contradictions are:
    - `East, South` → `r0c4` **or** `r0c0`
    - `North, East` → `r3c4` **or** `r2c0`
    - `North, West` → `r3c7` **or** `r2c2`
    - `West, South` → `r0c7` **or** `r0c2`
    - `North, West, East` → `r2c0` **or** `r2c1` **or** `r2c2`
    - `West, East` → `r3c0` **or** `r3c1` **or** `r3c2`
    - `North, NorthEast, West, East` → `r2c0 / r2c1 / r2c2`
    - `NorthWest, North, West, East` → `r2c0 / r2c1 / r2c2`
    - `NorthWest, North, NorthEast, West, East` → `r2c0 / r2c1 / r2c2`
  - **The architectural reason, which is the real content of this round: SLOT IS A RUN PROPERTY, NOT A
    CELL PROPERTY.** The `r2c0 / r2c1 / r2c2` and `r3c0 / r3c1 / r3c2` triples are
    LEFT_TERMINAL / BODY / RIGHT_TERMINAL. Whether a cell is the END of a horizontal front is not a fact
    about that cell's own 3x3 at all — it is a fact about **where the run stops**, which is only knowable
    by walking outward until it does. Card section 5 requires Layer A to depend only on "the current
    cell's canonical/local adjacency". **That requirement is unsatisfiable for slot.** A pure 3x3
    function cannot express terminal-versus-body, so Layer A as specified cannot produce Layer B.
  - **Seven beyond-3x3 semantic reads are present in production today**, printed from source by the
    harness: `MeasureRunDepth(grid,x,y)`, `MeasureRunBottom(grid,x,y)`, `MaxFrontWalk = 2`,
    `IsRaised(x, y-2)`, `IsRaised(x+2, y)`, `IsRaised(x+2, y-1)`, and
    `MeasureRunDepth(grid,x,y) >= 3` on a **neighbour**. Section 21 forbids unapproved x+2 / y-2
    lookahead and section 27.3 requires an immediate STOP when more than 3x3 is needed.
  - **R21 is therefore REOPENED and NOT superseded here:** its junction role is built on an `x+2` probe,
    so it cannot survive a strict 3x3-only semantic layer. `r3c5` / `r2c4` are retained as historical
    evidence only, exactly as section 14 requires, and no attempt was made to preserve them by
    distorting the new model because there is no new model yet.
  - **Honest defect in this round's harness:** the ASCII witness grid printout uses a confused row index
    and does not correspond exactly to the raw mask labels beside it. The COUNTS, the canonical-state
    list and the multi-sprite facts are unaffected and are what this conclusion rests on; the witness
    grids should not be read as exact reproductions until that printout is fixed.- **R22 grammar stability audit — DIAGNOSTIC ONLY, no production change. The headline result is
  NEGATIVE and it corrects the card's premise.**
  - **There is currently NO observed remote sprite mutation.** Sweeping lengths 1,2,3,4,5,6,7,8,16,32,
    64,100 on a plain vertical line, a plain horizontal line, both arms of a correct L, and the
    junction arm, and diffing every previously existing cell at every step, produced **411 records
    where a cell's 3x3 mask was UNCHANGED yet its resolver output record changed — and in 411 of 411
    the ROLE, the SLOT, the SPRITE and the VISUAL COORDINATE were all identical.** Not one Sprite moved
    on a cell that gained no neighbour.
  - **What is real, and is the actual finding: `MeasureRunDepth` rewrites `runDepth` on EVERY cell of a
    column whenever that column gets longer.** It is an unbounded whole-column walk in both directions,
    and `runBottom` likewise. So one Paint at the top of a 100-tall column mutates the resolver input
    of 63 of the 99 existing cells, none of which changed its local topology and none of which changed
    its picture. Growth is exactly linear: 1,1,2,3,4,5,6,7,15,31,63 mask-unchanged mutations at
    2,3,4,5,6,7,8,16,32,64,100.
  - **So `runDepth` is a volatile, region-sized input that is currently INERT, not currently harmful.**
    `RoleFor`'s depth branches happen to collapse to the same answer for a 1-wide column at every
    depth, which is why nothing visible moved. It is nonetheless the only mechanism by which a
    threshold crossing anywhere in `RoleFor` could flip a role on a cell whose own 3x3 never changed.
    That is a latent coupling, and it is the thing to remove, not a live bug.
  - **`offsetFromRunBottom` is clean.** It is `y - runBottom`, so extending a column upward does NOT move
    it for any existing cell. Confirmed by measurement: the horizontal arm sweep shows **0**
    mask-unchanged mutations at every length, and the vertical arm's mutations are entirely `runDepth`.
  - **No hidden remote coupling across a gap.** Extending a single edge far away in all eight
    directions N, S, E, W, NE, NW, SE, SW at distances 1,2,3,4,8,16,32,64 left a fixed corner target
    `(4,4)` **completely stable in all 8 x 8 = 64 probes**. So neither `FrontContinuesInMask`
    (`MaxFrontWalk = 2`) nor `IsAuthorCornerJunction`'s two-step-east probe leaks across empty ground.
  - **TWO OF MY OWN MEASUREMENT BUGS, both found and fixed before any of the above was believed.** The
    first diff read the "before" cell from the *after* dictionary, so every comparison was trivially
    equal and it reported 0 changes of any kind; a raw snapshot dump disproved it. The second was a
    ragged-row crash. Both are recorded because the first one would have produced a confident and
    completely false "the system is stable" report.- **R21 author corner continuation grammar SHIPPED. `Hills_r3c5` (no continuation) and `Hills_r2c4`
  (continuation).** The junction is the cell where a **vertical Raised boundary on the west meets a
  Raised platform running east**, and the vertical boundary ends immediately below it. Measured on the
  real resolver path at `(4,5)`:
  - **STATE A** raw `0xD0` canonical `East, South, SouthEast`, N **open** →
    `CORNER_NO_UPPER_CONTINUATION` → **`Hills_r3c5`**
  - **STATE B** raw `0xD2` canonical `North, East, South, SouthEast`, N **Raised** →
    `CORNER_WITH_UPPER_CONTINUATION` → **`Hills_r2c4`**
  - **`DIFFERING_NEIGHBORS = N: open -> Raised`.** North occupancy is the only A/B discriminator.
    `runDepth` is deliberately NOT used to decide A/B; it reads 2 in STATE A and 3 in STATE B, and the
    old sprite simply slid with it (`r0c0, r1c0, r2c0` as the wall grew), which is the reported defect:
    a two-deep west column drew `r0c0` directly over `r2c0` and **skipped the author's r1 body row
    entirely**.
  - **The height ladder, off-by-one fixed, is now 5/5: H1 `r3c5`, H2..H5 `r2c4`,** with the junction
    sprite never sliding back to `r0c0`/`r1c0`/`r2c0`. Repetition 10x: `r3c5 | r2c4 r2c4 r2c4 r2c4`,
    9/9 identical. Added height is carried by the already-locked vertical straight/body grammar only.
  - **Rectangles are NOT captured: 5/5 clean** (2x2, 3x2, 5x2, 3x3, 5x3 all resolve ZERO junction
    roles). This required two guards that were each added after measurement, not guessed:
    - the platform must **hold depth beside the junction** (`SE` Raised). R20's already-locked top
      corner is otherwise identical here: `[XXX / X..]` gives raw `0x50` with SE open while this
      junction's `[XXX / XX.]` gives raw `0xD0` with SE Raised. Without this guard R20 regressed 7
      assertions and its locked `Hills_r0c4` was stolen.
    - the platform must be **at least three cells wide and must step away** two columns east, which is
      what excludes a 2x2 block and any plateau that keeps the junction's depth all the way across.
  - **`Hills_r3c5` (rect 80,80) is now wired into the composition set** and covered by `IsComplete()`.
    Both junction components are tagged with their OWN author row, r3 and r2, so a consumer can tell
    which locked piece was chosen.
  - **Regressions, every frozen suite unchanged:** R18 41/0, R19 33/0, **R20 59/0**, R20B P fixture
    27/0, R11 172/0/1 SKIP with 2 legacy deferred, R16B 17/0/25 deferred, R16C 13/0. R20B's projection
    fix is untouched: outside-mask visual tiles remain 0 and no south displaced cliff was reintroduced.- **R21 corner continuation grammar: the RULE is now fully derived; only the slice identity is open.**
  PM clarified the target: the junction where a **vertical Raised boundary meets a Raised platform on
  its RIGHT**. In the locked P that cell is `(4,5)`. Measured on the real resolver path:
  - **STATE A** (no Raised above) — `(4,5)` raw `0xD0` canonical `East, South, SouthEast`,
    `runDepth 2`, role `TOP_SURFACE`, slot `LEFT_TERMINAL`, **N open**, currently `Hills_r0c0`.
  - **STATE B** (Raised continuation above) — `(4,5)` raw `0xD2` canonical
    `North, East, South, SouthEast`, `runDepth 3`, role `MIDDLE_SURFACE`, slot `LEFT_TERMINAL`,
    **N Raised**, currently `Hills_r1c0`.
  - **`DIFFERING_NEIGHBORS = N: open -> Raised`.** The entire A/B distinction is **one local bit**:
    whether the junction's own NORTH neighbour is Raised. Every other direction is identical
    (W open, E Raised, SE Raised, S Raised, NE/SW/NW open). The rule is therefore derivable purely
    from that cell's 3x3 occupancy.
  - **runDepth CANNOT be the discriminator and must not be.** It reads 2 in STATE A and 3 in STATE B,
    and today's sprite simply slides with it: over heights 1..5 the junction emits
    `Hills_r0c0, Hills_r0c0, Hills_r1c0, Hills_r2c0, Hills_r2c0`. That height-dependent sliding is
    exactly what section 10 forbids, and it is also the visible defect: a two-deep west column draws
    `r0c0` directly over `r2c0` and **skips the author's r1 body row entirely**.
  - **Still open, and reserved for PM by sections 6 and 18:** which author slice is the STATE A base
    component and which is the STATE B continuation component. Both cells currently resolve to plain
    already-proven `LEFT_TERMINAL` pieces (`r0c0` / `r1c0`), which PM has reported as visually wrong,
    and `Hills_r2c4` (rect 64,96) and `Hills_r3c5` (rect 80,80) have both been named as candidates.
    Section 18 forbids choosing between reasonable candidates, and nothing in the sheet layout or the
    locked grammar makes either assignment unique, so no slice was chosen and no role was written. Two measured facts
  contradict the card's premises and must be corrected before any rule is written.
  - **The author Hills sheet has NO empty slots. Measured: 99 of 99 slices NON_EMPTY, 0
    TRANSPARENT_EMPTY**, across rows r0..r8 x c0..c10, each cell checked by copying through
    `Graphics.Blit` + `ReadPixels` and testing for any pixel above alpha 8. Every one of the 99 cells has
    content. The card's instruction to classify transparent-empty slots therefore has no population, and
    "a transparent cell is an author empty slot, not a missing asset" does not apply to this sheet.
    (Earlier notes recorded `r2c9`/`r2c10` as ~2 px silhouette slivers; those still contain pixels, so
    they are non-empty rather than empty slots.)
  - **Extending the P's vertical arm upward by one cell changed NOTHING in the measured output.** Every
    one of the five cells reported `DIFFERING_NEIGHBORS = NONE` between HEIGHT 1 and HEIGHT 2, and no
    cell changed its sprite. So on the arm this card named, there is no observable STATE A / STATE B
    transition at +1, and the specific cell whose sprite PM reports as visually wrong does not change
    when a cell is added above it.
  - **Honest incompleteness in my own measurement:** the HEIGHT ladder in the committed harness has an
    off-by-one (`for (int extra = 2; extra < height; extra++)`), so HEIGHT 1 and HEIGHT 2 produced the
    identical 5-cell fixture and HEIGHT 3 is the first genuinely taller one. The ladder is therefore NOT
    yet a valid A/B comparison and must be re-measured before any conclusion is drawn. What the harness
    did show, measured, is the west arm's front cell `(4,4)` over H1..H5:
    `r2c0 r2c0 r2c0 r3c0 r3c0` — it moves from the r2 row to the r3 row at the point the column reaches
    four deep, which is the R16C `runDepth >= 4` rule, NOT an "upper continuation" rule. That is
    currently indistinguishable from what the card describes, which is why no rule was written.
  - **`Hills_r3c5` exists, is NON_EMPTY (rect 80,80) and is NOT referenced by the production
    composition set.** `Hills_r2c4` (rect 64,96) and `Hills_r2c5` (rect 80,96) are wired in but emitted
    by no rule. No state was assigned any of them, because section 18 forbids choosing between
    reasonable candidates and the measurement above does not yet isolate a unique STATE A / STATE B
    pair. Committed evidence harness: `ILW004SR21ContinuationInventory` (read-only, asserts nothing).- **P-junction projection FIXED. Out-of-mask visual tiles eliminated: 128 of 512 neighbourhoods -> 0.**
  The locked fixture is now correct. BEFORE `[XXX / X..]` = 4 logical / 4 visual / 0 outside.
  AFTER `[XXX / XX.]` = **5 logical / 5 visual / 0 outside**, and `(4,3)` / `(5,3)` no longer exist.
  Every visual tile now sits on its own logical cell and traces to a logical cell plus an author role
  plus an author Sprite.
  - **The fix is one branch.** `RaisedTopologyState.RoleFor`, `if (runDepth == 2)`: the bottom cell of a
    two-deep, two-or-more-wide column now returns `FRONT_CLIFF` when its south is exposed, so the
    author front wall sits INSIDE its own mask on the proven row r2. It previously returned
    `MIDDLE_SURFACE`, which is below `FRONT_CLIFF`, and that is the only condition under which
    `DrawsFrontCliffBelow` can fire — which is in turn the only way the projection ever places a tile
    at a coordinate other than its own logical cell. **Measured over all 512 neighbourhoods: displaced
    tiles 128 -> 0, and 128 of the original 128 came from this one branch.**
  - **P AFTER composition, per cell, read from the plan:**
    `(4,4)` raw `0x16` canon `North, NorthEast, East` role `FRONT_CLIFF` slot `LEFT_TERMINAL` ->
    **`Hills_r2c0`** · `(5,4)` raw `0x0F` canon `NorthWest, North, West` role `FRONT_CLIFF` slot
    `RIGHT_TERMINAL` -> `Hills_r2c2` · `(4,5)` raw `0xD0` role `TOP_SURFACE` -> `Hills_r0c0` ·
    `(5,5)` raw `0x78` role `TOP_SURFACE` -> `Hills_r0c1` · `(6,5)` raw `0x28` role
    `SECOND_FRONT_CLIFF` slot `RIGHT_TERMINAL` -> `Hills_r3c2`, **unchanged from BEFORE**.
    The west end of that front is a genuine `LEFT_TERMINAL` by 3x3 adjacency, which is what makes it
    the author's own `r2c0`, the left boundary plus front edge piece.
  - **`Hills_r2c4` and `Hills_r2c5` are now wired into the composition set** (rect (64,96) and (80,96))
    and are covered by `IsComplete()`, so they can no longer be silently missing. **Neither is emitted
    by any rule, and this is deliberate.** Two facts make it underivable rather than merely unfinished:
    **(a)** the P fixture's west front cell `(4,4)` has raw mask `0x16`, *byte identical* to a 3x2
    rectangle's west front cell, so no rule reading only a 3x3 neighbourhood can give them different art;
    **(b)** the P's front is **two separate runs** — row 4 `[(4,4),(5,4)]` and row 5 `[(6,5)]` — not one
    three-cell run, so PM's three row-r2 anchors cannot all be adjacent cells. Guessing a placement
    would be the hard-coding the card forbids, so the slices are present and reachable but unattached.
  - **Two legacy oracles are DEFERRED_LEGACY_EXPECTATION, not re-pinned:** R11's `2_DEEP_WIDE` and
    R16B's `ORACLE_3x2`. Both pinned the displaced row this card removed. Because of fact (a) above,
    rectangles cannot be left unchanged by any local rule, so their new composition is a measured
    consequence and is left for PM and the user to judge in the Scene View rather than blessed here.
- **R20 author top corner grammar — `Hills_r0c4` and `Hills_r0c7` shipped.** PM locked two more author
  cells against the source after a cell-by-cell comparison in Unity: **r0c4** is the LEFT TOP corner, a
  vertical left boundary that reaches the TOP and turns east into the horizontal top structure, and
  **r0c7** is the RIGHT TOP corner, the top structure reaching its right end and turning south. Both
  are real author slices of the same 16×16 grid: r0c4 is rect (64,128) and r0c7 is rect (112,128), the
  same pivot 8,8 and PPU 16 as the already-locked r0c1 at (16,128). Nothing mirrored, rotated,
  stretched or resampled. "Left top" and "right top" are the project's own Grammar Map position names
  and are used verbatim; they are not converted into any other corner naming scheme.
  - **No new BODY slice was introduced and none was needed.** The BODY between two top corners is the
    author's existing `r3c1`, because a one-cell-deep top structure is already the R18 r3 band body.
    The card's requirement that a cell adjacent to a corner must stay `r3c1` therefore needed no art
    change at all, only the removal of something that was overwriting it (below).
  - **The rule is a 3×3 window with no shape name, component id or coordinate test.** LEFT fires when
    north is open, west is open, east is Raised and **the cell south is genuinely one cell wide**; RIGHT
    is the mirror-image condition set.
  - **The "south neighbour is one wide" guard is load bearing and was measured.** Without it the
    north-west cell of *every* rectangle has the same signature, because a plateau's top cell is also
    its south-west corner. It was added specifically to keep R11's rectangles, R18's band grammar and
    R19's front corners intact, and all three suites confirm it unchanged: **R18 41/0**, **R19 33/0**,
    **R11 178/0/1 SKIP**.
  - **A real defect was found and fixed in the R14 run walk, and it is the interesting finding of this
    round.** `AuthorHillsLocalResolver.CliffSlotFor` decides whether a cell ends a front run by asking
    which neighbours emit a cliff on the same visual row. A top corner has a **leg below it**, so its
    own south is not exposed, it emits no cliff, and the walk stopped there. The body cell immediately
    inside a corner was consequently treated as a run END and drawn as the author's `r3c0` or `r3c2`
    terminal instead of `r3c1` — exactly the unconfirmed corner-adjacent piece the card forbids. The
    walk now continues **through** any of the four corner roles, on the stated grounds that a corner is
    a whole-cell composition occupying its own cell in the same visual row. The corner's own sprite
    still comes from its ROLE and can never be overwritten by the walk. This is measured to be a no-op
    for R19's front corners, which already emitted a cliff on that row.
  - **0 body cells between the two top corners is PROVED NOT CONSTRUCTIBLE** and was not forced. Two
    adjacent top corners would each need a Raised one-wide south neighbour, which forces the
    south-east and south-west cells to be simultaneously Raised and open. Fixtures therefore run
    1, 2, 3 and 6 body cells, and the narrowest possible two-wide shape is asserted to yield at most
    one corner as a direct demonstration.
  - **Measured compositions** (top-left visual grid, per cell, from the committed harness): corner raw
    mask `0x50` canonical `East, South` role `LEFT_TOP_CORNER` → `r0c4`; body raw `0x18`/`0x38`/`0x98`/
    `0xB8` canonical `West, East` role `SECOND_FRONT_CLIFF` slot `BODY` → `r3c1`; right corner raw
    `0x48` canonical `West, South` role `RIGHT_TOP_CORNER` → `r0c7`. Every fixture has visual tile
    count equal to logical Raised count and **zero tiles outside the logical mask**.
  - **R18, R19, R16C, MaxFrontWalk and hole classification are untouched and remain open.**

- **R19 author left/right corner grammar — `Hills_r3c4` and `Hills_r3c7` shipped.** PM locked the
  meaning of two cells after comparing them one Sprite at a time in Unity: **r3c4** is the LEFT corner,
  a vertical left boundary turning east into the horizontal front, and **r3c7** is the RIGHT corner, the
  front turning north into a vertical right boundary. Both are real author slices of the same 16x16
  grid — r3c4 is rect (64, 80), r3c7 is rect (112, 80), both 16x16 with pivot 8,8 and PPU 16, the same
  family as r3c3 at (48, 80). Nothing was mirrored, rotated, stretched or resampled; each corner is
  reached through its own adjacency, never by mirroring the other.
  - **This CORRECTS an earlier reading recorded in the composition set.** The set used to state that
    `r3c4..r3c7` is "an alternative author 4-wide band … deliberately NOT wired in … a second proof of
    repeatability". That reading was wrong. It came from the sheet layout rather than from the art, and
    the two cells are the author's corner primitives. The old note is replaced rather than deleted.
  - **The rule is a 3x3 window with no shape name, no component id and no coordinate test**, so it
    generalises to any outline with the same adjacency. LEFT fires when south is open, west is open,
    east is Raised and **the cell north is genuinely one cell wide**; RIGHT is the mirror-image
    condition set.
  - **The "north neighbour is one wide" condition is load bearing and was measured, not guessed.**
    Without it, every plateau's south-west cell has the same three-way signature and the rule paints
    corners along the bottom of every rectangle. It was added specifically to keep the R11 rectangles
    and the R18 band grammar intact, and both suites confirm it: **R18 41 PASS / 0 FAIL** and **R11
    178 PASS / 0 FAIL / 1 SKIP**, unchanged.
  - **The corner is decided first, before the R18 depth rules and before the junction rules**, so
    neither column thickness nor run width can substitute a different piece. Verified across
    `MIN / SHORT / LONG` at every arm length, both orientations, and with both corners in one shape.
    Because both roles sit at or above `FRONT_CLIFF`, the corner draws inside its own mask and every
    corner fixture has **visual tile count equal to logical Raised count and zero tiles outside the
    logical mask**.
  - **A measured side effect worth recording:** the narrow/wide author stack mixing inside one column
    went **192 (pre-R18) → 96 (R18) → 0 (R19)** across all 512 neighbourhoods. The only neighbourhoods
    that mixed the `c3` and `c0..c2` stacks were exactly these corners, and they are now resolved as a
    single whole-cell author composition.
  - **R18's `MaxFrontWalk`, hole region classification and every other R16C/R17A finding are untouched
    and remain open.**

- **R18 basic straight grammar — the PM-locked author grammar, now implemented.** After the source
  Basic Pack and `Hills.png` were inspected cell by cell, PM locked the composition of the three most
  basic shapes. All three were wrong before, in the same way: each one drew **more visual cells than
  logical cells** by hanging a cliff one row south of its own mask.
  - **1×1 → `Hills_r3c3`, one visual cell, nothing outside the mask.** It used to be `r0c3` over a
    displaced `r2c3`, i.e. two visual cells for one logical cell.
  - **1×N → `r0c3` over `r1c3` repeated over `r2c3`, all inside the mask.** A one-wide column now
    uses the author's `c3` stack at **every** depth, so it never reaches `r3c3`; the wide block keeps
    the `r0..r3` stack R4 reconstructed 1:1.
  - **N×1 → `r3c0 | r3c1 … r3c2`, all inside the mask.** A one-cell-high band was `r0c0..r0c2` over a
    displaced `r2c0..r2c2`.
  Measured on the live pipeline: every basic straight fixture now has **visual tile count equal to
  logical Raised count and exactly zero tiles outside the logical mask**, verified ten times with
  identical results, and verified again through the real Paint / Erase / Undo / Redo path against a
  fresh full rebuild on every single step.
- **R17A diagnosis of the large O (no fix shipped).** Two independent causes, both measured:
  - **Primary — hole size.** `IsEnclosedVoid` is cell-local (four cardinal Raised neighbours), so it
    only ever recognises **1×1** holes. On the real map it recognised 0 of 9 cells in a 3×3 hole,
    0 of 20 in a 5×4 hole and 1 of 1 in the 1×1 hole, which is why the ring's inner top edge dropped
    its cliff and painted soil inside the hole on 8 tiles. `FIRST_FAILING_SIZE = 4×4`.
  - **Secondary — straight-run width ≥ 7.** R16C's `FrontContinuesInMask` walk is bounded by
    `MaxFrontWalk = 2`, so on the 7-wide bottom edge of the 7×6 ring the join reaches 2 cells inward
    from each thick end and strands a cell one row low; that row jump then splits the cliff run and
    produces spurious interior terminals. Width 6 passes, width 7 fails. This defect was **introduced
    by R16C**.
  The required model is a **connected Normal region enclosed by the Raised component**, not a
  cell-local test, and the fix layer is hole-region classification. Neither cause is an author-art gap.
- **R18 fallout on complex shapes, recorded not fixed.** R18 changed the depth of the rows that L, T,
  U, notch and branch shapes sit on, which changed their art. In a shape with a branch the front run
  now terminates on an author terminal in the **middle** of a five-wide run rather than at its ends,
  because the run walker keys off the displaced cliff R18 removed, and an L's arm rows no longer emit
  the `r2c2`/`r2c0` pair into its pocket. Corner, junction and hole grammar are **out of scope for
  R18** and PM permits these shapes to stay wrong, so this was **not** patched. The corresponding
  R16B fixtures are now `DEFERRED`: they print the actual emitted art and count as neither a pass nor
  a failure, because their expectations describe the pre-R18 composition and blessing the new output
  would assert art nobody has verified. **PM + user must confirm the complex shapes visually before
  any of those assertions are restored.**
- **R13's north-exposure invariant is restated, not deleted.** "Every north-exposed cell uses the
  author cap row `r0`" is no longer the specified behaviour, because a one-cell-deep band is `r3` and
  that row carries no north cap. The exemption is derived from the locked grammar — a one-cell-deep
  column is exactly a cell whose south neighbour is not Raised — and the invariant is still asserted
  over every other neighbourhood.

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
- **Two measured families, pinned rather than "fixed".** Originally 32 of 512 neighbourhoods put a
  displaced cliff beside a cap row on the same row, all of them cells touching only **diagonally**,
  i.e. two separate masses meeting at a corner, never a connected junction. R18 removed the displaced
  cliff from one-cell-deep rows, so the population changed and the pin moved with it: now **96**. 192
  of 512 mixed the narrow `c3` stack with the wide `c0..c2` stack inside one author column wherever a
  column's width changes with height; that population also moved, to **96**. Both counts are
  re-pinned to measured values, not guessed, and are still asserted in the committed harness so any
  further change is visible.
- **`ILW004SR16CRealJunctionDiagnostic` reads the real map, not fixtures.** It loads the live
  `FirstIsland_TerrainData`, takes a `CreateGridData` snapshot and never writes: the harness scans its
  own source, with string literals and comments stripped, for `SaveAssets`, `SaveAssetAssets`,
  `Undo.Record`, `SetDirty` and for `SetData`/`SetElevations` on the loaded asset, and SHA256, size,
  mtime and the Raised count are all re-read afterwards. For every Raised cell it reports the raw mask
  in hex and bits, the normalised mask, `RaisedTopologyState`, role, slot, the in-mask versus displaced
  cliff decision and every emitted `HillVisualTile`. Categories are **local mask only** — `CONCAVE_WRAP`,
  `JUNCTION_MEET`, `DIAGONAL_ONLY_TOUCH`, `TERMINAL_TRANSITION`, `DISPLACED_CLIFF_BESIDE_CAP` — with no
  region walk, no span and no shape name. It also segments the real map into connected components and
  renders each one zoomed from the live asset.
- **R16C measurement on the user's three real samples (18 Raised cells, 3 components).** Read straight
  out of the live asset, not approximated: a 4 cell T, a 6 cell L and an 8 cell O. Two real defects were
  reproduced and fixed, and one is not expressible.
  - **O, inner boundary — FIXED.** Cell `(3,-4)`, raw mask `0xB8`, dropped its front cliff one row south
    and painted a block of **soil inside the ring's one-cell hole**. `IsEnclosedVoid` recognised holes
    on the north, east and west sides of a cell but not the **south**, so the hole was treated as open
    sky. `SolidSouth` now includes `SouthIsEnclosedVoid`, which makes an interior boundary behave like
    the three that already did. The hole is now empty.
  - **L, concave corner — FIXED.** Cell `(3,-11)`, raw mask `0x19`, `NOTCH`. The four-deep corner column
    carries its wall **inside** its own mask while the one-deep arm beside it drops its wall a row
    lower, so the front boundary stepped down and left a gap. The author's row choice follows column
    thickness, so at a thickness discontinuity the two rules disagreed. A wall now joins its neighbour's
    row when that thick neighbour sits at the **END** of the row. The end test is essential: without it
    the same rule fires on the R14 T, whose three-deep branch column is in the **middle** of its row,
    and it destroys both inner-corner terminals. The front is now one continuous band.
  - **T, junction — NOT EXPRESSIBLE, precisely.** Cells `(-2,-11)` raw `0x90`, `(0,-11)` raw `0x28`,
    bar middle `(-1,-11)` raw `0x58`, stem `(-1,-12)` raw `0x07`. A one-wide stem hanging off a bar
    protrudes one cell further south, so its front is genuinely one row lower, and the junction needs
    the stem's **east and west faces**. The author sheet has no east or west soil face at all, so the
    two soil bands either side of the stem meet it at a hard right angle. Unchanged, and left that way
    deliberately rather than faked.
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

Proven and shipped: rectangle width ≥ 3 at height ≥ 2 · height 2 and 3 unchanged and byte-identical
to the R6/R9 oracle · **height 1 and the one-wide column re-specified by R18** to the locked author
grammar (`N×1` = `r3c0 | r3c1 … r3c2`, `1×1` = `r3c3`, `1×N` = `r0c3 / r1c3… / r2c3`), each with
visual tile count equal to logical Raised count and zero tiles outside the logical mask · narrow column
width 1 height 1–8 · south/front cliff including the author's
second front row · outside-logical-mask cliff projection · **two-wide runs** · **freeform L, T, U,
notch, staircase, zigzag, cross, blob and irregular masses** · **plateaus 4 cells and taller** ·
**rings and holes, with the hole left unfilled**.
**Height 1 and the narrow 1×2/1×4 compositions are no longer byte-identical to the pre-R18 oracle —
that change is the point of R18 and is asserted against the newly locked grammar instead.**

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
  **R18 caveat:** this rule is still the production rule, but it keys off the displaced cliff that R18
  removed from one-cell-deep rows, so on a branch whose platform is one cell deep the run now
  terminates on a terminal in the middle of the bar rather than at its ends. See the R18 fallout
  bullet above; not fixed, out of scope.
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

