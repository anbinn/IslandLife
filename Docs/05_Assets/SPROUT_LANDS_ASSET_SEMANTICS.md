# Sprout Lands Asset Semantics & Catalog

Version: T0.3
Owner: PM
Last verified: ASSET-FOUNDATION-02, branch `IL-WORLD-003A`, HEAD `41c11af7`

Scope: **only the Sprout Lands Basic assets already present in this project.**
The Sorry Pack, the UI Pack and all external candidates are explicitly out of scope and
were **not** opened.

---

## 0. Hard rules

1. **TILE ≠ OBJECT.** A 16x16 source grid does not mean every game object is 16x16.
   Terrain is tile-oriented, objects are object-oriented, items are item-oriented,
   animations are frame-oriented.
2. **`rNcN` names are forbidden as the user-facing vocabulary.** They answer
   "where was this pixel rect", not "what is this". They may remain internally where
   technically required.
3. **Completeness is not placement permission.** A complete sprite may still be illegal on a
   surface (lily pad on grass). Placement comes from semantic category only.
4. **UNKNOWN beats a guess.** No asset whose identity cannot be proven enters the scene.
5. **Never present a guess as AUTHOR_DEFINED.** Use `NAME_SOURCE = PROJECT` and a
   `CONFIDENCE` when the author's naming is not proven by evidence.

### Categories
`GROUND_LAND` `GROUND_WATER` `PLANT_LAND` `PLANT_WATER` `TREE` `BUSH` `ROCK` `CROP`
`HARVEST_ITEM` `PICKUP_ITEM` `SEED_PACK` `TOOL` `MATERIAL` `FURNITURE` `CHEST`
`BRIDGE` `FENCE` `DOOR` `BUILDING` `STRUCTURE` `INTERACTIVE_PROP` `DECORATIVE_PROP`
`ANIMATION_SHEET` `REFERENCE_ONLY` `UNKNOWN`

### Naming prefixes
`Terrain_` `Tree_` `Bush_` `Rock_` `Plant_` `Flower_` `Mushroom_` `Crop_` `Item_` `Tool_`
`Material_` `Furniture_` `Prop_` `Building_` `Structure_` `Bridge_` `Fence_` `Door_`
`Chest_`

### Author facts used (PM-verified, not re-researched)
- Base tile 16x16.
- Tile update uses a 3x3 minimal bitmask reference; sheets may include extra decorative
  sprites; layering expected.
- Plants update distinguishes mushrooms, bushes, fruit trees, flowers, farming plants,
  seed packs, four growth stages, item icon.
- Author crop/plant names include: Corn, Carrot, Cauliflower, Tomato, Eggplant,
  Blue Tulip, Lettuce, Wheat, Parsnip, Red Flower, Beet, Blue Star Fruit, Cucumber.

**None of those thirteen named crops can be proven present in the imported Basic sheets.**
See §Farming.

---

## 1. Source sheet audit

All 25 imported source files under
`Assets/Art/Environment/SproutLands/{Objects,Tilesets}/`.
Every PNG currently shares: `textureType: 8` (Sprite), `spriteMode: 2` (Polygon),
`spritePixelsToUnits: 16`, `spritePivot: {0.5, 0.5}`.

"slices" = entries in the importer's `internalIDToNameTable`.
"generated" = existing `Sprites/*_rNcN.asset` files.

