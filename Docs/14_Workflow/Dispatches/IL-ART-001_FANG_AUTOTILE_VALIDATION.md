# Dispatch IL-ART-001 — Fang Auto Tile Compatibility & Workflow Validation

Status: IN_PROGRESS
Owner: Worker
PM Review: Required
Branch: `experiment/terrain-autotile`

## Objective

Validate whether Fang Auto Tile can become IslandLife's terrain-production accelerator under the locked Unity environment.

This is a workflow/compatibility test, not final art production.

Target workflow:

small 5-pattern terrain source sheet
→ Fang generates adjacency combinations
→ Tile Palette paints irregular terrain
→ surrounding edge/corner visuals refresh automatically.

## Execution Environment

- Repository: `anbinn/IslandLife`
- Branch: `experiment/terrain-autotile`
- Formal local repository: `F:\IslandLife\IslandLife`
- Unity: `6000.3.25f1` only
- Temporary IslandLife workspaces, if absolutely needed: `F:\临时开发区\IslandLife`
- Do not create IslandLife repository/worktree/development copies under C: temp locations.

The branch already pins Fang Auto Tile to commit:
`1d322cbb191b4613d251a37ad0ee3f5a6ddc2c07`

Do not change the package revision during this task.

## Allowed Scope

1. Checkout/pull `experiment/terrain-autotile`.
2. Open `Game/Unity/IslandLife` with Unity `6000.3.25f1`.
3. Allow Unity Package Manager to resolve the pinned Fang package.
4. Commit the resulting `Packages/packages-lock.json` update if Unity changes it.
5. Create a minimal terrain prototype under:
   - `Assets/_Project/Art/Terrain/Prototype/`
   - `Assets/_Project/Scenes/Prototype/`
   - `Assets/_Project/Editor/TerrainPrototype/` only if a small editor helper is genuinely useful.
6. Use a disposable/test 5-pattern source sheet. It does not need IslandLife final art quality.
7. Create one Fang Auto Tile and generate its adjacency output.
8. Create a small Tilemap test scene and paint an irregular shape containing:
   - straight edges;
   - outer corners;
   - inner corners;
   - one-tile protrusions/indentations.
9. Erase and repaint several cells to verify surrounding tiles refresh automatically.
10. If the package works, test at least one of:
   - Random mode with 2+ horizontal frames; or
   - Animation mode with 2+ horizontal frames.
   Prefer Animation if it can be tested quickly, because IslandLife water will need it.
11. Record concise validation evidence in:
   `Docs/03_Art/TERRAIN_AUTOTILE_VALIDATION.md`

## Fang Source Format

For the basic non-slope test:

- width = tile size × frame count;
- height = tile size × 5;
- the 5 required source patterns are arranged vertically;
- additional Random/Animation frames are arranged horizontally;
- texture compression must be `None`;
- keep `Enable Padding` enabled for the seam test;
- slopes are out of scope for this task.

Use Fang's own Basic sample/template as reference for the exact 5-pattern geometry. Do not manually author all 47 adjacency outputs.

## Forbidden Scope

- Do not merge into `main`.
- Do not modify PM governance documents.
- Do not start first-island production.
- Do not create final Grass/Sand/Water/Cliff art.
- Do not hand-cut 47 terrain variants.
- Do not change Unity version.
- Do not add unrelated packages.
- Do not refactor existing Core/Data/System scripts.
- Do not add gameplay systems.
- Do not spend time polishing temporary prototype visuals.

## Acceptance Criteria

PASS requires all of the following:

1. Unity `6000.3.25f1` resolves the pinned Fang package without compile/package errors.
2. A valid 5-pattern source generates Fang adjacency combinations successfully.
3. The generated tile can be placed in Tile Palette and painted on a Tilemap.
4. Irregular painted shapes automatically select correct edge/corner/center variants.
5. Erasing/repainting a cell refreshes neighboring visuals automatically.
6. Padding does not introduce obvious dirty seams at normal gameplay zoom.
7. Random or Animation multi-frame mode is demonstrated successfully.
8. No unrelated project files are modified.
9. Validation document records:
   - Unity/package result;
   - exact test performed;
   - PASS / BLOCKED;
   - known issues;
   - changed files;
   - whether PM should continue with Fang or fall back to Unity AutoTile.

If package compatibility fails, do not patch Fang extensively. Record the exact error and stop at BLOCKED so PM can decide whether to use the existing Unity AutoTile fallback.

## Deliverables

Worker must push to `experiment/terrain-autotile` and return:

- Commit SHA
- Changed files
- Diff summary
- Unity Console result
- Test result: PASS or BLOCKED
- Any screenshot(s) of the painted irregular Tilemap test
- Final status: `WAITING_PM_ACCEPTANCE`

## Expected Commit Message

`test: validate Fang terrain auto-tiling on Unity 6.3`

## PM Verification Rule

Worker completion text is not acceptance.

PM will review the pushed GitHub commit/diff before the task is accepted.
