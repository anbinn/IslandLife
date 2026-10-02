# IslandLife Project Status Tracking

Version: T0.1
Owner: PM

## Purpose

Define how project progress, risks, decisions, and milestones are tracked.

## Status Categories

- Planned
- In Progress
- Review
- Accepted
- Blocked

## Tracking Rules

- Every major task must have a clear owner.
- Every completed task must reference GitHub changes.
- Important decisions must be recorded in documents.
- Blocked issues must be identified early.

## PM Review Points

PM reviews:

- Scope completion.
- Code changes.
- Asset changes.
- Performance impact.
- Alignment with product direction.


## Current Execution Status

### IL-WORLD-001 — First Island Production Scene Skeleton
- Status: Accepted
- Implementation commit: `d98cf630d76a2074ef8336154b39393d9938228a`
- PM verification: GitHub diff reviewed; two Allowed Scope files only.
- Result: first-island editable scene skeleton established.

### IL-WORLD-002 — Sprout Lands Terrain Pipeline Validation
- Status: Accepted
- Final implementation commit: `33d29c7b64e2cde71536dfad70cfb391f0673981`
- Merge commit on `main`: `66fe06091979d3c6af14ce73b1bdc807582d06ea`
- PM verification: GitHub diff and Unity Editor result reviewed.
- Result: Sprout Lands import, sprite slicing, Grass RuleTile auto-tiling, 4-frame Animated Tile water, Tilemap terrain, and independent prop placement were validated.
- Scope note: this was a technical pipeline experiment, not the approved final first-island visual target.

### Current Environment Gate
- Formal workspace synchronized to accepted `main`.
- Formal Unity project opened successfully with Unity `6000.3.25f1`.
- Unity MCP `v10.2.0` is running through HTTP Local at `127.0.0.1:8080`.
- Kilo → Unity MCP read-only connectivity verified against `Assets/Scenes/FirstIsland_Prototype.unity`.


### IL-WORLD-003A — First Island Grass Silhouette / Autotile Correction
- Status: In Progress / NOT ACCEPTED
- Branch: `IL-WORLD-003A`
- Initial silhouette commit: `ec6d122122c25ddd68c2a4acb26c95b9688950a7`
- Failed corrective commit: `55ec3bb65c2abc2cc6be3e5f364cd624cd9515c7`
- PM verification: the irregular island silhouette direction is retained, but the Grass terrain rendering is not accepted. Thick internal seams/gaps remain on irregular boundaries.
- Root-cause finding: the production `Grass.png` importer was based on irregular transparency-derived sprite rectangles, while the original Sprout Lands source is a 176×112 texture on a 16×16 grid (11 columns × 7 rows = 77 cells). The reduced hand-authored 9-rule Grass RuleTile is not an accepted production solution for irregular terrain.
- Asset authority: `F:\IslandLife\Projects\SproutLands_Basic_PM_Reviewed.zip` is the reviewed original Sprout Lands Basic source. Its `Grass.png` and supplied bitmask references are authoritative asset inputs. Do not substitute another downloaded template.
- Technical direction: correct Grass to grid-aligned 16×16 sprites first; validate that change independently; only then implement the selected Unity Tilemap Extras 3×3 AutoTile terrain step. Do not continue patching the 9-rule RuleTile.
- R1 research attempt: stopped and not accepted. The attempt drifted into mapping inference, GIF decoding, custom analysis scripts, and other research work that belongs to PM. R1 files are not authoritative production inputs and must not be continued as a mapping source.
- Worker boundary: PM decides the technical approach, authoritative inputs, mappings/values, and acceptance criteria. Worker executes the specified solution only. If information is insufficient or more than one plausible implementation exists, Worker must return `BLOCKED_PM_DECISION` rather than research, guess, invent a workaround, install/configure tools, or change architecture.
- PM dispatch rule: do not issue pseudo-execution cards containing unresolved research/design questions. Solve those decisions at PM level before dispatch.
- PM acceptance rule: Worker reports are not acceptance. PM independently verifies the pushed GitHub branch/SHA, full diff, allowed/forbidden scope, implementation details, and Unity visual/runtime result when applicable.
- Git synchronization rule: each Worker card is one small independently verifiable closed loop. After completing that loop, Worker must commit and push immediately, then stop at `WAITING_PM_ACCEPTANCE`. Do not accumulate multiple completed stages as unpushed local work. GitHub is the formal progress checkpoint.
- Blocker rule: half-finished or ambiguous work is not committed as a formal result unless the card explicitly defines a valid independently verifiable partial deliverable. On a blocker, report exact local diff/status and return `BLOCKED_PM_DECISION`.
- Session rule: prefer a fresh Worker session for each new small execution card when prior context has become large or contains obsolete Todos. Project truth comes from GitHub plus the current PM card, not from inherited chat Todos.
- Accepted slice: `IL-WORLD-003A-GRASS-SLICE-01` — PM-accepted at commit `8b9fac2505f07a7be3858c08c8ea948608166d08`. GitHub verification confirmed the production Grass importer now contains 77 grid-aligned 16×16 sprites (`Grass_r0c0` through `Grass_r6c10`), PPU 16, Point filtering, Compression None, and no scope expansion.
- Environment blocker before AutoTile: the abandoned R1 validation workspace remains untracked under `Assets/_Project/Validation/GrassAutotile/`; `GifDecoder.cs` currently produces 11 pre-existing C# compile errors. These files are explicitly non-authoritative and must be removed as a separate cleanup slice before AutoTile execution.

### Next Gate
Run `IL-WORLD-003A-R1-CLEANUP-01`: remove only the abandoned untracked R1 validation artifacts under `Assets/_Project/Validation/GrassAutotile/` and verify the R1-caused compile errors disappear. Do not touch production terrain assets or unrelated dirty environment files. After PM acceptance of that cleanup, proceed to the separately specified AutoTile slice.