| Source file | Dim | Importer slices | Generated .asset | Handling class | Import correctness | Required action |
|---|---|---|---|---|---|---|
| `Tilesets/Grass.png` | 176x112 | 77 (`Grass_r0c0..r6c10`) | 77 | `TERRAIN_TILESET` | **CORRECT** (only grid-named sheet) | none — production AutoTile depends on it |
| `Tilesets/Water.png` | 64x16 | 1 (`Water_0`) | 4 (`Water_r0c*`) | `ANIMATED_TILE` | **PARTIALLY_CORRECT** | reconcile: 3 generated names not in importer table; `AnimatedTile_Water` depends on them |
| `Tilesets/Hills.png` | 176x144 | 1 (`Hills_0`) | 99 (`Hills_r*c*`) | `TERRAIN_TILESET` | **INCORRECT** | 98 stale generated sprites; unused — do not activate |
| `Tilesets/Tilled_Dirt.png` | 176x112 | 8 | 77 | `TERRAIN_TILESET` | **INCORRECT** | 76 stale; unused in production |
| `Tilesets/Tilled_Dirt_v2.png` | 176x112 | 8 | 77 | `TERRAIN_TILESET` | **INCORRECT** | 77 stale; unused |
| `Tilesets/Tilled_Dirt_Wide.png` | 176x112 | 10 | 77 | `TERRAIN_TILESET` | **INCORRECT** | 77 stale; unused |
| `Tilesets/Tilled_Dirt_Wide_v2.png` | 176x112 | 8 | 77 | `TERRAIN_TILESET` | **PARTIALLY_CORRECT** | 24 stale; **aliased as fake Sand/Beach — legacy incorrect semantics** |
| `Tilesets/Tilled Dirt.png` | 128x128 | 8 | 0 | `TERRAIN_TILESET` | **UNKNOWN** | duplicate-of-name, 128x128, 0 generated sprites; decide keep/archive |
| `Tilesets/Wooden House.png` | 112x80 | 3 | 0 | `OBJECT_SHEET` | **UNKNOWN** | building variants; not structural tiles |
| `Tilesets/Wooden_House_Roof_Tilset.png` | 112x80 | 2 | 0 | `STRUCTURAL_TILESET` | **UNKNOWN** | modular roof pieces — do not merge into one object |
| `Tilesets/Wooden_House_Walls_Tilset.png` | 80x48 | 2 | 0 | `STRUCTURAL_TILESET` | **UNKNOWN** | modular wall pieces |
| `Tilesets/Doors.png` | 16x64 | 1 | 0 | `ANIMATION_SHEET` | **UNKNOWN** | author documents door animation; frame grouping unproven |
| `Tilesets/Fences.png` | 64x64 | 4 | 0 | `ANIMATION_SHEET` | **UNKNOWN** | author documents fence-gate animation; grouping unproven |
| `Tilesets/Bitmask references 1.png` | 480x256 | 1 | 0 | `REFERENCE_ONLY` | CORRECT (as reference) | never a game object |
| `Tilesets/Bitmask references 2.png` | 480x256 | 1 | 0 | `REFERENCE_ONLY` | CORRECT (as reference) | never a game object |
| `Tilesets/Bitmask references gif.gif` | 24860x540 | 1 | 0 | `REFERENCE_ONLY` | CORRECT (as reference) | never a game object |
| `Objects/Basic_Grass_Biom_things.png` | 144x80 | 35 | 45 | `OBJECT_SHEET` | **PARTIALLY_CORRECT** | 10 stale generated; contains trees/bushes spanning multiple cells |
| `Objects/Basic_Plants.png` | 96x32 | 12 | 12 | `ANIMATION_SHEET` + `ITEM_SHEET` | **PARTIALLY_CORRECT** | counts match but **names differ** (`Basic_Plants_0` vs `Basic_Plants_r1c0`); all 12 generated names absent from importer |
| `Objects/Basic_Furniture.png` | 144x96 | 29 | 0 | `OBJECT_SHEET` | **UNKNOWN** | non-uniform rects; map to complete furniture |
| `Objects/Basic_tools_and_meterials.png` | 48x32 | 6 | 0 | `ITEM_SHEET` | **UNKNOWN** | tools/materials |
| `Objects/Chest.png` | 240x96 | 10 | 0 | `ANIMATION_SHEET` | **UNKNOWN** | author documents chest animation; frame grouping unproven |
| `Objects/Egg_item.png` | 16x16 | 1 | 0 | `ITEM_SHEET` | UNKNOWN | single item icon |
| `Objects/Free_Chicken_House.png` | 48x48 | 1 | 0 | `OBJECT_SHEET` | **UNKNOWN** | one large sprite (~40x48), already object-oriented |
| `Objects/Paths.png` | 64x64 | 8 | 0 | `STRUCTURAL_TILESET` | UNKNOWN | wooden plank path pieces |
| `Objects/Simple_Milk_and_grass_item.png` | 64x16 | 4 | 4 | `ITEM_SHEET` | **PARTIALLY_CORRECT** | 4 generated, **0 referenced** |
| `Objects/Wood_Bridge.png` | 80x48 | 4 | 0 | `OBJECT_SHEET` | UNKNOWN | non-16x16 directional bridge variants |

### Cross-cutting import finding (NEW, pre-existing)

`Grass.png` is the **only** sheet whose importer still declares the 16x16 grid naming
(`Grass_r0c0`). Every other sheet was imported in `spriteMode: 2` with the author's own
sequential names (`Hills_0`, `Tilled_Dirt_0`, `Basic_Plants_0`, ...).

Consequence: the `Sprites/` folder mixes
(a) one grid-derived family (`Grass`, 77, production-critical) with
(b) many **legacy grid-derived families whose names no longer exist in any importer**.

This is why `rNcN` files exist for Hills/Water/Tilled_Dirt at all. It is pre-existing
technical debt, **not** something this card changed. The author's own rects are the
authoritative shapes (per §6/§10: audit current rects, do not assume they are wrong).

**Safe to change importer?** `Grass.png` **NO** (production AutoTile references its
generated sprites). `Water.png` **NO** (`AnimatedTile_Water` references 4 generated
sprites). All other sheets: **YES**, but not in this card.

---

## 2. Semantic catalog

Counts are distinct visual objects, not source cells.

> ### Row-orientation convention — AUTHORITATIVE
>
> **`rN` = image row counted from the TOP** (`r0` = topmost row).
> `m_Rect.y = sheetHeight - (N + 1) * 16`.
>
> Verified directly from the generated Sprite assets, per sheet:
> `Basic_Grass_Biom_things` `r0=64 … r4=0` · `Basic_Plants` `r0=16 … r1=0` ·
> `Grass` `r0=96 … r6=0` · `Hills` `r0=128 … r8=0` · `Tilled_Dirt*` `r0=96 … r6=0`.
> The convention is **uniform across every sheet**.
>
> The earlier audit interpreted row orientation incorrectly and is **superseded**.
> Every `rNcN` index it recorded was re-derived from source pixels for this card:
> connected-component flood fill over non-transparent pixels, plus direct visual
> inspection at 12x–26x. **Source evidence wins over the earlier labels.**
>
> **All 45 cells of `Basic_Grass_Biom_things` and all 12 cells of `Basic_Plants`
> are now identified below. There is no second, older mapping left in this document.**

