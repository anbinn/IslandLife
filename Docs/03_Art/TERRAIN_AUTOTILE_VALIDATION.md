# Terrain Auto Tile Validation

Version: EXP-0.2
Owner: Worker
Status: PARTIAL - generator side verified, Editor side not yet exercised
Related dispatch: `Docs/14_Workflow/Dispatches/IL-ART-001_FANG_AUTOTILE_VALIDATION.md`

## Result Summary

| Area | Result |
| --- | --- |
| Package resolve on Unity 6000.3.25f1 | PASS |
| Package compile (runtime + editor asmdef) | PASS |
| 5-pattern source sheet accepted by Fang | PASS |
| 47 adjacency combinations generated | PASS |
| Multi-frame (Animation, 2 frames) generation | PASS |
| Tile Palette painting / edge / corner auto selection | NOT VERIFIED |
| Erase + repaint neighbour refresh | NOT VERIFIED |
| Animation preview in play mode | NOT VERIFIED |
| Screenshot evidence of a painted Tilemap | NOT AVAILABLE |

Overall: **PARTIAL**. No Fang defect was found. The unfinished items were not
reached because the Worker's Unity access is headless batchmode only, and the
dispatch requires interactive Editor steps.

## Unity / Package Result

Unity: `6000.3.25f1` (`F:\Unity\Editors\6000.3.25f1`).

Pinned package, unchanged by this task:

`com.ruccho.fang-auto-tile` -> `https://github.com/ruccho/FangAutoTile.git?path=/Packages/com.ruccho.fang-auto-tile#1d322cbb191b4613d251a37ad0ee3f5a6ddc2c07`

Observations:

- `Packages/manifest.json` was not modified by this task.
- `Packages/packages-lock.json` gained the `com.ruccho.fang-auto-tile` entry
  after Package Manager resolved the pinned revision. This is the only
  intentional package change and is committed with this validation.
- `Library/PackageCache/com.ruccho.fang-auto-tile@fe77dfa7ea54` was populated.
- Both assemblies compiled with no `error CS` output:
  `Library/ScriptAssemblies/FangAutoTile.dll`,
  `Library/ScriptAssemblies/FangAutoTile.Editor.dll`.
- No package resolution error, no compile error, no console exception related
  to the package.

## Test Performed

Prototype location:

- `Assets/_Project/Art/Terrain/Prototype/IL-ART-001_kusa.png` - 16x80 source
  sheet, 1 frame, 5 patterns, `Compression = None`, `Enable Padding = on`.
- `Assets/_Project/Art/Terrain/Prototype/IL-ART-001_mizu.png` - 32x80 source
  sheet, 2 horizontal frames, 5 patterns, same settings.

The 5 source patterns, top to bottom in the sheet, follow the Fang Basic
sample geometry: isolated tile, vertical connection, horizontal connection,
center tile, fully filled tile. The test art is procedurally generated
placeholder color blocks. Fang's own sample art was **not** copied, because the
`MapChip_pipo` sample images are licensed by pipoya.net under separate terms
and are not appropriate to commit to this repository.

Generated tiles:

- `Assets/_Project/Art/Terrain/Prototype/IL-ART-001_Kusa.asset` - Frame mode
  Random, 1 frame.
- `Assets/_Project/Art/Terrain/Prototype/IL-ART-001_Mizu.asset` - Frame mode
  Animation, 2 frames.

Fang's own generator was used. No Fang source was patched.

Measured results:

| Check | Value |
| --- | --- |
| `IL-ART-001_Kusa.asset` combinations | 47 |
| `IL-ART-001_Kusa.asset` sprites | 47, all distinct references |
| `IL-ART-001_Mizu.asset` combinations | 47 |
| `IL-ART-001_Mizu.asset` sprites | 94 (47 x 2 frames) |
| `combinationTable` entries written | yes, values 0-46 |

Both source sheets were accepted by `CheckValidity` without error, which
confirms the documented source format contract:

