# AGY Current-State Checkpoint

## Purpose

Use this checkpoint when Codex needs an independent, bounded understanding of how an existing feature currently works.

The objective is context compression:

```text
several source files
→ AGY
→ a few actionable findings
```

## When to Use

Use for:

- non-trivial changes to existing features
- unfamiliar modules
- multi-file workflows
- legacy code
- import/export behavior
- authorization flows
- database-backed workflows
- hidden side effects
- duplicate business logic
- large logs or test output

Skip when the task is clearly SOLO.

## Preparation

Before calling AGY, Codex should:

1. understand the user request
2. run cheap local discovery
3. identify the likely workflow
4. narrow the scope
5. select approximately 3–8 relevant files when possible

Useful local commands:

```bash
rg
find
git ls-files
git status
targeted file reads
```

## Required Prompt Fields

Every request should specify:

```text
Scope
Question
Read/write policy
Focus areas
Expected output
Maximum findings
```

Default:

```text
Do not modify files.
```

## Standard Prompt Template

```bash
agy -p "
You are performing a bounded current-state analysis.

Scope:
- <path/to/file1>
- <path/to/file2>
- <path/to/file3>

Do not modify files.

Task:
Explain how the existing feature currently works.

Focus on:
1. entry points
2. control/data flow
3. business rules
4. persistence behavior
5. validation
6. authorization
7. dependencies and side effects
8. regression-sensitive behavior

Return at most 5 findings.

For each finding include:
- file / function / location
- observed current behavior
- evidence
- why it matters

Do not redesign the application.
Do not propose broad refactors.
If something cannot be established from the scoped files, state that explicitly.
"   --model gemini-3.8-flash-medium   --output-format json
```

## Specialized Focus Areas

### Import / Data Flow

Focus on:

- input parsing
- normalization
- identity matching
- duplicate handling
- validation
- write order
- update semantics
- error handling
- transaction behavior
- historical-data assumptions

### Authorization

Focus on:

- authentication
- role checks
- tenant / organization checks
- resource ownership
- server-side enforcement
- API/UI differences
- privilege escalation paths

### GAS Workflow

Focus on:

- form/web-app entry point
- Sheet reads/writes
- Drive operations
- trigger behavior
- property storage
- authorization
- naming conventions
- side effects
- admin dashboard dependencies

## Verification Rule

Codex must independently verify findings that affect:

- business rules
- authorization
- data integrity
- destructive behavior
- schema assumptions
- external integrations
- implementation direction

Do not blindly implement AGY recommendations.

## Scope Failure

If AGY needs more context:

1. identify the missing dependency
2. add only necessary files
3. rerun a narrower question if justified

Avoid replacing a bounded checkpoint with a full-repository dump.

## Completion

The checkpoint is complete when Codex can distinguish:

```text
Current State
Desired State
Gap
```