### ⚠ `Basic_tools_and_materials.png` — no addressable slices

`Basic_tools_and_meterials.png` has **0 generated Sprite slices**. The earlier
`Tool_WateringCan`/`Tool_Axe`/`Tool_Pickaxe`/`Material_Stone`/`Material_Wood`
`rNcN` references pointed at assets that **do not exist in this project** and are
withdrawn. The art exists only inside the texture importer. No `rNcN` mapping is
recorded because none can be verified.

### `Simple_Milk_and_grass_item.png` — re-confirmed

Single-row sheet, so row orientation cannot affect it. `Item_Milk_01..03` =
`r0c0`,`r0c1`,`r0c2` and `Plant_GrassPatch_01` = `r0c3` are **confirmed correct**.

| Project name | Source | Visual identity | Category | Sub | Name source | Conf. | Surface | Role | Animated | Complete object | Scene ready |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `Terrain_Grass` | `Grass.png` | green 47-blob sheet | `GROUND_LAND` | terrain | AUTHOR | HIGH | LAND | tile | no | n/a | YES |
| `Terrain_Water` | `Water.png` | flat teal, 4 frames | `GROUND_WATER` | terrain | AUTHOR | HIGH | WATER | tile | **yes** | n/a | YES |
| `Terrain_Hills` | `Hills.png` | grass top + cliff band | `STRUCTURE` | cliff | AUTHOR | MED | LAND | tile | no | n/a | NO (unused) |
| `Terrain_FarmSoil` | `Tilled_Dirt*.png` (5) | pale cream/tan plot tiles | `GROUND_LAND` | farm soil | PROJECT | MED | LAND | tile | no | n/a | NO |
| `Tree_01` | `Basic_Grass_Biom_things` `r0c0`(top)+`r1c0`(bottom) | small tree | `TREE` | tree | PROJECT | HIGH | LAND | object | no | **yes** (2 cells) | **YES (prefab)** |
| `Tree_02` | `r0c1`+`r0c2`(top)+`r1c1`+`r1c2`(bottom) | large tree | `TREE` | tree | PROJECT | HIGH | LAND | object | no | **yes** (4 cells) | **YES (prefab)** |
| `Tree_03` | `r0c3`+`r0c4`(top)+`r1c3`+`r1c4`(bottom) | blossom tree | `TREE` | fruit-bearing look | PROJECT | HIGH | LAND | object | no | **yes** (4 cells) | **YES (prefab)** |
| `Bush_01` | `r3c0`+`r3c1` | bush with pink flowers | `BUSH` | bush | PROJECT | HIGH | LAND | object | no | yes (2 cells) | **YES (prefab)** |
| `Bush_02` | `r4c0`+`r4c1` | bush with log end | `BUSH` | bush | PROJECT | HIGH | LAND | object | no | yes (2 cells) | **YES (prefab)** |
| `Bush_03` | `r4c2`+`r4c3`+`r4c4` | wide bush with log end | `BUSH` | bush | PROJECT | HIGH | LAND | object | no | yes (3 cells) | **YES (prefab)** |
| `Rock_01` | `r1c7` | grey stone | `ROCK` | rock | PROJECT | HIGH | LAND | ground decoration | no | yes | **YES (prefab)** |
| `Rock_02` | `r1c8` | pale grey stone | `ROCK` | rock | PROJECT | HIGH | LAND | ground decoration | no | yes | **YES (prefab)** |
| `Rock_03` | `r4c5` | grey rock, pale base | `ROCK` | rock | PROJECT | HIGH | LAND | rock object | no | yes | **YES (prefab)** |
| `Rock_04` | `r4c6` | small grey rock | `ROCK` | rock | PROJECT | HIGH | LAND | rock object | no | yes | **YES (prefab)** |
| `Rock_Mossy_01` | `r4c7` | moss-covered rock | `ROCK` | rock | PROJECT | HIGH | LAND | rock object | no | yes | **YES (prefab)** |
| `Rock_Mossy_02` | `r4c8` | two moss-covered rocks | `ROCK` | rock | PROJECT | HIGH | LAND | rock object | no | yes | **YES (prefab)** |
| `Mushroom_01` | `r0c5` | brown mushroom cluster | `PLANT_LAND` | mushroom | AUTHOR | HIGH | LAND | ground decoration | no | yes | NO |
| `Mushroom_02` | `r0c6` | brown mushroom cluster | `PLANT_LAND` | mushroom | AUTHOR | HIGH | LAND | ground decoration | no | yes | NO |
| `Mushroom_03` | `r0c7` | purple mushroom | `PLANT_LAND` | mushroom | AUTHOR | HIGH | LAND | ground decoration | no | yes | NO |
| `Mushroom_04` | `r0c8` | two purple mushrooms | `PLANT_LAND` | mushroom | AUTHOR | HIGH | LAND | ground decoration | no | yes | NO |
| `Plant_Sprig_01` | `r1c5` | pale-green leaf sprigs | `LAND_PLANT` | foliage | PROJECT | HIGH | LAND | ground decoration | no | yes | NO |
| `Plant_Sprig_02` | `r1c6` | pale-green leaf sprigs (2) | `LAND_PLANT` | foliage | PROJECT | HIGH | LAND | ground decoration | no | yes | NO |
| `Plant_Berry_01` | `r2c0` | small mauve berries, no outline | `LAND_PLANT` | berry plant | PROJECT | MED | LAND | ground decoration | no | yes | NO |
| `Plant_Berry_02` | `r2c1` | mauve berry + leaf, no outline | `LAND_PLANT` | berry plant | PROJECT | MED | LAND | ground decoration | no | yes | NO |
| `Plant_Berry_03` | `r3c2` | single mauve berry, no outline | `LAND_PLANT` | berry plant | PROJECT | MED | LAND | ground decoration | no | yes | NO |
| `Plant_Berry_04` | `r3c3` | mauve berry + leaves, no outline | `LAND_PLANT` | berry plant | PROJECT | MED | LAND | ground decoration | no | yes | NO |
| `Plant_TallFlower_01` | `r2c8`(top)+`r3c8`(bottom) | sunflower on stem | `LAND_PLANT` | tall flower | PROJECT | HIGH | LAND | decoration | no | yes (2 cells) | NO |
| `Flower_01` | `r2c6` | yellow bud + leaves | `FLOWER` | flower | PROJECT | HIGH | LAND | ground decoration | no | yes | NO |
| `Flower_02` | `r2c7` | yellow flower + leaves | `FLOWER` | flower | PROJECT | HIGH | LAND | ground decoration | no | yes | NO |
| `Flower_03` | `r3c5` | blue flower + leaves | `FLOWER` | flower | PROJECT | HIGH | LAND | ground decoration | no | yes | NO |
| `Flower_04` | `r3c6` | pink bud + leaves | `FLOWER` | flower | PROJECT | HIGH | LAND | ground decoration | no | yes | NO |
| `Flower_05` | `r3c7` | pink flower + leaves | `FLOWER` | flower | PROJECT | HIGH | LAND | ground decoration | no | yes | NO |
| `Prop_Log_01` | `r2c5` | fallen log, cut end + branch stub | `DECORATIVE_PROP` | log | PROJECT | HIGH | LAND | prop | no | yes | NO |
| **`Item_Harvest_01`** | `r2c2` | mauve fruit, **white outline**, leaf | **`HARVEST_ITEM`** | harvest | PROJECT | HIGH | **NONE** | inventory | no | yes | manual spawn only |
| **`Item_Harvest_02`** | `r3c4` | mauve berry cluster, **white outline**, leaves | **`HARVEST_ITEM`** | harvest | PROJECT | HIGH | **NONE** | inventory | no | yes | manual spawn only |
| **`Plant_LilyPad`** | — | — | `WATER_PLANT` | aquatic | — | — | WATER ONLY | water decoration | — | — | **`LILY_PAD_IMPORTED = NOT_FOUND`** |
| **`UNKNOWN_01`** | `r2c3` | tan tapered form, unidentified | **`UNKNOWN`** | — | — | — | **NONE** | — | — | — | **MUST NOT enter scene** |
| **`UNKNOWN_02`** | `r2c4` | tan rounded form, unidentified | **`UNKNOWN`** | — | — | — | **NONE** | — | — | — | **MUST NOT enter scene** |
| `Crop_Sprout_01` | `Basic_Plants` `r0c1` | small green sprouts on soil | `CROP` | sprout | PROJECT | HIGH | LAND | farm | no | yes | NO |
| `Crop_Sprout_02` | `r0c2` | taller green sprouts on soil | `CROP` | sprout | PROJECT | HIGH | LAND | farm | no | yes | NO |
| `Crop_Sprout_03` | `r1c1` | tiny sprout on soil | `CROP` | sprout | PROJECT | HIGH | LAND | farm | no | yes | NO |
| `Crop_Wheat_01` | `r0c3` | golden wheat bundle on soil | `CROP` | wheat | AUTHOR | HIGH | LAND | farm | no | yes | NO |
| `Crop_Wheat_02` | `r0c4` | golden wheat stalks on soil | `CROP` | wheat | AUTHOR | HIGH | LAND | farm | no | yes | NO |
| `Crop_Berry_01` | `r1c2` | small plant, pink centre | `CROP` | berry | PROJECT | MED | LAND | farm | no | yes | NO |
| `Crop_Berry_02` | `r1c3` | leafy plant, pink fruit | `CROP` | berry | PROJECT | MED | LAND | farm | no | yes | NO |
| `Crop_Beet_01` | `r1c4` | large leafy plant, purple root | `CROP` | beet | AUTHOR | MED | LAND | farm | no | yes | NO |
| **`SeedPack_01`** | `r0c0` | tan seed packet | `SEED_PACK` | seed | AUTHOR | HIGH | **NONE** | inventory | no | yes | manual only |
| **`SeedPack_02`** | `r1c0` | tan seed packet, berry graphic | `SEED_PACK` | seed | AUTHOR | HIGH | **NONE** | inventory | no | yes | manual only |
| **`Tool_Sickle`** | `r0c5` | sickle, white outline | `TOOL` | tool | PROJECT | HIGH | **NONE** | inventory | no | yes | NO |
| **`Item_Harvest_03`** | `r1c5` | purple round item, **white outline** | **`HARVEST_ITEM`** | harvest | PROJECT | HIGH | **NONE** | inventory | no | yes | manual spawn only |
| **`UNKNOWN_03..07`** | `Basic_tools_and_materials` | — | **`UNKNOWN`** | — | — | — | **NONE** | — | — | — | **0 generated slices — not addressable** |
| `Plant_GrassPatch_01` | `Simple_Milk_and_grass_item` `r0c3` | grass clump, white outline | `PICKUP_ITEM` | pickup | PROJECT | HIGH | NONE | inventory | no | yes | NO |
| `Item_Milk_01..03` | `Simple_Milk_and_grass_item` `r0c0..r0c2` | milk bottles | `PICKUP_ITEM` | pickup | PROJECT | HIGH | NONE | inventory | no | yes | NO |
| `Item_Egg` | `Egg_item.png` | egg on plate | `PICKUP_ITEM` | pickup | PROJECT | HIGH | NONE | inventory | no | yes | NO |
| `Chest_01` | `Chest.png` (10 rects) | chest/barrel states | `CHEST` | container | AUTHOR | MED | LAND | interactive | **author says yes** | yes | NO |
| `Building_ChickenCoop` | `Free_Chicken_House.png` | coop, one ~40x48 sprite | `BUILDING` | building | PROJECT | HIGH | LAND | building | no | yes | NO |
| `Bridge_01..04` | `Wood_Bridge.png` (4 rects) | directional bridges | `BRIDGE` | structure | PROJECT | MED | LAND | structure | no | yes | NO |
| `Fence_01..04` | `Fences.png` (4 rects) | fence pieces | `FENCE` | structure | AUTHOR | MED | LAND | structure | **author says animated** | ? | NO |
| `Door_01` | `Doors.png` (1 rect) | door + wall strip | `DOOR` | structure | AUTHOR | LOW | LAND | structure | **author says animated** | ? | NO |
| `Building_WoodenHouse` | `Wooden House.png` (3 rects) | house shell/variants | `BUILDING` | building | PROJECT | MED | LAND | building | no | ? | NO |
| `Structure_House_Roof` | `Wooden_House_Roof_Tilset.png` (2 rects) | modular roof | `STRUCTURE` | modular | PROJECT | MED | LAND | construction | no | **no — modular** | NO |
| `Structure_House_Walls` | `Wooden_House_Walls_Tilset.png` (2 rects) | modular walls | `STRUCTURE` | modular | PROJECT | MED | LAND | construction | no | **no — modular** | NO |
| `Structure_PlankPath_01..08` | `Paths.png` (8 rects) | wooden plank path | `STRUCTURE` | path | PROJECT | MED | LAND | construction | no | ? | NO |
| `Furniture_*` (29 rects) | `Basic_Furniture.png` | beds, wardrobes, chairs, tables | `FURNITURE` | interior | PROJECT | MED | INDOOR | furnishing | no | mixed | NO |

