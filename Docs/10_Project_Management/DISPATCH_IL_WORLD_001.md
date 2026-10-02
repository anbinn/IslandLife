# Dispatch — IL-WORLD-001 First Island Production Scene Skeleton

## Task ID / Title
IL-WORLD-001 — First Island Production Scene Skeleton

## Objective
Create the smallest clean Unity scene foundation for the first island using the project's locked mainstream hybrid 2D workflow.

This task establishes editable production structure only. It does **not** attempt final terrain art, AI generation, gameplay systems, custom editor tooling, or a full island.

## Execution Environment
- Formal local Git repository: `F:\IslandLife\IslandLife`
- Unity project: `F:\IslandLife\IslandLife\Game\Unity\IslandLife`
- Temporary development root: `F:\临时开发区\IslandLife`
- Unity: `6000.3.25f1`
- Existing relevant packages:
  - 2D SpriteShape `13.0.0`
  - 2D Tilemap `1.0.0`
  - 2D Tilemap Extras `6.0.3`
  - 2D Aseprite Importer `3.0.2`
  - URP `17.3.0`

Do not create IslandLife clones/worktrees/temp development copies on C:.

## Repository / Branch
- Repository: `anbinn/IslandLife`
- Branch: `main`

Before editing, sync `main` and confirm the working tree is clean.

## Required References
Read before execution:
- `Docs/00_Governance/PM_GOVERNANCE.md`
- `Docs/00_Governance/DISPATCH_RULES.md`
- `Docs/13_Environment/LOCAL_DEVELOPMENT_ENVIRONMENT.md`
- `Docs/03_Art/ART_DIRECTION.md`
- `Docs/04_Technology/AI_2D_WORLD_GENERATION_RESEARCH.md`
- `Docs/00_Governance/ASSET_GOVERNANCE_RULES.md`

Visual target remains the approved first-island reference: warm detailed 2D tropical island, approximately 45-degree tilted top-down / three-quarter presentation. Exact island geometry is not locked.

## Allowed Scope
Create only:
- `Game/Unity/IslandLife/Assets/Scenes/FirstIsland_Prototype.unity`
- Unity-generated `.meta` files required for that new scene.
- New files/folders under `Game/Unity/IslandLife/Assets/_Project/WorldPrototype/**` only if Unity requires them for this minimal scene structure.

Use existing Unity packages/components. No external asset import is required for this task.

## Forbidden Scope
Do not modify:
- `Game/Unity/IslandLife/Assets/Scenes/SampleScene.unity`
- `Game/Unity/IslandLife/Packages/**`
- `Game/Unity/IslandLife/ProjectSettings/**`
- `Game/Unity/IslandLife/Assets/Settings/**`
- any PM/governance/research document
- any unrelated asset/script/prefab

Also forbidden:
- paid assets or paid tools
- importing Happy Harvest or another complete sample into this project
- new packages
- custom editor tools
- custom validation/test harnesses
- procedural-generation frameworks
- gameplay systems
- refactors/cleanup
- unrelated fixes

If Unity automatically touches unrelated files, revert those unrelated changes before commit.

## Execution Steps
1. Open the project in Unity `6000.3.25f1`.
2. Create a new scene named `FirstIsland_Prototype.unity`; do not overwrite `SampleScene`.
3. Establish this minimal hierarchy:
   - `FirstIsland_Root`
     - `VisualTerrain`
       - `Grid`
         - `Ground`
         - `Paths`
         - `FarmOverlay`
       - `FreeformTerrain`
     - `WorldObjects`
       - `StaticProps`
       - `InteractiveProps`
     - `Gameplay`
       - `Occupancy`
       - `SpawnPoints`
   - `Main Camera`
4. `Grid` must be a Unity Grid object. `Ground`, `Paths`, and `FarmOverlay` must be editable Tilemap children.
5. `FreeformTerrain` is the reserved root for later SpriteShape/freeform coastline, lake, cliff, and path work. Do not build custom terrain art in this task.
6. Configure `Main Camera` as Orthographic and suitable for a 2D scene. Do not redesign the locked visual direction.
7. Save and reopen the scene in the Editor.
8. Check Console for compile/errors.
9. Review Git diff and revert every file outside Allowed Scope.
10. Commit and push.

## Acceptance Criteria
- Project opens in Unity `6000.3.25f1` without new compile errors caused by this task.
- `FirstIsland_Prototype.unity` exists separately from `SampleScene`.
- Required hierarchy exists.
- `Ground`, `Paths`, and `FarmOverlay` are editable Tilemaps under a Grid.
- `FreeformTerrain`, `WorldObjects`, and `Gameplay` are independently editable roots.
- Main Camera is Orthographic.
- No paid/external assets were introduced.
- No packages, ProjectSettings, existing scenes, governance docs, or unrelated files changed.
- Git diff contains only Allowed Scope files.

## Deliverables
Worker completion report must include:
- Commit SHA
- Changed files
- Short diff summary
- Unity Editor open/reopen result
- Console error result
- Any blocker/known issue

## Expected Commit Message
`world: create first island production scene skeleton`

## Completion Status
Return:
`WAITING_PM_ACCEPTANCE`
