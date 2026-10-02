# Terrain Auto-Tile Prototype

Version: EXP-0.1
Owner: PM
Status: Experiment — not production-accepted yet

## Objective

Validate a Stardew-like high-efficiency terrain workflow without manually cutting or placing every tile.

The production goal is:

AI / artist creates a very small terrain source set
→ auto-tiler expands adjacency combinations
→ Unity Tile Palette paints terrain
→ the engine selects edge/corner/center variants automatically.

## Selected Prototype

Prototype dependency: Fang Auto Tile 3.0.1 source at commit `1d322cbb191b4613d251a37ad0ee3f5a6ddc2c07`.

License: MIT.

Reason for experiment:
- requires only 5 source tile patterns for the base terrain format;
- generates the full 47 adjacency combinations automatically;
- supports random frames and animated frames;
- supports padding to avoid dirty tile seams;
- supports packing generated tile textures;
- supports connector tiles and experimental slopes.

The repository already contains Unity 2D Tilemap Extras 6.0.3, including Unity AutoTile. Unity AutoTile remains the fallback if the Fang package is incompatible with Unity 6000.3.25f1.

## Source Art Contract

For one terrain type, the base source sheet is:

- width = tile size × frame count;
- height = tile size × 5;
- 5 vertical source patterns per frame.

The artist/AI does **not** create 47 final tiles and does not cut the map manually.

For static terrain, frame count may be 1.

For water or other animated terrain, frames are arranged horizontally and the generated tile can use Animation frame mode.

For grass/sand visual variation, multiple horizontal frames can use Random frame mode.

## Prototype Terrain Set

First validation set:
1. Grass
2. Sand
3. Water
4. Cliff

Do not build the full island until this prototype passes.

## Acceptance Test

The experiment passes only if all are true:

1. Unity 6000.3.25f1 resolves and compiles the package without project errors.
2. A 5-pattern source sheet generates adjacency combinations successfully.
3. Painting an irregular terrain shape does not require manual edge/corner selection.
4. Repainting/removing a tile refreshes surrounding visual connections automatically.
5. Water can run as generated animated auto-tiles.
6. No visible tile seam caused by the generator/padding at normal gameplay zoom.
7. The workflow is materially faster than manual Aseprite slicing.

## Rejection / Rollback

Reject Fang Auto Tile if it fails Unity 6000.3.25f1 compatibility, produces unstable assets, or requires manual repair that defeats the efficiency target.

Fallback:
- keep `com.unity.2d.tilemap.extras` 6.0.3;
- use Unity AutoTile 2x2/3x3 masks and reusable AutoTile templates.

## Storage

Formal repository:
`F:\\IslandLife\\IslandLife`

Temporary local experiment copies:
`F:\\临时开发区\\IslandLife`

Do not create IslandLife development copies under the Windows C: temp locations.