### Counts by category
`TERRAIN/STRUCTURE_TILESET` 5 · `ANIMATED_TILE` 1 · `REFERENCE_ONLY` 3 · `OBJECT_SHEET` 4 ·
`ITEM_SHEET` 4 · `ANIMATION_SHEET` 3 · `STRUCTURAL_TILESET` 3 · `BUILDING` 2

Object-level (corrected): `TREE` 3 · `BUSH` 3 · `ROCK` 6 · `MUSHROOM` 4 · `LAND_PLANT` 8 ·
`FLOWER` 5 · `WATER_PLANT` **0 (`LILY_PAD_IMPORTED = NOT_FOUND`)** · `HARVEST_ITEM` 3 ·
`DECORATIVE_PROP` 1 · `CROP` 8 · `SEED_PACK` 2 · `TOOL` 1 · `PICKUP_ITEM` 4 ·
`FURNITURE` ~29 · `CHEST` 1 · `BRIDGE` 4 · `FENCE` 4 · `DOOR` 1 · `BUILDING` 2 ·
`STRUCTURE` 10 · `UNKNOWN` 2 + 5 unaddressable tool/material art

AUTHOR-named identities used: Wheat, Beet, mushroom, seed pack, fence, door, chest, grass,
water, hills, tilled dirt. **PROJECT-named**: all tree, bush, rock, sprig, berry, flower,
sprout and log variants. **UNKNOWN**: `r2c3`, `r2c4` (unidentified tan forms), plus the
unaddressable `Basic_tools_and_materials` art.

