# Local Development Environment

## Purpose

PM and developer must maintain a reproducible local environment. The local environment is required for code review, build verification, testing, and final acceptance.

## Roles

### Worker Environment
- Used for implementation.
- Changes must be pushed to GitHub.
- GitHub commit history is the source of truth.

### PM Review Environment
- Used for pulling the latest GitHub state.
- Used for reviewing Diff, building, running tests, and acceptance verification.
- PM local environment must be able to reproduce the project state.

## Required Local Setup

- Git
- Project engine and matching version
- Required SDK/toolchain
- IDE/editor
- Build tools
- Test tools

## Workflow

1. Worker creates changes.
2. Worker commits and pushes to GitHub.
3. PM reviews GitHub changes.
4. PM pulls approved changes locally.
5. PM runs build and verification.
6. Acceptance result is recorded.

## Rule

No local-only modification is considered accepted. Every important change must exist in GitHub history before review.
