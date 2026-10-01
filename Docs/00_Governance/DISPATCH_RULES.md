# Dispatch Rules

## Purpose

Dispatch is the only execution instruction format between PM and Worker.

## Required Fields

- Title
- Objective
- Execution Environment
- Repository
- Allowed Scope
- Forbidden Scope
- Acceptance Criteria
- Deliverables

## Worker Rules

Worker executes the assigned scope only.

Worker must not redefine product direction, architecture boundaries, or governance rules.

## Completion

Worker returns:

- Commit SHA
- Changed files
- Diff summary
- Test result

Status after completion:

WAITING_PM_ACCEPTANCE
