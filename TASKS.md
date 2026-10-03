# Tasks

| Task | Status | Notes |
| --- | --- | --- |
| 001 .NET 8 solution | Done | Five projects in `AGYReady.sln` |
| 002 App, Core, Windows, tests | Done | WPF diagnostic shell |
| 003 ProcessRunner | Done | ArgumentList, timeout, stdout/stderr |
| 004 SystemInfoService | Done | OS, architecture, privilege |
| 005 PathService read only | Done | Process, User, Machine PATH |
| 006 AgyLocator | Done | where.exe, default directory, PATH directories |
| 007 Milestone 1 checks | Done | 11 required check IDs |
| 008 DiagnosticService | Done | State classification and check collection |
| 009 Debug output | Done | WPF text output and Debug.WriteLine |
| 010 Automated scenarios | Done | Missing, off PATH, working, stale, invalid |

Milestone 2 User PATH repair, backup, conditional restore, environment broadcast, and fresh-process verification: implemented. Milestone 3 official PowerShell installer, CMD fallback, network endpoint check, timeout, and re-verification: implemented. The WPF window now exposes these actions with confirmation. Local Release build and 22 automated tests passed with the temporary .NET 8 SDK. AGY performed bounded current-state, design-challenge, and implementation-review checkpoints. Windows manual acceptance remains pending.

Milestone 6 clasp slice: Git, Node.js 20+, npm, and clasp checks plus confirmed `npm install @google/clasp -g` and version verification are implemented. Automatic Git/Node installation remains open. Local Release build and 34 automated tests passed. Windows clasp acceptance remains pending.
