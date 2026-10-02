# AI-Assisted 2D World / Map Generation Research

Version: R0.1  
Date: 2026-10-02  
Owner: PM  
Status: Candidate selection for visual benchmark

## Locked Constraints

This research optimizes production only. It does not reopen IslandLife's art direction.

Mandatory final result:

- 2D presentation.
- Approximately 45-degree tilted top-down / three-quarter view.
- Approved first-island visual master remains the visual target.
- Warm, detailed, cozy pixel / pixel-hybrid language remains locked.
- A tool is rejected if its speed depends on changing the game into realistic 3D, true isometric tactics art, 90-degree bird's-eye art, or another visual identity.

## Research Question

Can an existing 2026 tool generate enough of the map and its source assets automatically that IslandLife avoids manually authoring every terrain sprite, transition, prop, and map placement?

## Candidate 1 — PixelLab

Current finding: strongest candidate for the next benchmark.

PixelLab is purpose-built for pixel-art game production rather than generic image generation. Current public product/docs expose:

- scene/environment generation;
- game-map generation;
- top-down tileset generation;
- Pro terrain/path/building tile generation;
- isometric and oblique tile/projection support;
- map-object generation;
- reference/style-guided generation;
- inpainting/editing;
- character/object rotations;
- Map Workshop;
- API and MCP automation surfaces.

Important capabilities for IslandLife:

1. Create Map can generate game-like pixel-art maps from text plus an init image.
2. Map generation supports iterative extension/inpainting instead of redrawing the whole map.
3. Create Tiles Pro can generate terrain transitions, paths and building kits.
4. Pro terrain sets support square top-down, isometric and oblique shapes; current documentation exposes view/depth controls.
5. PixelLab can use reference imagery for style consistency on supported generation routes.
6. Current product offers map generation on Tier 1.
7. PixelLab's terms state the user retains ownership of generated output and may use it commercially.
8. Public 2026 ecosystem documentation reports Map Workshop Unity/Godot export. This must be verified directly in the subscribed UI before production adoption; do not treat third-party route mapping as final proof.

Caveat:

PixelLab's basic Create Map camera is documented as high top-down/bird's-eye, which is not automatically the same as IslandLife's locked approximately 45-degree three-quarter view. The benchmark must therefore test whether init/reference guidance plus the Pro oblique/view controls can preserve the approved projection. If not, PixelLab can still remain an asset generator rather than the map renderer.

Official sources:
- https://www.pixellab.ai/
- https://www.pixellab.ai/docs/guides/map-tiles
- https://www.pixellab.ai/docs/tools/create-map
- https://www.pixellab.ai/docs/tools/create-tileset
- https://www.pixellab.ai/docs/tools/create-tiles-pro
- https://www.pixellab.ai/docs/tools/create-isometric-tile
- https://www.pixellab.ai/docs/options/camera
- https://www.pixellab.ai/docs/options/projection
- https://www.pixellab.ai/termsofservice

## Candidate 2 — Scenario + Retro Diffusion

Scenario currently exposes purpose-built pixel-art models including Retro Diffusion Tile and Retro Diffusion Plus.

Strengths:

- seamless pixel-art terrain/tile generation;
- top-down map/environment generation;
- custom/style-consistent asset generation;
- strong candidate for producing many matching props/terrain assets.

Weakness for the current question:

- it is primarily an asset-generation pipeline, not a proven one-click IslandLife world/map authoring pipeline;
- map composition / gameplay data would still need another system.

Use as a secondary art-generation candidate if PixelLab cannot match the visual master.

Sources:
- https://www.scenario.com/models?tag=pixel+art
- https://www.scenario.com/models/retro-diffusion-tile
- https://help.scenario.com/articles/4202673551-retro-diffusion-models-the-essentials

## Candidate 3 — Frigga

Frigga is a Unity-native procedural 2D level/map generator.

Current Asset Store description explicitly includes:

- procedural islands;
- forests and biomes;
- overworlds;
- multi-layer Tilemap generation;
- rule-based decoration;
- hand-authored + procedural hybrid maps;
- Tilemaps and GameObjects;
- Unity 6 compatibility.

This is valuable for automatically arranging a world once compatible art exists.

Weakness:

Frigga does not eliminate the art-source problem by itself. It is best considered as a procedural placement/generation layer paired with generated or authored IslandLife assets.

Source:
- https://marketplace.unity.com/packages/tools/utilities/frigga-procedural-dungeon-level-map-generator-227242

## Candidate 4 — SpriteFlow Game Map Generator

SpriteFlow can generate visual 2D map concepts with:

- top-down / three-quarter / isometric projections;
- pixel / modern-retro / hand-drawn presets;
- up to three reference images;
- explicit examples for cozy farming and survival spaces.

This is unusually close to IslandLife's required camera/art-direction vocabulary.

However, SpriteFlow explicitly states that its output is a visual concept, not an engine-ready Tilemap, collision layer, navigation mesh, or gameplay-validated level.

Use it as a fast whole-map concept/layout generator, not as the current production world system.

Source:
- https://spriteflow.io/game-map-generator

## Candidate 5 — Sprixen

Sprixen currently advertises AI-generated sprites, maps, an isometric Map & World Builder, style locking, and Unity/Godot-oriented exports.

Potentially useful, but it is less well validated for IslandLife than PixelLab and its highlighted map workflow is strongly isometric. Keep as a secondary benchmark, not the first paid test.

Source:
- https://sprixen.com/

## Current Conclusion

No researched tool has yet been proven to satisfy all three IslandLife requirements in one click:

1. approved 2D three-quarter visual style;
2. production-editable world/map data;
3. gameplay-ready logical layers.

But PixelLab is materially closer to the requested "generate the map and assets for us" workflow than the previously researched SpriteShape/Fang-only pipelines.

Therefore the next action is not another Unity terrain implementation. It is a controlled PixelLab visual-production benchmark.

If PixelLab passes the visual/projection test, evaluate its Unity export and map-editing workflow before building custom terrain tooling.

If PixelLab fails the visual/projection test, preserve it as an asset generator and test the hybrid:
PixelLab/Scenario generated art -> SpriteShape or Frigga/Tilemap placement -> hidden IslandLife gameplay grid.

## Rule

Do not purchase, import, or architect around any candidate solely from marketing claims. A visual benchmark against the approved first-island master is mandatory before production adoption.
