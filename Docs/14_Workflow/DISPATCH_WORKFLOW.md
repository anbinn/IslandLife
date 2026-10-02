# Dispatch Workflow

## Purpose
Define how PM assigns work to AI workers.

## Flow

PM checks current governance/environment → PM creates Dispatch → Worker executes exact scope → Commit → Push → PM reviews GitHub diff → Acceptance.

## Dispatch Requirements

Every work card must follow `Docs/00_Governance/DISPATCH_RULES.md` and include:

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

## Pre-Dispatch Check

Before issuing a work card, PM must verify that workspace paths, branch, tool versions, and storage roots are current.

Do not copy environment values blindly from a previous Dispatch.

Canonical references:

- `Docs/00_Governance/PM_GOVERNANCE.md`
- `Docs/13_Environment/LOCAL_DEVELOPMENT_ENVIRONMENT.md`
- `Docs/13_Environment/TOOLCHAIN_REQUIREMENTS.md`

## Scope Control

Worker must not expand scope without PM approval.

Unrequested test frameworks, batch/headless validation, helper tooling, refactors, automation, cleanup, and unrelated fixes are forbidden unless explicitly listed in Allowed Scope.

When the card specifies a direct/manual acceptance path, use it. Do not invent a more elaborate validation path.

A non-core/out-of-scope issue that takes more than 10 minutes must be stopped and reported instead of investigated indefinitely.

## Review

Worker self-report is not acceptance.

PM reviews the pushed GitHub commit/diff and checks the Dispatch acceptance criteria before accepting the task.
