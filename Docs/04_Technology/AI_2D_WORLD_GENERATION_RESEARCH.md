# AI-Assisted 2D World / Map Generation Research

Version: R0.3  
Date: 2026-10-02  
Owner: PM  
Status: Acceleration research; controllable hybrid workflow is production baseline

## Locked Constraints

This research optimizes production only. It does not reopen IslandLife's art direction.

Mandatory final result:

- 2D presentation.
- Approximately 45-degree tilted top-down / three-quarter view.
- Approved first-island visual master remains the visual target.
- Warm, detailed, cozy pixel / pixel-hybrid language remains locked.
- A tool is rejected if its speed depends on changing the game into realistic 3D, true isometric tactics art, 90-degree bird's-eye art, or another visual identity.

## Mainstream Production Baseline

Verified references support a controllable hybrid workflow rather than one-shot whole-map generation.

- Unity Happy Harvest: top-down farming sample using Tilemap, Rule Tiles, independent scene content, gameplay APIs, 2D lighting and shaders.
- Stardew Valley public map documentation: layered terrain/building/path maps, tile properties, tilesheets, and separate location/game data.
- Unity Lost Crypt: SpriteShape for organic outdoor terrain plus Tilemap/Rule Tile for grid-suited areas and separate decorative objects.
- Cult of the Lamb: a different visual target, but its developers publicly describe direct editor layout and a hybrid scene structure under a controlled camera.

IslandLife baseline:

- Unity-native editable world is the source of truth.
- Tilemap/Rule Tile for grid-suited terrain.
- SpriteShape/freeform sprites for organic boundaries where useful.
- Independent GameObjects/Sprites for interactive objects.
- Hidden logical data for placement, occupancy, farming and build rules.
- Generation/AI accelerates art production and repetitive work but does not replace editability.
- Paid art asset packs are excluded.

Sources:
- https://unity.com/blog/games/happy-harvest-demo-latest-2d-techniques
- https://discussions.unity.com/t/how-to-create-art-and-gameplay-with-2d-tilemaps/1643416
- https://stardewvalleywiki.com/Modding:Maps
- https://unity.com/blog/games/download-new-2d-sample-project-lost-crypt
- https://unity.com/blog/games/recipe-behind-smash-hit-cult-of-the-lamb

## Research Question

Can an existing 2026 tool generate enough of the map and its source assets automatically that IslandLife avoids manually authoring every terrain sprite, transition, prop, and map placement?

## Candidate 1 — SpriteFlow Game Map Generator

Current finding: strongest candidate for the **whole-map visual benchmark**.

Verified current capabilities:

- explicit Three-quarter projection;
- Pixel — Modern Retro / 16-bit / hand-drawn 2D presets;
- up to three reference images retained as visual references;
- 1K, 2K HD and 4K UHD PNG output;
- region/world/settlement/top-down-level templates;
- existing examples include a Classic Island Overworld, Cozy Farming Valley, and three-quarter modern-retro scenes;
- free accounts receive starter credits; generation begins at 3 credits for 1K.

This is closer than PixelLab's current Create Map tool to IslandLife's locked approximately 45-degree three-quarter presentation.

Critical limitation:

SpriteFlow explicitly labels the result as a visual concept. It does not output native Unity Tilemaps, collisions, navigation, object layers, or gameplay-ready map data.

For IslandLife this limitation may still be acceptable for a **baked visual terrain** experiment, because interactive trees/rocks/buildings/crops can remain separate GameObjects and gameplay placement can remain on a hidden logical grid. The benchmark must prove whether a generated high-resolution base terrain can survive close gameplay viewing and local editing.

Source:
- https://spriteflow.io/game-map-generator

## Candidate 2 — PixelLab

Current finding: strongest candidate for **generated production assets / terrain kits**, but not currently the strongest whole-map renderer.

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

## Candidate 3 — Scenario + Retro Diffusion

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

## Candidate 4 — Frigga

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

The research now separates two jobs that should not be forced into one tool:

1. **Whole-map / base-terrain visual generation:** SpriteFlow currently deserves the first benchmark because it explicitly supports three-quarter projection, reference images and up to 4K output.
2. **Reusable production asset generation:** PixelLab currently deserves the first benchmark for terrain transitions, oblique/isometric tile kits, map objects, style-consistent objects and editing.

Important correction: PixelLab's current Create Map documentation exposes high top-down bird's-eye or sidescroller map views; its general image/Pro tile tools expose low/high top-down and isometric/oblique controls. Therefore do not assume PixelLab Create Map itself can reproduce IslandLife's locked three-quarter world view.

### Fastest candidate pipeline to test

**Generated baked base terrain + independent interactive objects + hidden gameplay grid**

- Generate the non-interactive visual terrain/background in the locked three-quarter view.
- Keep trees, rocks, chests, buildings, crops, workstations and other interactables out of (or removable from) the baked terrain where practical.
- Generate those interactive objects separately in a matching style.
- Unity handles sorting, collision, interaction and placement through independent objects and hidden logical data.
- Farming/building changes can be overlays rather than destructive edits to the base terrain.

This pipeline can eliminate most manual terrain slicing if the visual benchmark passes. It does not require the final game to become 3D or visibly grid-based.

### Benchmark order

A. SpriteFlow — whole-scene / base-terrain visual fidelity.
B. PixelLab — reusable terrain/object generation and local editability.
C. If A+B do not combine cleanly, Frigga/SpriteShape remains the assembly fallback using generated assets.

Do not build custom terrain tooling until these existing workflows have been visually tested.

## Rule

Do not purchase, import, or architect around any candidate solely from marketing claims. A visual benchmark against the approved first-island master is mandatory before production adoption.
