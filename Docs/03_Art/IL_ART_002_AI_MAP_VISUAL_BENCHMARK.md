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

## Phase A — SpriteFlow Whole-Map Test

SpriteFlow is the first whole-map benchmark because its current map generator explicitly supports Three-quarter projection, Pixel — Modern Retro output, up to three visual reference images, and 1K/2K/4K PNG output.

Use the approved first-island master as the primary reference. Do not rely on text-only style matching.

### Test A1 — Whole-scene generation

Use the first-island visual master as a reference image.

Recommended first-run configuration:

- Map Type: Region Map or Top-down Level (choose the one that allows the island patch to read as playable terrain rather than a symbolic world map).
- View & Projection: Three-quarter.
- Art Style: Pixel — Modern Retro.
- Aspect Ratio: 16:9 for the first patch.
- Resolution: 1K for the first decision run; do not spend 4K credits before visual direction passes.
- Labels: Off.
- Grid Overlay: Off.
- Layout: tropical island / organic boundary / high water coverage / low landmark density.
- Extra direction: preserve the reference camera, warm tropical palette, detailed cozy pixel-hybrid rendering, readable beach-to-grass transition, visible cliff depth; no realistic 3D, no tactical diamond grid, no labels, no UI.

Generate only a representative island patch first.

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

### Test A3 — Baked-terrain suitability

Evaluate whether the generated scene can function as a visual base layer if interactive objects are separated.

Check:

- terrain scale is consistent enough for a player sprite;
- beach/grass/water/cliff geometry reads correctly at gameplay zoom;
- there are no perspective contradictions that would break movement;
- the terrain can plausibly be split/chunked if needed;
- major trees/rocks/buildings can be excluded, removed, or covered by separately generated interactive objects.

### Test A4 — Reference fidelity

Compare directly against the approved first-island master:

- camera;
- palette;
- texture/detail density;
- cliff depth;
- coastline language;
- vegetation language;
- overall cozy/commercial finish.

Do not accept a technically impressive map that changes IslandLife's visual identity.

## Phase B — PixelLab Asset Test

Only after (or independently of) the SpriteFlow whole-map result, test PixelLab for reusable production assets.

PixelLab's current Create Map view is documented as high top-down bird's-eye; do not use that as evidence that it matches the locked IslandLife projection.

Use PixelLab Pro/general tools for the jobs where it exposes style reference and oblique/view controls.

### Test B1 — Terrain source

Generate one compatible terrain transition/source set, preferably grass-to-sand or grass-to-cliff, using the approved visual master/style reference where supported.

### Test B2 — Interactive object

Generate one tree or rock as an independent map object/source asset in the same visual language.

### Test B3 — Local edit

Use inpainting/editing on one generated source asset and verify that a small correction does not require regeneration of the whole set.

### Test A5 — Editability

For the whole-map output, make one controlled revision if the tool supports a practical rerun/reference iteration. Record whether local control is sufficient or whether the output must be treated as a baked base image.

Do not downgrade the result solely because it is not a Tilemap. The purpose of this test is to determine whether baked visual terrain plus separate gameplay data is viable.

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
3. the base terrain does not reproduce the old manual tile/seam workload;
4. the map can plausibly serve as a baked/chunked visual terrain layer while gameplay stays separate;
5. PixelLab or another asset generator can produce independent objects/terrain overlays that are stylistically compatible;
6. workflow is materially faster than the previous manual cut-and-repair experiment.

A beautiful image that cannot preserve the camera/style is FAIL.

A beautiful image that is only a concept is not automatically a failure. If it can serve directly as a stable baked visual terrain layer with hidden gameplay data and separate interactive objects, record that explicitly and continue the Unity integration gate. If it cannot survive gameplay-scale use, mark PARTIAL.

## Timebox

Do not spend hours tuning prompts.

First decision checkpoint: 30 minutes of hands-on generation/editing.

If the required projection/style is still clearly wrong after the timebox, stop and record PARTIAL/FAIL rather than inventing a custom repair pipeline.

## After the Benchmark

- SpriteFlow visual PASS -> test the exported PNG as a disposable Unity base-terrain layer with hidden grid + one independent tree/chest.
- PixelLab asset PASS -> test generated tree/rock/terrain overlay against that base.
- PARTIAL -> keep each tool only for the part it does well.
- FAIL -> move to the next researched candidate; do not distort IslandLife's art direction to fit the tool.
