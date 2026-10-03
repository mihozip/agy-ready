# COLLAB Workflow

## Purpose

COLLAB is the default workflow for meaningful modifications to an existing system.

Use it when the task is too substantial for SOLO but does not require the full DEEP process.

Typical cases:

- adding functionality to an existing feature
- changing existing GAS workflows
- changing existing PHP application behavior
- modifying validation
- changing import/export behavior
- modifying permissions
- adding fields that interact with existing records
- multi-file changes
- workflow changes involving approximately 3 or more relevant files

The objective is:

```text
understand current behavior
→ design the smallest safe change
→ implement
→ verify
```

## Standard Workflow

```text
1. Understand the user request
2. Inspect repository state
3. Perform cheap local discovery
4. Identify the relevant workflow and files
5. Run AGY Current-State Checkpoint
6. Verify important AGY findings
7. Define desired behavior
8. Design minimal change
9. Implement with Codex
10. Run focused tests
11. Inspect git diff
12. Decide whether independent review is justified
13. If needed, run AGY Review Checkpoint
14. Verify review findings
15. Fix confirmed issues
16. Run final tests
17. Summarize the result
```

## Current-State Checkpoint

Read:

```text
agent/AGY_CURRENT_STATE.md
```

Use AGY to investigate only the bounded scope.

AGY should answer:

- Where does the workflow begin?
- Which files participate?
- What business rules already exist?
- What data is read or written?
- What validation occurs?
- What side effects occur?
- What authorization exists?
- What assumptions could be broken?

AGY should not redesign the application.

## Verify AGY Findings

Codex must independently inspect any finding that materially affects implementation.

Do not blindly trust claims about:

- business rules
- authorization
- database behavior
- side effects
- security

AGY is a second source of analysis, not the final authority.

## Desired State

Separate:

```text
Current State
Desired State
Gap
```

Codex owns the desired behavior and business decisions.

## Minimal Change Design

Prefer:

- minimal file count
- minimal schema changes
- compatibility with existing behavior
- reuse of existing abstractions
- explicit validation
- reversible changes

Avoid broad refactors unless necessary.

## Implementation

Codex owns all code changes.

Default:

```text
Codex = writer
AGY = investigator / reviewer
```

Preserve unrelated behavior and avoid overlapping edits.

## Tests

Depending on the project, consider:

- syntax checks
- linting
- type checks
- unit tests
- integration tests
- build tests
- API tests
- database checks
- manual workflow verification

AGY review does not replace testing.

## Diff Inspection

After implementation:

```bash
git diff
```

Check that:

- only intended files changed
- no accidental formatting churn exists
- no secrets were introduced
- no unexpected deletion occurred
- business-rule changes match the design

## Review Decision

Read:

```text
agent/AGY_REVIEW.md
```

when the change affects:

- authorization
- security
- data writes
- record identity
- import logic
- cross-module behavior
- historical records
- difficult edge cases
- production-critical workflows

Review may be skipped when the implementation is small, narrow, well-tested, and low risk.

## Completion Criteria

A COLLAB task is complete when:

```text
desired behavior implemented
+
relevant existing behavior preserved
+
tests performed
+
diff inspected
+
material AGY findings verified
+
no known critical regression
```

If deployment is requested, follow:

```text
agent/DEPLOYMENT.md
```

## Anti-Patterns

Avoid:

- AGY calls made only for decoration
- Codex and AGY repeating the same full analysis
- two agents editing overlapping files
- infinite Codex → AGY loops

Normal COLLAB work should usually require:

```text
one current-state checkpoint
+
zero or one review checkpoint
```
