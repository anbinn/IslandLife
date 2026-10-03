# IslandLife — PM Handoff & Project Status

Version: T0.2  
Owner: PM  
Last PM refresh: 2026-10-04

> Replacement-PM entry point. Keep this file short enough to read completely, but complete enough that a new PM does not restart architecture, rebuild existing systems, or rely on chat memory.

## 1. Takeover order

Before issuing any new Worker card:

1. Read this file completely.
2. Read the active milestone's owning docs (especially `Docs/05_Assets/` while Asset Foundation is active).
3. Verify the current Git branch/HEAD and inspect the existing implementation relevant to the task.
4. Check current local/Worker report for uncommitted work before assuming Git HEAD is the whole live state.
5. Only then decide whether work is missing.

**Do not reconstruct IslandLife from chat history. Do not implement something merely because this file does not mention its exact class. Git/code/assets are engineering fact; this file is the fast project index.**

## 2. Authority & collaboration

- **Product Owner (user):** product direction, visual/product choices, final hands-on acceptance when needed.
- **PM (ChatGPT):** research, architecture, sequencing, semantic interpretation, task boundaries, independent review, documentation governance.
- **Worker/local coding agent:** executes the PM card inside its boundaries; it is not the architect or product researcher. If more than one materially reasonable implementation exists, stop with `BLOCKED_PM_DECISION`.
- **Source Evidence Wins:** actual source art, author documentation, Git/Unity data and observed runtime behavior override old PM assumptions and stale docs. Correct the record when evidence conflicts.
- PM research happens without interrupting the user. Contact the user only for a real product decision/blocker, final PM result, or at a Worker execution node; at that node send **PM conclusion + complete Worker card together**.
- Do not make the user act as routine permission reviewer, command memorizer, CI/log relay, or architecture intermediary.
- Normal Chat/GitHub PM work does **not** require Work mode. Do not hand PM-owned documentation/research to Worker merely because a tool exists.

## 3. Development method

IslandLife uses **Vertical Slice + Incremental Development + MVP Scope Control**.

Current intended first playable loop (later milestone):

`enter island → move → chop tree → gain wood → craft campfire → night → experience ends`

Foundation work comes first. A foundational milestone must close before content/feature work that depends on it begins. Do not knowingly leave a foundational defect to “fix later” and then build on top of it.

Validation levels are explicit:
- `STATIC_VERIFIED` — Git/source/serialized data proves the claim.
- `UNITY_VISUAL_PENDING` — requires Editor/runtime visual confirmation.
- `UNITY_PASS` — actually observed in Unity.
Static verification must never be reported as Unity PASS. Lack of Worker Unity control should not repeatedly block other static-safe work; batch Editor validation where practical.

## 4. Architecture / skill tree

This is the project map a replacement PM should understand before creating work.

### Foundation
- **Unity:** 6000.3.25f1.
- **Repository:** `anbinn/IslandLife`.
- **Active development branch at this handoff:** `IL-WORLD-003A`; accepted Git baseline before current uncommitted Asset Foundation work: `41c11af74e2e0400f27f11c266ed90f8bf2f98c7`.
- Git is the engineering source of truth. Worker reports may describe newer **uncommitted** local state; verify before overwriting/rebuilding it.

### World / terrain — implemented production path
`FirstIsland_TerrainData.asset → TerrainMapRuntimeLoader → runtime TerrainGridData → TerrainTilemapRenderer → Water/Grass Tilemaps`

Production facts:
- Water: `AnimatedTile_Water`, 4 frames, animation verified visually.
- Grass: `AutoTile_Grass`, edge behavior verified visually.
- FirstIsland terrain baseline: Water 1825, Grass 1100, Sand 0, origin (-32,-22), size 65×45.
- Fake Beach remains disabled. GroundDetails/random decoration remains inactive.
- Legacy Sand/Beach aliases point at tilled-dirt art and are **not** real Sand/Beach.
- Old custom Grass resolver path is no longer the production rendering path; tests must follow the AutoTile production fact.
- Do not create a second terrain pipeline unless Git proves the production path was intentionally replaced.

### Asset system — active milestone
**Milestone: Sprout Lands Asset Foundation.**

Permanent rule:
- Terrain/grid art is managed as terrain/tile data.
- Complete world things are managed as complete objects even if their source occupies multiple 16×16 cells.
- **16×16 is the base grid, not a rule that every object must be cut into 16×16 project-facing pieces.**
- User-facing Unity assets should answer “what is this?” (`Tree_01`, `Bush_01`, etc.), not require knowledge of `rNcN` source coordinates.

New asset intake order is fixed:

`source art → slicing/frame audit → semantic identity → naming/category → complete project-facing object → Unity validation → allowed into map/content`

No random scene placement before semantic/surface rules are known. Items, seed packs and tools are not natural ground scatter. UNKNOWN assets stay out of scene placement.

Current Sprout Lands Basic scope only. Sorry Pack, UI Pack and external candidate packs are postponed; do not import them during this milestone.

### Project-facing object contract
For multi-renderer objects such as trees/bushes:
- one logical root object;
- root represents ground/base contact;
- internal SpriteRenderers may reconstruct source art;
- `SortingGroup` makes a multi-renderer object sort as one logical unit;
- raw source slices remain implementation detail;
- global Y-sort/occlusion gameplay is a later system, not to be invented during asset cataloging.

