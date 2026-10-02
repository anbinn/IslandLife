# Local Development Environment

## Purpose
Define the current local environment required for PM acceptance, testing, and development verification.

## Principles
- PM local environment must be able to reproduce Worker results.
- GitHub is the source of truth.
- Local builds are for verification, not the primary development source.
- Dispatches must use the current paths in this document; do not reuse stale paths from older work cards.

## Current IslandLife Workspace

- Repository: `anbinn/IslandLife`
- Formal local Git repository: `F:\\IslandLife\\IslandLife`
- Unity project: `F:\\IslandLife\\IslandLife\\Game\\Unity\\IslandLife`
- Non-Git project/work area: `F:\\IslandLife\\Projects`
- Temporary development root: `F:\\临时开发区\\IslandLife`
- Locked Unity version: `6000.3.25f1` (Unity 6.3 LTS)

IslandLife temporary clones, worktrees, verification/audit/probe copies, test workspaces, and build-isolation copies must stay under `F:\\临时开发区\\IslandLife`.

Do not create IslandLife repository/worktree/development copies under C: temporary locations.

Normal Windows/system development tools may remain on C:. This rule does not require global TEMP/TMP relocation.

## Required Setup
- Git client
- GitHub access
- Unity `6000.3.25f1`
- Required project packages/SDK/toolchain
- Build and debug tools required by the assigned Dispatch

## Verification Flow
GitHub Commit → Local Sync → Build/Test using the Dispatch-specified method → PM Acceptance

## Maintenance Rule

When the Product Owner changes a formal workspace, temporary root, Unity version, or other execution-critical environment value, PM must update this document promptly. Future Dispatches must reference the updated value.
