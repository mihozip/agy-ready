# Tasks

Version 0.2.0: rebuilt the macOS arm64 app and Windows x64 self-contained EXE with the AGY first-run setup flow. Automated .NET tests pass (57 total); native Windows interaction and macOS XCTest acceptance remain pending.

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

Milestone 5 macOS slice: copy and save an anonymized diagnostic report with check results and the last operation error. macOS build passes; XCTest execution awaits a full Xcode test runtime. Windows report export remains open.

Windows clasp login: a user-initiated button opens `clasp.cmd login` in a separate command window using a fresh PATH and the Windows system directory as its working directory. Windows interactive acceptance remains pending.

Windows PowerShell bare `clasp` repair: optional CurrentUser `RemoteSigned` action with policy checks and rollback on failed verification, cancellation, or exception. Release build and 57 automated tests passed locally; the `win-x64` self-contained EXE was republished. Windows manual acceptance remains pending.
