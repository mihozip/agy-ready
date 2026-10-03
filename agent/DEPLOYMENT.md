# Production Deployment Workflow

## Purpose

Code modification and production deployment are separate actions.

Codex may prepare deployment automatically.

Codex must not perform production deployment unless:

- the user explicitly asks to deploy, or
- deployment is clearly part of the current requested workflow

AGY must never perform production deployment.

## Pre-Deployment Gate

Before deployment, verify:

```text
1. intended change is complete
2. relevant tests pass
3. git diff has been inspected
4. target environment is identified
5. target files are identified
6. unintended files are excluded
7. database implications are understood
8. backup / rollback is available where appropriate
9. credentials are not exposed
10. deployment authorization exists
```

## File Deployment

Before replacing production files:

- identify exact target paths
- back up existing files when practical
- confirm only intended files will be uploaded
- preserve environment-specific configuration
- avoid overwriting secrets or local production config
- verify ownership/permissions when relevant

## Database Deployment

Before production schema/data changes, determine:

- migration sequence
- affected tables
- expected affected rows
- existing-data compatibility
- lock / downtime implications
- rollback method
- backup availability
- partial-failure behavior

Never assume production data matches development data.

## Destructive Operations

For operations such as:

- DROP
- DELETE without narrow predicates
- irreversible migrations
- bulk overwrite
- historical-data normalization
- credential rotation
- mass file deletion

Codex should require explicit confirmation unless the user already clearly authorized that exact destructive action.

AGY may review the plan but must not execute it.

## Preferred Deployment Sequence

```text
Local implementation
→ local/focused tests
→ broader tests when justified
→ git diff inspection
→ AGY review if required
→ Codex verifies review
→ final tests
→ backup
→ deploy
→ smoke test
→ verify production behavior
```

## Smoke Test

After deployment, verify the smallest safe set of behaviors needed to establish success.

Examples:

- page loads
- authentication works
- changed workflow completes
- expected database write occurs
- no obvious server error
- critical unrelated route still works

Avoid destructive production testing.

## Rollback

Before risky deployment, know:

```text
what will be restored
from where
in what order
what database state must accompany rollback
```

A file rollback without a compatible database rollback may be unsafe.

## Deployment Summary

After deployment, summarize:

- what was deployed
- what was not deployed
- tests performed
- production verification performed
- remaining risk or manual follow-up

Do not claim success if production verification was not possible.

## AGY Boundary

AGY may:

- inspect deployment-related code
- review migrations
- challenge rollback assumptions
- review a deployment diff or file list

AGY must not:

- upload production files
- run production migrations
- delete production data
- modify credentials
- change production permissions
- trigger irreversible production actions

Final production responsibility belongs to Codex.
