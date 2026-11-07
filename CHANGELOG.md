# Changelog

## 0.2.0 (2026-11-07)

- Bootstrap .NET 8 solution and read only AGY diagnostics.
- Add a minimal WPF status window and automated state tests.
- Identify Windows 11 by OS build number instead of displaying the shared 10.0 version.
- Add User PATH repair, backup, conditional restore, environment notification, and fresh-process verification.
- Add official AGY installer execution with PowerShell and CMD fallback, network check, and post-install verification.
- Open AGY's official interactive login, authorization, and color setup after successful installation on macOS and Windows, with a reopen button for existing installations.
- Add confirmation controls for installation and repair in the WPF window.
- Add Git, Node.js, npm, and clasp checks, plus confirmed clasp installation with post-install version verification.
- Add macOS diagnostic report copy and save actions with home-path redaction and the last operation error.
- Add a user-initiated Windows clasp login window using `clasp.cmd login` and show the same command for PowerShell.
- Add an optional, confirmed Windows PowerShell `clasp` repair with policy checks, fresh-process verification, and restoration on failure.
- Guard PowerShell policy restoration after cancellation or verification errors, and launch clasp login outside user-writable directories.
