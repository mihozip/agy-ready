# AGENTS.md

## Purpose

This repository uses a lean **Codex × AGY checkpoint-based collaboration workflow**.

The goal is not maximum agent activity. The goal is:

- minimum duplicated context
- clear ownership
- useful independent analysis
- safer implementation
- efficient token usage

Core routing:

```text
Context-heavy        → AGY
Decision-heavy       → Codex
Implementation-heavy → Codex
Risk-heavy           → AGY review
Final responsibility → Codex
```

## Roles

### Codex

Codex is the primary:

- orchestrator
- decision maker
- implementation owner
- integration owner
- final verifier
- deployment owner

Codex owns all code changes and final decisions.

### AGY

AGY is primarily a bounded, read-only:

- investigator
- tracer
- analyzer
- challenger
- independent reviewer

AGY findings are advisory. Codex must verify material findings before changing code.

## Task Modes

Classify each task internally as:

```text
SOLO
COLLAB
DEEP
```

The user does not need to choose the mode.

### SOLO

Use for small, isolated, low-risk changes.

Examples:

- typo or copy changes
- CSS adjustments
- obvious syntax errors
- isolated function changes
- mechanical changes limited to roughly 1–2 files
- changes with no business-rule, data, authorization, or cross-file impact

Workflow:

```text
Codex inspect
→ implement
→ test
→ verify
```

AGY is normally unnecessary.

### COLLAB

Default for non-trivial modifications to an existing system.

Examples:

- modifying an existing feature
- changing validation or import logic
- adding behavior that interacts with existing data
- changing permissions or workflow behavior
- multi-file changes
- existing GAS / PHP / database-backed feature changes

Workflow:

```text
Codex local discovery
→ identify relevant files
→ AGY current-state analysis
→ Codex verify findings
→ Codex design
→ Codex implement
→ tests
→ AGY review when risk justifies it
→ Codex final verification
```

For COLLAB tasks, read:

```text
agent/COLLAB.md
```

### DEEP

Use for high-risk or difficult-to-rollback work.

Examples:

- authentication
- authorization
- database migrations
- production data
- record identity
- destructive operations
- payment / financial logic
- security-sensitive changes
- major refactors
- architecture changes
- complex legacy systems

Workflow:

```text
Codex discovery
→ AGY current-state investigation
→ Codex design
→ AGY challenge
→ Codex implementation
→ tests
→ AGY independent diff review
→ Codex verification
→ deployment preparation
```

For DEEP tasks, read:

```text
agent/DEEP.md
```

## Collaboration Rule

For a non-trivial modification to an existing system, AGY current-state inspection is the default.

Codex may skip AGY only when all of these are true:

- the change is clearly isolated
- relevant existing behavior is already understood
- no business rule is affected
- no authorization or security rule is affected
- no data-integrity implication exists
- no meaningful cross-file interaction exists
- AGY would mostly duplicate already-completed work

When uncertain, prefer one bounded AGY current-state checkpoint.

## Context Rule

Do not send AGY the entire repository as the first step.

Codex should first narrow scope using cheap local inspection such as:

```bash
rg
find
git status
git diff
git ls-files
targeted file reads
```

Preferred AGY scope:

- one workflow
- one module
- one bounded directory
- approximately 3–8 relevant files
- one bounded log
- current git diff

## AGY Rules

Every AGY request should define:

1. exact scope
2. exact question
3. read/write policy
4. expected output
5. maximum findings

Default:

```text
AGY = read only
Codex = writer
```

AGY should return conclusions and evidence, not a long narrative of everything it read.

Do not call AGY merely to make the workflow look multi-agent.

## Git

Before meaningful changes:

```bash
git status
```

After implementation:

```bash
git diff
```

Prefer AGY reviewing the current diff instead of rereading the full repository.

## Safety

AGY must not perform:

- production deployment
- destructive production database operations
- credential changes
- irreversible deletion
- broad permission changes

Production deployment belongs to Codex and requires explicit user authorization unless deployment is clearly part of the current request.

## On-Demand Workflow Docs

Read detailed workflow instructions only when needed:

```text
agent/COLLAB.md
agent/DEEP.md
agent/AGY_CURRENT_STATE.md
agent/AGY_REVIEW.md
agent/DEPLOYMENT.md
```

Do not load every workflow document for every task.

The objective is to provide only the context required for the current stage of work.
