# IslandLife Asset Tracking

Version: T0.1
Owner: PM

## Asset Record Requirements

Each external or reusable asset should record:

- Name
- Source
- License
- Modification permission
- Usage location
- Style compatibility review

## Rule

Unrecorded assets cannot enter the production build.

## Sprout Lands Asset Foundation status

| Gate | Status |
|---|---|
| STATIC SOURCE AUDIT | **PASS, CORRECTED** |
| SEMANTIC MAPPING | **PASS** — full sheet re-audit complete |
| PROJECT-FACING TREE/BUSH | **STATIC_VERIFIED** |
| PROJECT-FACING ROCKS | **STATIC_VERIFIED** |
| CHICKEN HOUSE PREFAB | **PENDING_UNITY_IMPORT** |
| UNITY VISUAL VALIDATION | **PENDING_USER/EDITOR** |
| MILESTONE | **NOT COMPLETE** |

Milestone SPROUT LANDS ASSET FOUNDATION is **NOT COMPLETE**.

**Row-orientation correction (authoritative):** `rN` = image row from the **TOP**
(`m_Rect.y = sheetHeight - (N + 1) * 16`). Verified per sheet from the generated Sprite
assets: `Basic_Grass_Biom_things` `r0=64…r4=0` · `Basic_Plants` `r0=16…r1=0` ·
`Grass` `r0=96…r6=0` · `Hills` `r0=128…r8=0` · `Tilled_Dirt*` `r0=96…r6=0`.
The convention is uniform across every sheet. The earlier audit that assumed `r0` = bottom
is **superseded**; all of its `rNcN` indices were re-derived from source pixels.
Coverage: **45/45** `Basic_Grass_Biom_things` cells, **12/12** `Basic_Plants` cells.

Project-facing prefabs under `Assets/Art/Prefabs/SproutLands/`:

| Prefab | Source slices | Renderers | SortingGroup |
|---|---|---|---|
| `Trees/Tree_01.prefab` | `r0c0` + `r1c0` | 2 | YES |
| `Trees/Tree_02.prefab` | `r0c1` `r0c2` + `r1c1` `r1c2` | 4 | YES |
| `Trees/Tree_03.prefab` | `r0c3` `r0c4` + `r1c3` `r1c4` | 4 | YES |
| `Bushes/Bush_01.prefab` | `r3c0` + `r3c1` | 2 | YES |
| `Bushes/Bush_02.prefab` | `r4c0` + `r4c1` | 2 | YES |
| `Bushes/Bush_03.prefab` | `r4c2` + `r4c3` + `r4c4` | 3 | YES |
| `Rocks/Rock_01.prefab` | `r1c7` | 1 | no (single renderer) |
| `Rocks/Rock_02.prefab` | `r1c8` | 1 | no (single renderer) |
| `Rocks/Rock_03.prefab` | `r4c5` | 1 | no (single renderer) |
| `Rocks/Rock_04.prefab` | `r4c6` | 1 | no (single renderer) |
| `Rocks/Rock_Mossy_01.prefab` | `r4c7` | 1 | no (single renderer) |
| `Rocks/Rock_Mossy_02.prefab` | `r4c8` | 1 | no (single renderer) |

Root `Transform` = ground-contact anchor in all 12. `SortingGroup` serial format
(classID 210) was derived from a real Unity 6 asset in this project
(`Library/PackageCache/com.unity.2d.spriteshape@*/Samples~/Samples/3 Platformer.unity`),
not guessed. Static verification of all 12 prefabs: **0 failures** — correct source GUID,
`m_Sprite` fileID `21300000`, exact positions, renderer counts, root at `(0,0)`, no
duplicate fileIDs, no unresolved refs, uniform CRLF, pivot `0.5/0.5`, PPU 16 unchanged.

**Semantic corrections carried forward:**

- `LILY_PAD_IMPORTED = NOT_FOUND`. The old `r0c7`/`r0c8` lily-pad claim is false — those
  cells are purple mushrooms (`Mushroom_03/04`). The WATER-only rule is retired with the
  asset; no replacement was manufactured.
- `Basic_tools_and_materials.png` has **0 generated slices**. The old
  `Tool_WateringCan`/`Tool_Axe`/`Tool_Pickaxe`/`Material_Stone`/`Material_Wood` `rNcN`
  references pointed at non-existent assets and are withdrawn.
- `ChickenHouse_01` — `SEMANTIC = CONFIRMED`, `SOURCE_ART = CONFIRMED`,
  `PROJECT_PREFAB = PENDING_UNITY_IMPORT`, `MILESTONE_BLOCKING = NO`. No Sprite
  GUID/fileID was fabricated.
- Authoritative single mapping: `SPROUT_LANDS_ASSET_SEMANTICS.md` §2. No superseded
  inverted-row table remains in that document.

**Release gate unchanged:** `LICENSE_FOR_COMMERCIAL_RELEASE = UNRESOLVED`.

## Candidates (recorded, NOT adopted)

Status for every entry below: **CANDIDATE — NOT ADOPTED**.
Nothing has been downloaded, imported, or evaluated in-engine. Do not change the current
art style based on these entries.

| Candidate | Possible relevance | License / source | Style compatibility | Status |
|---|---|---|---|---|
| AxulArt | 8-direction character movement | UNKNOWN — not yet reviewed | UNKNOWN | CANDIDATE, NOT ADOPTED |
| Small 8-direction Characters | Character sprites for player/NPC | UNKNOWN — not yet reviewed | UNKNOWN | CANDIDATE, NOT ADOPTED |
| Little Dreamyland | Environment / props | UNKNOWN — not yet reviewed | UNKNOWN | CANDIDATE, NOT ADOPTED |
| Gayapon — Shining Fields, Bright Forest | `stone path`, `wood plank path` (fills the missing non-farm path gap; Sprout Lands only has wooden plank paths) | UNKNOWN — not yet reviewed | UNKNOWN | CANDIDATE, NOT ADOPTED |

Before any candidate can move out of this table, it needs the full record above:
license, modification permission, and an explicit art-style review against
Cup Nooble's Sprout Lands palette.

## Licensing gate — IslandLife

| Item | Value |
|---|---|
| Asset | Sprout Lands Basic (Free version) |
| Author | Cup Nooble |
| License | **NON-COMMERCIAL** per current official author page |
| Premium variant | commercial use permitted per current official author page |
| `LICENSE_FOR_COMMERCIAL_RELEASE` | **UNRESOLVED** |
| Credit | required — "Assets - From : Sprout Lands - By : Cup Nooble" |
| Modification | permitted |
| Pack redistribution | not permitted |

This is a **release / commercialization gate only**. It does not block development.
Nothing has been purchased and Premium has not been downloaded. Must be resolved before
any commercial or public release.
