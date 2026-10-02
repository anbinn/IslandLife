# IslandLife PM Governance

Version: T0.1
Owner: PM

## 1. Purpose

This document defines the project governance rules for IslandLife. It is maintained by PM and is the source of truth for development workflow, review rules, and collaboration boundaries.

## 2. Roles

### PM

Responsibilities:
- Define product direction and development standards.
- Create Dispatch work cards for Workers.
- Review every meaningful code/content change through GitHub history and diff.
- Maintain project documents and rules.
- Reject changes that violate architecture, style, or product direction.

### Worker (Kiro Agent)

Responsibilities:
- Execute assigned Dispatch tasks.
- Modify only the required scope.
- Commit changes with clear messages.
- Push changes to GitHub.
- Report completed work and waiting status.

Worker does not modify PM governance documents unless explicitly assigned.

## 3. Source of Truth

GitHub repository is the project source of truth.

Rules:
- Development changes must exist in Git history.
- Local-only modifications are not considered accepted.
- PM reviews commits and diffs before local acceptance.

## 4. Development Flow

Standard flow:

PM Dispatch
→ Worker execution
→ Git commit
→ Push to GitHub
→ PM review diff
→ Acceptance
→ Local synchronization and testing

## 5. Dispatch Rules

Every task should contain:

- Task ID
- Objective
- Scope
- Restrictions
- Acceptance criteria
- Required commit message

Small iterations are preferred over large uncontrolled changes.

## 6. Change Rules

Workers must:

- Avoid unrelated file modifications.
- Avoid changing architecture without approval.
- Explain changed files after completion.
- Preserve established visual and technical standards.

## 7. Review Rules

PM checks:

- Changed files
- Diff content
- Commit message
- Architecture impact
- Regression risk

A task is not complete only because a Worker reports completion. GitHub state is the verification source.

## 8. Project Principle

IslandLife follows a stable foundation approach:

- Establish visual direction before large-scale production.
- Reuse suitable existing resources where licensing permits.
- Optimize performance from the beginning.
- Prefer controlled iteration over uncontrolled expansion.

## 9. Communication Efficiency Principle

Project communication follows a high-efficiency approach:

- Prioritize key information over unnecessary explanation.
- Do not repeat confirmed decisions unless there is a change.
- Report blockers, decisions, and required actions directly.
- Avoid spending time on options that already violate project criteria.
- PM responses and task instructions should focus on actionable information.

The goal is to maximize execution speed while maintaining clear decision records and review quality.


## 10. Local Workspace and Storage Roots

IslandLife development must keep project and temporary development data off the Windows system drive.

- **Formal local project root:** `F:\\IslandLife\\IslandLife`.
- PM Dispatches and Worker instructions must treat `F:\\IslandLife\\IslandLife` as the canonical local IslandLife workspace unless the Product Owner explicitly changes it.
- **Temporary development root:** `F:\\临时开发区\\IslandLife`.
- Any IslandLife temporary clone, Git worktree, verification/audit/probe copy, test workspace, build-isolation workspace, or other substantial development copy created on the user's Windows machine must live under `F:\\临时开发区\\IslandLife`.
- **Forbidden:** creating IslandLife repository/worktree/development copies under `%TEMP%`, `%TMP%`, `%LOCALAPPDATA%\\Temp`, or any other C: location. Tool defaults do not override this rule.
- GitHub-hosted runner temporary storage is remote runner storage and is not the user's C: drive, so hosted CI scratch paths are unaffected.

This is a persistent PM/Worker rule. The user must not need to repeat the F: storage requirement in future Dispatches.
