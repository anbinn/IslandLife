# Dispatch Rules

## Purpose

Dispatch is the only execution instruction format between PM and Worker.

## Required Fields

Every Dispatch must use the project work-card structure and contain all of the following:

- Task ID / Title
- Objective
- Execution Environment
- Repository / Branch
- Allowed Scope
- Forbidden Scope
- Execution Steps
- Acceptance Criteria
- Deliverables
- Expected Commit Message
- Completion Status

A Dispatch is not considered ready if any required field is missing.

## Environment Freshness Rule

Before writing every new Dispatch, PM must re-check the current canonical project/environment documents instead of copying paths or versions from an older work card.

At minimum, confirm:

- Repository
- Target branch
- Formal local workspace
- Unity project path when applicable
- Temporary development root
- Locked Unity/tool/package versions relevant to the task

Current canonical environment values are maintained in:

- `Docs/00_Governance/PM_GOVERNANCE.md`
- `Docs/13_Environment/LOCAL_DEVELOPMENT_ENVIRONMENT.md`
- `Docs/13_Environment/TOOLCHAIN_REQUIREMENTS.md`

If the project workspace, branch strategy, toolchain, or storage policy changes, update the canonical document first and use the new value in subsequent Dispatches.

## Worker Rules

Worker executes the assigned scope only.

Worker must not redefine product direction, architecture boundaries, or governance rules.

Worker must not add unrequested:

- validation frameworks
- headless/batch test harnesses
- helper systems or editor tooling
- refactors
- automation infrastructure
- cleanup or "while here" improvements

If the Dispatch specifies an acceptance method, use that method. Do not replace a simple Editor/manual validation with a more elaborate automated validation unless the Dispatch explicitly requests it.

Out-of-scope findings must be recorded and reported, not fixed.

If an out-of-scope or non-core issue consumes more than 10 minutes without resolution, stop that direction and report it to PM. PM decides whether it becomes a separate Dispatch.

The goal is the shortest controlled path to the requested result, not extra engineering completeness.

## Completion

Worker returns:

- Commit SHA
- Changed files
- Diff summary
- Test result
- Blockers / known issues, if any

Status after completion:

WAITING_PM_ACCEPTANCE