`Basic_Grass_Biom_things` coverage: **45 / 45 cells identified.**
`Basic_Plants` coverage: **12 / 12 cells identified.**

---

## 3. Tree / Bush result

### 3.1 Source structure (re-verified)

`Tree_01` 2 cells (16x32), `Tree_02` 4 cells (32x32), `Tree_03` 4 cells (32x32);
`Bush_01` 2 cells, `Bush_02` 2 cells, `Bush_03` 3 cells.

Re-derived for ASSET-FOUNDATION-03 from the source PNG, not from the previous catalog:

| Object | Upper cells | Lower cells | Footprint |
|---|---|---|---|
| `Tree_01` | `r0c0` | `r1c0` | 1 x 2 |
| `Tree_02` | `r0c1`, `r0c2` | `r1c1`, `r1c2` | 2 x 2 |
| `Tree_03` | `r0c3`, `r0c4` | `r1c3`, `r1c4` | 2 x 2 |
| `Bush_01` | — | `r3c0`, `r3c1` | 2 x 1 |
| `Bush_02` | — | `r4c0`, `r4c1` | 2 x 1 |
| `Bush_03` | — | `r4c2`, `r4c3`, `r4c4` | 3 x 1 |

Evidence: connected-component flood fill over non-transparent pixels of
`Objects/Basic_Grass_Biom_things.png` (144x80) yields exactly three tall components in
image rows 0–1 (bbox `1,0-14,27`, `20,1-43,30`, `52,1-75,30`) and three wide components in
image rows 3–4 (bbox `0,48-31,62`, `2,68-31,78`, `36,64-67,78`). Each component's bounding box
is exactly cell-aligned, so placing the source slices at their cell centres reconstructs the
original arrangement with no gap, overlap, or half-object.

