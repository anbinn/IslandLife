# IslandLife World / Terrain Workflow Research

Version: R0.1  
Date: 2026-10-02  
Owner: PM  
Status: Research recommendation — prototype required before production lock

## 1. Objective

Find the fastest practical workflow for producing IslandLife's approved first-island visual result without repeating the manual tile cutting / seam-repair workflow.

The visual result is the constraint. The renderer or authoring method is not.

## 2. Research Principle

Before inventing project-specific tooling, prefer:

1. current Unity-supported workflows;
2. proven shipped/sample-project workflows;
3. mature external editors/importers only when they reduce total production cost;
4. the smallest prototype that can prove visual quality and authoring speed.

Do not build custom automation, terrain frameworks, or editor tooling until an existing workflow has been tested and shown insufficient.

## 3. Evidence Reviewed

### Unity SpriteShape 13

Current Unity SpriteShape documentation describes SpriteShape as a world-building tool that:

- edits terrain through splines/control points;
- automatically changes/deforms edge sprites based on outline angle;
- supports Closed Shapes with tiled fill textures;
- supports reusable SpriteShape Profiles;
- can update EdgeCollider2D / PolygonCollider2D with the edited shape;
- supports placing GameObjects along a spline;
- generates fill/edge geometry using C# Jobs, with additional Burst benefit when available.

IslandLife already includes `com.unity.2d.spriteshape: 13.0.0`; no new package is required for a prototype.

Sources:
- https://docs.unity3d.com/Packages/com.unity.2d.spriteshape@13.0/manual/index.html
- https://docs.unity3d.com/Packages/com.unity.2d.spriteshape@13.0/manual/SSProfile.html
- https://docs.unity3d.com/Packages/com.unity.2d.spriteshape@13.0/manual/SSController.html
- https://docs.unity3d.com/Packages/com.unity.2d.spriteshape@13.0/manual/SSCollision.html
- https://docs.unity3d.com/Packages/com.unity.2d.spriteshape@13.0/manual/FillTessellation.html

### Unity Lost Crypt production sample

Unity's Lost Crypt sample demonstrates organic 2D terrain built with SpriteShape. Unity describes the workflow as similar to vector drawing: adjust SpriteShape Profiles and edit terrain without manually readjusting many sprites and colliders. The sample also attaches decorative elements such as rocks/flowers to terrain splines.

This is evidence for the authoring pattern, not a requirement to import Lost Crypt into IslandLife.

Sources:
- https://unity.com/blog/games/download-new-2d-sample-project-lost-crypt
- https://assetstore.unity.com/packages/essentials/tutorial-projects/lost-crypt-2d-sample-project-158673

Important: do not import the full Lost Crypt package into IslandLife. Unity's own setup guidance notes that the sample can overwrite project settings. Use it only as a reference project if inspected separately.

### Unity 2026 2D guidance

Unity's 2026 2D guide shows a top-down island/farm scene built from layered world data: water/ground/elevation/farming tile layers plus separate GameObjects for trees, bushes, flowers, rocks, buildings and other objects.

This supports the project principle that visible world presentation, terrain data, and interactive objects do not need to be one monolithic system.

Source:
- https://cdn.bfldr.com/S5BC9Y64/at/w3gts7mvwqptm8wfssrhbnp/2D_game_art_animation_lighting_Unity_63LTS_final.pdf

### LDtk / Tiled

LDtk supports IntGrid logical layers, Auto-Layers and free/grid entity placement. Tiled supports tile layers, free Object Layers, Terrain tools and Automapping.

These are mature alternatives for rule-driven level data, but both add another editor/import pipeline and remain more grid-centric than the first visual-terrain prototype requires.

Sources:
- https://ldtk.io/docs/general/intgrid-layers/
- https://ldtk.io/docs/general/auto-layers/
- https://ldtk.io/api/
- https://doc.mapeditor.org/en/stable/manual/introduction/
- https://doc.mapeditor.org/en/latest/manual/automapping/

## 4. Current Recommendation

Prototype a hybrid world pipeline:

### Visual terrain

Use large editable visual regions rather than forcing the visible island into gameplay cells.

Candidate composition:

- ocean/background: simple large visual layer;
- beach/island footprint: Closed SpriteShape;
- grass interior: Closed SpriteShape layered above beach;
- lake/water regions: separate visual shape/layer;
- cliffs/highlands: separate SpriteShape / edge-stamp layer;
- paths and irregular borders: SpriteShape or reusable stamps;
- final texture/detail pass: independent decorative sprites/stamps.

SpriteShape is a tool in the pipeline, not a requirement that every terrain feature use SpriteShape.

### Hidden gameplay space

Maintain an invisible logical grid/data layer for:

- walkability;
- buildability;
- occupancy;
- farmland;
- water;
- placement validation.

Visible terrain does not need to align visually to cell boundaries.

### Interactive objects

Trees, rocks, chests, buildings, crops and workstations remain independent GameObjects/Sprites with their own footprint/collision/interaction data.

Removing a tree removes the tree object; it does not require repainting terrain.

## 5. Why Not Lock to Other Options Yet

### Fang / full Tilemap

IL-ART-001 proved the Fang generator side works: five source patterns can generate adjacency combinations efficiently. Interactive authoring remains unverified.

Keep Fang as a benchmark/fallback for terrain that genuinely benefits from cell-by-cell rules. Do not make the entire island renderer depend on it yet.

### One giant island texture

A single visual plate is the absolute fastest mock-up route, but it becomes less convenient for local terrain edits and can create large texture-memory costs. Unity supports textures up to 16384 subject to hardware/memory limits; a 16384 RGBA32 texture alone is about 1 GB uncompressed.

Use large visual plates only where they simplify production; prefer layered/chunked/repeatable textures for production.

Source:
- https://docs.unity3d.com/6000.0/ScriptReference/SystemInfo-maxTextureSize.html

### Full 3D / 2.5D

Still a valid future option, especially if the visual master proves difficult in pure 2D. It is not the first prototype because it adds modeling, materials, camera, lighting and pixelization/shader work before proving the island layout.

## 6. Next Prototype

Next prototype should answer only:

> Can IslandLife create an attractive first-island patch quickly using freeform visual terrain while a hidden grid independently controls gameplay placement?

Minimum visual proof:

- ocean;
- irregular beach/island outline;
- grass interior;
- one lake/shore segment;
- one cliff/highland segment;
- several independent trees/rocks;
- one placeable chest;
- hidden grid normally invisible, debug-toggle visible.

The prototype must produce screenshots / visual evidence.

## 7. Prototype Constraints

- Do not build a full terrain framework.
- Do not create custom editor tooling unless the prototype cannot be completed with existing Unity tools.
- Do not add a new external level editor for this prototype.
- Do not implement inventory, crafting, farming simulation, save system or full building system.
- Do not polish the entire first island.
- Do not create final production art.
- Do not import the full Lost Crypt project into IslandLife.
- Measure both visual quality and authoring time.

## 8. Decision Gate

After the visual prototype, compare:

- visual similarity to the approved first-island master;
- time required to create/edit terrain;
- manual repair burden;
- ability to move/replace interactive objects;
- hidden-grid placement compatibility;
- mobile feasibility.

Only then lock the production terrain workflow.
