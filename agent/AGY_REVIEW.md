# AGY Independent Review Checkpoint

## Purpose

Use AGY as an independent reviewer after meaningful code changes.

Focus on real defects:

- correctness
- regression
- security
- authorization
- data integrity
- edge cases
- unintended side effects

Do not use this as a style-review stage.

## When Review Is Recommended

Use review when changes affect:

- authentication
- authorization
- database writes
- record identity
- import logic
- migrations
- production data
- historical records
- security boundaries
- financial logic
- cross-module behavior
- production-critical workflows
- significant refactors

For DEEP work, review is normally expected.

## Preferred Scope

Prefer:

```text
git diff
```

or a bounded patch.

Before review, Codex should:

1. run tests
2. inspect git diff
3. remove unrelated changes
4. identify known constraints

## Standard Prompt Template

```bash
agy -p "
Review only the current git diff.

Do not modify files.
Do not redesign the feature.

Check only for:
1. correctness bugs
2. regression risks
3. security issues
4. authorization problems
5. data-integrity problems
6. missing edge cases
7. unintended side effects

Ignore subjective formatting or style unless it causes a real defect.

Return at most 8 findings.

For each finding include:
- severity
- affected file / location
- issue
- evidence
- likely impact
- suggested verification

If no meaningful issue exists, say so clearly.
"   --model claude-sonnet-4-6   --output-format json
```

If the configured reviewer model is unavailable, use the closest available strong reasoning/review model.

## Severity Guidance

```text
Critical
→ likely security breach, destructive data loss, or unusable production path

High
→ serious correctness, authorization, or integrity defect

Medium
→ meaningful regression or edge-case failure

Low
→ real but limited defect
```

Do not report cosmetic preferences as defects.

## Review Variants

### Security

Focus on:

- authentication bypass
- authorization bypass
- IDOR / ownership
- tenant isolation
- unsafe input handling
- secret exposure
- privilege escalation
- server/client enforcement mismatch

### Database / Migration

Focus on:

- migration ordering
- existing data compatibility
- nullability
- constraints
- duplicate records
- identity semantics
- rollback
- partial failure
- data loss

### Import

Focus on:

- duplicate matching
- overwrite behavior
- idempotency
- partial import
- validation order
- historical records
- year/version identity
- error recovery

### GAS

Focus on:

- Sheet column assumptions
- Drive permissions
- file naming
- trigger duplication
- concurrent writes
- property storage
- authorization
- quota-sensitive loops
- partial failure

## Codex Verification

For every material finding:

1. inspect referenced code
2. reproduce or reason through the issue
3. classify as confirmed / rejected / uncertain
4. fix only confirmed or well-supported issues
5. rerun relevant tests

Do not create speculative churn.

## Review Loop Limit

Normal:

```text
one independent review
→ Codex fixes confirmed issues
→ final tests
```

A second review is justified only when fixes materially changed the implementation or a high-risk issue remains unresolved.

## Completion

The review is complete when:

```text
material findings verified
+
confirmed issues fixed
+
relevant tests rerun
+
no known critical issue remains
```