### 3.2 Project-facing objects (created)

| Project object | Prefab | Root anchor | Child renderers | SortingGroup |
|---|---|---|---|---|
| `Tree_01` | `Assets/Art/Prefabs/SproutLands/Trees/Tree_01.prefab` | ground contact, local `(0,0,0)` | 2 | YES |
| `Tree_02` | `Assets/Art/Prefabs/SproutLands/Trees/Tree_02.prefab` | ground contact, local `(0,0,0)` | 4 | YES |
| `Tree_03` | `Assets/Art/Prefabs/SproutLands/Trees/Tree_03.prefab` | ground contact, local `(0,0,0)` | 4 | YES |
| `Bush_01` | `Assets/Art/Prefabs/SproutLands/Bushes/Bush_01.prefab` | ground contact, local `(0,0,0)` | 2 | YES |
| `Bush_02` | `Assets/Art/Prefabs/SproutLands/Bushes/Bush_02.prefab` | ground contact, local `(0,0,0)` | 2 | YES |
| `Bush_03` | `Assets/Art/Prefabs/SproutLands/Bushes/Bush_03.prefab` | ground contact, local `(0,0,0)` | 3 | YES |
| `Rock_01` | `Assets/Art/Prefabs/SproutLands/Rocks/Rock_01.prefab` | ground contact, local `(0,0,0)` | 1 | no (single renderer) |
| `Rock_02` | `Assets/Art/Prefabs/SproutLands/Rocks/Rock_02.prefab` | ground contact, local `(0,0,0)` | 1 | no (single renderer) |
| `Rock_03` | `Assets/Art/Prefabs/SproutLands/Rocks/Rock_03.prefab` | ground contact, local `(0,0,0)` | 1 | no (single renderer) |
| `Rock_04` | `Assets/Art/Prefabs/SproutLands/Rocks/Rock_04.prefab` | ground contact, local `(0,0,0)` | 1 | no (single renderer) |
| `Rock_Mossy_01` | `Assets/Art/Prefabs/SproutLands/Rocks/Rock_Mossy_01.prefab` | ground contact, local `(0,0,0)` | 1 | no (single renderer) |
| `Rock_Mossy_02` | `Assets/Art/Prefabs/SproutLands/Rocks/Rock_Mossy_02.prefab` | ground contact, local `(0,0,0)` | 1 | no (single renderer) |

**Object contract (binding for all future world objects):**

- `ROOT` = ground contact / footprint anchor. Root `Transform` sits at the object's base,
  never at its visual centre. Children are offset above and around the root.
- `MULTI_RENDERER` = the root carries a `SortingGroup` (`UnityEngine.Rendering.SortingGroup`,
  serialized classID 210) so the whole object sorts as one unit.
- `SINGLE_RENDERER` = one SpriteRenderer, no `SortingGroup` needed. The rock root still
  carries no renderer; the single `Visual` child sits at local `(0, 0.5)` so the rock rests
  on the ground line instead of being centred on it.
- `SOURCE_SPRITES` = internal implementation detail. The `rNcN` slices are dependencies of
  the prefab and are never user-facing vocabulary.
- Child renderers use equal `m_SortingOrder` and are ordered by `m_Children`, so stacking
  inside the group is deterministic.

**Static verification status: `STATIC_VERIFIED` / `UNITY_VISUAL_PENDING`.**
All 12 prefabs machine-checked: correct source GUID per expected slice, `m_Sprite` fileID
`21300000`, exact local positions, renderer count, `SortingGroup` present on all 6
multi-renderer objects and absent on all 6 rocks, root at `(0,0)` in every case, no duplicate
local fileIDs, no unresolved internal references, uniform CRLF, `m_Pivot 0.5/0.5`,
`m_PixelsToUnits 16`, scale and filter unchanged. **0 failures.**
Not promoted to `UNITY_PASS` — no Editor validation.

### 3.3 Not created

- **`Buildings/ChickenHouse_01.prefab` — `PENDING_UNITY_IMPORT`.**
  `SEMANTIC = CONFIRMED` · `SOURCE_ART = CONFIRMED` · `MILESTONE_BLOCKING = NO`.
  `Free_Chicken_House` exists only as an importer sub-asset with **0** generated
  `Sprites/*.asset` slices, so no Sprite GUID/fileID exists to reference. Not fabricated.
- Empty folder + `.meta` scaffolding exists for `Buildings/`, `Structures/`, `Furniture/`,
  `Props/` so the structure is ready.
- No prefab for items, icons, terrain, UNKNOWN, or modular structure sheets.
- No global Y-sort, Transparency Sort Mode change, or Y-sort script (out of scope).

