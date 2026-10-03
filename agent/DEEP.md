# DEEP Workflow

## Purpose

DEEP is for high-risk, architecture-heavy, security-sensitive, or data-sensitive work.

Typical triggers:

- authentication
- authorization
- production database schema
- migrations
- destructive operations
- record identity
- historical data reconciliation
- payment / financial logic
- security boundaries
- major refactors
- architecture changes
- complex legacy behavior

## Standard Workflow

```text
1. Understand request
2. Inspect repository state
3. Narrow the affected subsystem
4. AGY Current-State Investigation
5. Codex builds explicit current-state model
6. Codex defines desired state
7. Codex proposes implementation plan
8. AGY challenges assumptions
9. Codex finalizes design
10. Codex implements minimal safe change
11. Run focused tests
12. Run broader regression tests
13. Inspect git diff
14. AGY independent diff review
15. Codex verifies every material finding
16. Fix confirmed problems
17. Run final tests
18. Prepare rollback / deployment plan
19. Deploy only when authorized
```

## Current-State Phase

Read:

```text
agent/AGY_CURRENT_STATE.md
```

Investigate:

- entry points
- data flow
- business rules
- authorization
- security boundaries
- persistence
- side effects
- external integrations
- historical-data assumptions
- rollback-sensitive behavior
- regression surfaces

Codex must independently verify material findings.

## Explicit Current-State Model

For DEEP work, Codex should understand:

```text
who can perform the action
what data enters
what validation occurs
what records are read
what records are written
what side effects occur
what happens on failure
what historical states may exist
```

## Desired State

Codex owns all business and architectural decisions.

Internally distinguish:

```text
Current State
Desired State
Constraints
Migration / Compatibility Requirements
Risk Areas
Rollback Requirements
```

## Challenge Checkpoint

Use AGY after Codex has a concise implementation plan.

Suggested prompt:

```bash
agy -p "
Act as an independent implementation-plan challenger.

Do not modify files.

Scope:
- <file/module 1>
- <file/module 2>
- <file/module 3>

Proposed plan:
<concise Codex plan>

Check only for:
1. misunderstood business rules
2. security or authorization risks
3. data-integrity risks
4. migration / compatibility risks
5. missing edge cases
6. hidden side effects
7. opportunities to reduce the change surface

Return at most 7 findings.

For each finding include:
- concern
- evidence
- likely impact
- recommended verification
"   --model gemini-3.8-flash-medium   --output-format json
```

If that model is unavailable, use the closest available AGY reasoning model without broadening scope.

## Database Safety

Before schema or data changes, consider:

- existing row states
- nullability
- duplicate records
- unique constraints
- foreign keys
- record identity
- update semantics
- delete semantics
- import reconciliation
- migration order
- rollback

Never assume production data is clean.

## Security / Authorization

Verify:

- authentication requirement
- role checks
- tenant / organization boundary
- direct object access
- server-side authorization
- privilege escalation
- hidden admin paths
- API parity with UI restrictions
- failure behavior

UI visibility is not authorization.

## Tests

DEEP work should usually include:

- focused tests
- broader regression tests
- permission tests
- negative tests
- migration tests when applicable
- rollback checks when applicable
- data-integrity checks
- manual workflow verification

## Independent Review

Read:

```text
agent/AGY_REVIEW.md
```

For DEEP tasks, independent diff review is normally expected.

## Deployment

Read:

```text
agent/DEPLOYMENT.md
```

Deployment must never be delegated to AGY.

## Completion Criteria

DEEP work is complete when:

```text
current state understood
+
desired state explicitly defined
+
implementation risk challenged
+
minimal safe change implemented
+
focused tests passed
+
regression tests performed
+
diff independently reviewed
+
review findings verified
+
rollback/deployment implications understood
```