### Future skill branches — not current work
After Asset Foundation closes, later branches include map composition/content, player/world sorting & occlusion, interaction/resource objects, chopping/wood, crafting/campfire, time/night and the vertical-slice finish. Do not skip foundation gates to start these early.

## 5. Accepted history that must not be rebuilt

- **IL-WORLD-001 — First Island Production Scene Skeleton:** Accepted. Commit `d98cf630d76a2074ef8336154b39393d9938228a`.
- **IL-WORLD-002 — Sprout Lands Terrain Pipeline Validation:** Accepted technical experiment. Final implementation `33d29c7b64e2cde71536dfad70cfb391f0673981`; historical merge on main `66fe06091979d3c6af14ce73b1bdc807582d06ea`. It validated Sprout Lands import, Grass auto-tiling, animated water, Tilemap terrain and independent props; it was not the final island visual target.
- **IL-WORLD-003A / terrain production migration:** production runtime terrain now uses the data-driven path described above. Do not redo the old experimental pipeline.
- Water animation and Grass boundary have been visually accepted in FirstIsland. The scene was deliberately reduced back to Water + Grass while asset semantics are fixed.

Historical task-by-task commit archaeology belongs in Git/history, not this handoff. Record only facts that prevent duplicated work or wrong architecture.

## 6. Current Asset Foundation state

The asset audit established that Sprout Lands sheets use mixed author slicing; non-16×16 rectangles are **not automatically wrong**. `Basic_Grass_Biom_things.png` is the main mixed multi-cell object sheet.

A later correction proved its generated row convention is **r0 = TOP**, not bottom. Earlier semantic mappings based on inverted rows are superseded. Current corrected Tree/Bush project-facing static prefabs reported by Worker:
- `Tree_01`, `Tree_02`, `Tree_03`
- `Bush_01`, `Bush_02`, `Bush_03`

They are **STATIC_VERIFIED / UNITY_VISUAL_PENDING**, not final Unity PASS. Rock semantics are being corrected from source evidence rather than forced into the obsolete “3 small + 1 large” assumption. Chicken House source identity is confirmed, but its project-facing prefab is pending safe Unity Sprite import/reference.

Authoritative detailed semantic/catalog data belongs in:
- `Docs/05_Assets/SPROUT_LANDS_ASSET_SEMANTICS.md`
- `Docs/05_Assets/ASSET_TRACKING.md`

Do not duplicate their cell-by-cell tables here.

## 7. Current live/uncommitted work warning

At this handoff, Worker has been operating from `IL-WORLD-003A` with uncommitted Asset Foundation changes. Known carried work includes:
- Grass production wiring to `AutoTile_Grass`;
- Water remains `AnimatedTile_Water`;
- fake Beach disabled;
- GroundDetails inactive;
- Asset Foundation semantic docs;
- project-facing Tree/Bush prefab work;
- stale Grass terrain test correction;
- pre-existing dirty `ProjectSettings/ShaderGraphSettings.asset` that must not be casually reverted/staged.

**This list is a warning/index, not a substitute for `git status` and diff.** Before issuing or accepting the next card, read the newest Worker report and verify the actual branch/worktree.

## 8. Asset licensing / external candidates

Sprout Lands Basic is being used for development. Commercial-release licensing remains a release gate to resolve before commercialization; do not let a future release condition block present foundation work.

Tracked candidates (not adopted/imported) live in `Docs/05_Assets/ASSET_TRACKING.md`, including AxulArt, Little Dreamyland and Shining Fields. A candidate is not a project dependency until PM explicitly adopts it.

## 9. PM preflight before every new implementation card

Ask, in this order:

1. **Does this already exist?** Check this status map, owning docs and Git/code/assets.
2. **Is the current implementation production, legacy, experiment, or pending?**
3. **What source evidence defines the correct behavior/asset semantics?**
4. **Which milestone owns the change, and is its prerequisite closed?**
5. **Can PM decide the architecture now?** If yes, decide it before Worker execution.
6. **What must be static-verified vs Unity/user-validated?**

Only then issue the card. Worker must never be used to rediscover architecture that PM could have established from existing project evidence.

## 10. Documentation rule

Durable PM documentation records:
- current architecture and project skill tree;
- accepted baselines that must not be rebuilt;
- active milestone and gates;
- important rejected/legacy paths that could mislead a future PM;
- durable collaboration/governance rules;
- unresolved risks with clear reopen conditions.

Do **not** turn this file into a chronological diary, raw asset catalog, or class-by-class encyclopedia. One sentence is preferred when one sentence preserves the meaning. Detail belongs in the owning technical document or Git history.

## 11. Resume point

Current priority is **finish Sprout Lands Asset Foundation completely**, including corrected semantics/project-facing objects and consolidated Unity visual validation, before returning to island decoration/layout or later gameplay.

A replacement PM should first verify the newest Worker result against the corrected source evidence and current Git state, then continue/close this milestone. Do not restart terrain work, do not re-enable fake Beach, and do not randomize decorations.