### 3.4 Carried-over blockers

- No Unity execution → prefabs are **not** Editor-validated; visual validation is pending.
- FirstIsland keeps its inactive scene-local `Tree_01/02/03` and `Tree_0` objects as
  `LEGACY_SCENE_OBJECTS`. They are **not** converted to prefab instances by this card, and
  they were composed from the now-falsified cell mapping, so their art is not authoritative.
- Global Y-sort, Transparency Sort Mode, and a runtime Y-sort script are deliberately
  **not** part of this card.


---

## 4. Farming asset result

`Basic_Plants.png` is 96x32 = 6 columns × 2 rows. **All 12 cells identified** under the
authoritative `r0` = top convention.

| Cell | Identity | Category |
|---|---|---|
| `r0c0` | tan seed packet | `SEED_PACK` |
| `r0c1` | small green sprouts on soil | `CROP` sprout |
| `r0c2` | taller green sprouts on soil | `CROP` sprout |
| `r0c3` | golden wheat bundle on soil | `CROP` wheat |
| `r0c4` | golden wheat stalks on soil | `CROP` wheat |
| `r0c5` | sickle, white outline | `TOOL` |
| `r1c0` | tan seed packet, berry graphic | `SEED_PACK` |
| `r1c1` | tiny sprout on soil | `CROP` sprout |
| `r1c2` | small plant, pink centre | `CROP` berry |
| `r1c3` | leafy plant, pink fruit | `CROP` berry |
| `r1c4` | large leafy plant, purple root | `CROP` beet |
| `r1c5` | purple round item, white outline | `HARVEST_ITEM` |

**Seed / growth / mature / item relationships actually present: NONE.**
There is no evidence of the author's four growth stages, and no crop forms a
seed→growth→mature→item chain. The sprouts (`r0c1`,`r0c2`,`r1c1`) are visually a growth
stage and the mature plants sit beside them, but nothing links a seed pack to a specific
sprout, so **no chain is asserted**. The one white-outlined item (`r1c5`) is not
demonstrably the harvest of `r1c4`.

**Unresolved identities:** `Basic_Grass_Biom_things r2c3`, `r2c4` (tan tapered / rounded
forms) remain **UNKNOWN** and must not enter the scene. The `Basic_tools_and_materials`
art (watering can, axe, pickaxe, stone, logs) exists in the PNG but has **0 generated
slices**, so it is not addressable and no `rNcN` mapping is recorded.

**Author crop names with no proven match in the imported sheets:** Corn, Carrot,
Cauliflower, Tomato, Eggplant, Blue Tulip, Lettuce, Parsnip, Red Flower, Blue Star Fruit,
Cucumber. **These assets are NOT present in the current import.** Not invented, not
downloaded.

`Crop_<Name>_SeedPack` / `_Growth_01..03` / `_Mature` / `Item_<Name>` naming is **defined as
a convention only** — no asset is named this way because no complete crop chain exists in
evidence.

### 4.1 Lily pads

`LILY_PAD_IMPORTED = NOT_FOUND`.

The earlier `r0c7`/`r0c8` = lily pads claim is **false**; those cells are **purple
mushrooms** (`Mushroom_03`, `Mushroom_04`). A full visual sweep of all 45
`Basic_Grass_Biom_things` cells and all 12 `Basic_Plants` cells found **no** lily-pad art.
The WATER-only surface rule is therefore **retired with the asset**, not reassigned.
No replacement was manufactured from another sprite.

---

## 5. Furniture / building result

- **Complete objects:** `Free_Chicken_House` (one ~40x48 sprite — already object-oriented,
  do not reslice); `Wooden House.png` 3 rects (building variants, **not** structural).
- **Modular pieces:** `Wooden_House_Roof_Tilset` (2 rects), `Wooden_House_Walls_Tilset`
  (2 rects), `Paths.png` (8 rects). Must **not** be merged into single objects.
- **Animation / frame groups:** `Chest.png` 10 rects and `Fences.png` 4 rects are
  author-documented as animated, and `Doors.png` 1 rect likewise. **Frame grouping is
  UNPROVEN** — no ordering, frame count or state sequence can be derived from the sheets
  alone. Classified `ANIMATION_SHEET`, `UNKNOWN` grouping. Not merged into static sprites.
- **Wrong cuts:** `Wooden House.png` was previously conflated with the Roof/Wall structural
  tilesets — corrected here. No evidence of any incorrect cut was found in these sheets;
  they use the author's non-uniform rects, which are authoritative.

---

## 6. Item / tool result

All item-like art is `HARVEST_ITEM` / `PICKUP_ITEM` / `SEED_PACK` / `TOOL` / `MATERIAL` with
**ALLOWED_SURFACE = NONE**. None may enter a placement pool.

The white outline on `r2c2` / `r1c4` is the author's item visual language, not vegetation.

---

## 7. Terrain result

Production terrain **untouched and verified**:
Water **1825**, Grass **1100**, Sand **0**, origin **(-32,-22)**, size **65x45**;
`git hash-object` of `FirstIsland_TerrainData.asset` is identical to the HEAD blob.
`AnimatedTile_Water` (visually confirmed animating) and `AutoTile_Grass` (visually confirmed
edges) both preserved. Fake Beach remains disabled; GroundDetails remains inactive;
**active random decoration count = 0**.

