# Decisions

## 2026-10-02: First round scope

The product plan says the first development round covers Milestones 0 and 1. The initial WPF window is a diagnostic output surface only; the complete wizard belongs to Milestone 4.

## 2026-10-02: Development host validation

The macOS host had no system `dotnet` command. A .NET 8 SDK was installed under `/tmp/agyready-dotnet` for local build and tests. The Windows WPF application cannot run on this host; Windows behavior still requires Windows validation.

## 2026-10-02: Official install and PATH repair

Milestones 2 and 3 use the official `https://antigravity.google/cli/install.ps1` and `install.cmd` scripts. The application downloads each script to a temporary file and runs it via `IProcessRunner` after explicit user confirmation. PowerShell execution policy is not modified. User PATH is read and written through HKCU Registry APIs to preserve `REG_SZ` versus `REG_EXPAND_SZ` and literal variables; a backup enables conditional restore. Diagnostic checks remain read only.

## 2026-10-02: clasp support

Google's clasp guide requires Node.js 20 or later and documents `npm install @google/clasp -g`. The application checks Git, Node, npm, and clasp, but only automates clasp installation. Node/Git installation and `clasp login` remain user actions. npm and clasp commands run through a child CMD process with a freshly assembled PATH; exit code and `clasp --version` are both checked before reporting success.
