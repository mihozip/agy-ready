# Architecture

- `AGYReady.Core`: check contract, diagnostic orchestration, remediation policy, version parsing, state classification, platform abstractions.
- `AGYReady.Windows`: Windows environment, process, executable discovery, User PATH backup/repair, official AGY installer, and development tool adapters.
- `AGYReady.App`: WPF diagnostic and explicit AGY/clasp repair/install controls.
- `tests`: state and path behavior tests.

All external command execution flows through `IProcessRunner`. Checks remain read only. Repair changes only the current user's PATH and verifies it before reporting success. The installer downloads scripts from the fixed official HTTPS endpoint and executes them only after user confirmation.

The clasp installer uses the fixed npm package name `@google/clasp`, runs only after Node/npm prerequisite checks, and verifies `clasp --version` after npm exits.