**Legacy incorrect semantics recorded (not changed here):**
`TerrainSpriteSet_Sand` and `Tile_Beach_*` both resolve to `Tilled_Dirt_Wide_v2.png`.
There is **no true Sand and no true Beach** in this project. Both names are legacy
incorrect aliases and are documented as such.

---

## 8. Legacy generated sprites

`Assets/Art/Environment/SproutLands/Sprites/` — 549 generated `.asset` files.
**Nothing deleted. No mass deletion. No GUID breakage.**

| Family | Generated | Referenced | Orphans | Classification |
|---|---|---|---|---|
| `Grass_*` | 77 | 50 | 27 | `KEEP_PRODUCTION` (AutoTile_Grass + TerrainSpriteSet_Grass) |
| `Water_*` | 4 | 4 | 0 | `KEEP_PRODUCTION` (AnimatedTile_Water) |
| `Basic_Grass_Biom_things_*` | 45 | 31 | 14 | `KEEP_INTERNAL` |
| `Basic_Plants_*` | 12 | 3 | 9 | `KEEP_INTERNAL` |
| `Simple_Milk_and_grass_item_*` | 4 | 0 | 4 | `UNUSED` |
| `Tilled_Dirt_*` | 77 | 1 | 76 | `UNUSED` |
| `Tilled_Dirt_v2_*` | 77 | 0 | 77 | `UNUSED` |
| `Tilled_Dirt_Wide_*` | 77 | 0 | 77 | `UNUSED` |
| `Tilled_Dirt_Wide_v2_*` | 77 | 53 | 24 | `KEEP_PRODUCTION` (fake Sand/Beach aliases) |
| `Hills_*` | 99 | 1 | 98 | `DEPRECATE_LATER` |

`KEEP_PRODUCTION` entries must not be touched: deleting them breaks AutoTile_Grass,
AnimatedTile_Water, TerrainSpriteSet_Sand or Tile_Beach_*.

---

## 9. Sorting / occlusion audit (audit only, nothing implemented)

Current project state:
- `Renderer2D.asset` → `m_TransparencySortMode: 0` (**Default**); the present
  `m_TransparencySortAxis {x:0, y:1, z:0}` is **inactive**.
- Exactly one sorting layer: `Default`.
- **No `SortingGroup` components anywhere.**
- Every renderer uses a hard-coded `m_SortingOrder`.

Recommended anchor/pivot semantics for later (design only):

| Object | Anchor |
|---|---|
| Tree / Bush / Rock / Prop_Log | ground/base point |
| Building / Coop / House | footprint centre on base |
| Furniture / Chest | footprint centre on base |
| Structure (roof/wall/path/fence/door) | per-tile, modular |

What a later system must support:
- character behind a tree → the tree canopy may cover the character;
- character in front → the character renders in front appropriately;
- a multi-renderer tree sorts as **ONE logical object**.

**Verdict: MISSING.** No Y-sort, no transparency sort axis, no sorting groups, no pivot
contract. Not implemented in this card.

---

## 10. Licensing record

| Item | Value |
|---|---|
| Author | Cup Nooble — Sprout Lands |
| Basic / free version | **NON-COMMERCIAL** license per current official author page |
| Premium | commercial use permitted per current official author page |
| Project state | **`LICENSE_FOR_COMMERCIAL_RELEASE = UNRESOLVED`** |
| Credit | required: "Assets - From : Sprout Lands - By : Cup Nooble" |
| Modification | permitted |
| Redistribution of the pack itself | not permitted |

This is a **release / commercialization gate only**. It does not block development.
Nothing was purchased and Premium was not downloaded.

---

## 11. Current author-pack gaps (imported Basic only)

- no accepted standalone tree prefab; no true single-sprite tree
- no tree animation
- no stump asset
- no accepted Y-sort / occlusion foundation
- no water-decoration layer (lily pads have nowhere legal to go yet)
- no semantic farming data model; no four growth stages; none of the 13 author-named crops
  are present in the import
- no character or animal assets imported (`Characters/` folder of the source pack untouched)
- 10 of 25 sheets have stale generated sprite families

---

## 12. Terrain test debt (reported, NOT fixed)

`TerrainRenderTestField.VerifyResolvedTerrainSprite` has exactly two call sites:
- **L931 → `renderAssets.Sand`** — still valid; the Sand path still uses `TerrainSpriteResolver`.
- **L944 → `renderAssets.Grass`** — **STALE.** Production Grass now holds
  `AutoTile_Grass`; asserting `tilemap.GetSprite(...) == TerrainSpriteResolver.TryResolve(...)`
  validates a deprecated path.

Three mechanically reasonable corrections exist, with materially different future value:
(a) assert `grassTilemap.GetTile(pos) == renderAssets.GrassAutoTile`, mirroring production
`VerifyRenderedOutput`; (b) keep comparing AutoTile's resolved sprite against the resolver, as a
migration-equivalence regression check; (c) drop the Grass sprite assertion and rely on the
tile-reference check.

Per §17/§19 this is reported as **`BLOCKED_PM_DECISION`** for the test correction; no test
code was modified. Note `renderAssets.Grass` is also used at L254 in
`VerifyAllTerrainMasks`, which unit-tests the retained `SproutLandsTerrainMaskMap` and is
still legitimate.

Production terrain behaviour was not changed to satisfy any test.
