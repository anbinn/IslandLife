# IL-ART-002 — AI Map Generation Visual Benchmark

Status: READY_FOR_VISUAL_TEST  
Owner: PM / Interactive Art Test  
Architecture Decision: NONE — benchmark only

## Goal

Determine whether a current AI game-art/map tool can produce IslandLife terrain/world content close enough to the approved first-island visual master that manual terrain-asset production can be substantially reduced.

## Locked Input

The approved first-island visual master is the reference.

Do not change:

- 2D presentation;
- approximately 45-degree tilted top-down / three-quarter camera;
- warm cozy island-life atmosphere;
- detailed pixel / pixel-hybrid rendering language;
- tropical island subject;
- readable beach / grass / water / cliff / vegetation separation.

## Phase A — PixelLab First

PixelLab is the first benchmark because it combines map generation, tiles/terrain generation, object generation, reference guidance, editing, and game-oriented workflows.

### Test A1 — Whole-scene generation

Use the first-island visual master as init/reference guidance where supported.

Generate only a small island patch first, containing:

- ocean/water;
- curved sandy beach;
- grass interior;
- one elevated cliff/highland edge;
- a few trees/rocks;
- no UI;
- no labels.

Do not attempt the full island.

### Test A2 — Projection fidelity

Reject the output if it becomes:

- true 90-degree bird's-eye;
- tactics-style isometric diamond;
- realistic 3D;
- generic low-resolution RPG tiles;
- a different art direction from the visual master.

The test exists to match IslandLife, not to discover a new style.

### Test A3 — Editability

Make one controlled edit:

- move/change one shoreline section OR
- change one grass/sand boundary OR
- remove/replace one tree.

Record whether the change can be made without rebuilding the scene.

### Test A4 — Asset generation

Generate at least:

- one compatible terrain transition/source set;
- one tree or rock map object.

Check whether these can visually coexist with the whole-scene output.

## Evidence Required

- original approved visual master;
- generated scene screenshot/export;
- one edited-version screenshot/export;
- generated terrain/object examples;
- elapsed hands-on time;
- exact tool/mode/settings used;
- PASS / PARTIAL / FAIL.

## Acceptance

PASS requires:

1. camera/projection is recognizably compatible with the locked IslandLife view;
2. visual language is close enough that refinement is cheaper than recreating the art manually;
3. terrain transitions do not show the old obvious manual seam problem;
4. at least one local edit is practical;
5. generated object/terrain source can be kept stylistically consistent;
6. workflow is materially faster than the previous manual cut-and-repair experiment.

A beautiful image that cannot preserve the camera/style is FAIL.

A beautiful image that is only a non-editable concept is PARTIAL, not PASS.

## Timebox

Do not spend hours tuning prompts.

First decision checkpoint: 30 minutes of hands-on generation/editing.

If the required projection/style is still clearly wrong after the timebox, stop and record PARTIAL/FAIL rather than inventing a custom repair pipeline.

## After the Benchmark

- PASS -> verify Unity export/integration on a disposable test, then decide production workflow.
- PARTIAL -> keep the tool only for the part it does well (for example asset generation).
- FAIL -> move to the next researched candidate; do not distort IslandLife's art direction to fit the tool.