- width = tile size x frame count;
- height = tile size x 5;
- `Compression = None`;
- `Enable Padding` enabled.

## Known Issues

### 1. Interactive Editor steps could not be executed

The dispatch requires Tile Palette painting, erase/repaint refresh checking,
animation preview and a screenshot. The Worker environment can only run Unity
in `-batchmode`; there is no interactive Editor session available to drive the
Tile Palette or the Scene view. These steps are therefore unverified, not
failed.

### 2. Headless Tilemap painting limitation (recorded, not fixed)

A temporary batchmode editor helper was written to paint an irregular shape
programmatically instead of using the Tile Palette. It was discarded per PM
instruction and is not part of this commit. For the record, the limitation
observed was:

- A single `Tilemap.SetTile` call on a freshly created runtime Tilemap
  succeeded.
- A loop of `SetTile` calls over a 42 cell irregular shape left the Tilemap
  empty (`GetUsedTilesCount() == 0`, `GetTile()` returned null), so adjacency
  and refresh assertions could not be evaluated headlessly.
- Explicitly setting `tilemap.origin` / `tilemap.size` did not change the
  behaviour.
- No exception was reported by the Fang tile itself: `RefreshTile` and
  `GetTileData` both executed and `GetTileData` returned a valid sprite for an
  isolated cell.

This is a limitation of the headless harness, not evidence of a Fang defect.
It was left unfixed and the helper script was removed.

### 3. Prototype art is placeholder only

The source sheets are solid color blocks. They prove the format contract and
the generator, not visual quality. Padding seam behaviour (acceptance item 6)
could not be judged on placeholder art and remains unverified.

## Changed Files

- `Game/Unity/IslandLife/Packages/packages-lock.json` - Fang entry added by
  Package Manager.
- `Game/Unity/IslandLife/Assets/_Project/Art/Terrain/Prototype/*` - source
  sheets and generated tile assets with meta files.
- `Game/Unity/IslandLife/Assets/**/*.meta` - meta files Unity generated for the
  folders and scripts that were committed in earlier dispatches.
- `Game/Unity/IslandLife/ProjectSettings/SceneTemplateSettings.json` - created
  by Unity 6000.3 on first project open.
- `Docs/03_Art/TERRAIN_AUTOTILE_VALIDATION.md` - this document.

No gameplay code, no `Docs` governance document, no Unity version change and
no first-island production content was touched. `ShaderGraphSettings.asset`
was reverted after Unity rewrote it on open, to keep the diff limited.

## Screenshot

None. The only image produced by the headless attempt was rendered from an
empty Tilemap and was deleted rather than submitted as misleading evidence.

## Recommendation

Generator side: **Fang Auto Tile is compatible with Unity 6000.3.25f1 and
delivers the promised efficiency.** One 5-pattern sheet produced all 47
adjacency variants with no manual cutting, and the 2-frame animation sheet
produced 94 sprites. That is the core claim of the workflow and it holds.

Runtime side: **still open.** Edge/corner selection while painting, neighbour
refresh on erase, and animation playback are exactly the parts that decide
whether designers can actually work in the Tile Palette without manual repair,
and none of them have been observed yet.

Suggested next step for PM, on a machine with an interactive Editor:

1. Open `Game/Unity/IslandLife` with Unity `6000.3.25f1`.
2. Open `Assets/_Project/Art/Terrain/Prototype/IL-ART-001_Kusa.asset` and drag
   it into a Tile Palette.
3. Paint an irregular area with straight edges, outer corners, inner corners
   and one-tile protrusions.
4. Erase and repaint a few cells and watch the neighbours.
5. Repeat with `IL-ART-001_Mizu.asset` and enter play mode to see the two
   animation frames.
6. Capture screenshots.

Decision guidance:

- If painting behaves correctly, continue with Fang for IslandLife terrain.
- If padding seams are visible at gameplay zoom, evaluate the fallback
  `com.unity.2d.tilemap.extras` AutoTile, which is already installed, before
  committing to Fang.
