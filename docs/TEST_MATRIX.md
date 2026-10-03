# Milestone 0/1 test matrix

Automated: Windows 10/11 build classification, missing AGY, AGY outside PATH, working AGY, stale process PATH, failed or missing version, privacy filtering, PATH normalization, `where.exe` current-directory hit, process output cap and timeout.

Windows manual checks still required: fresh machine, installed AGY, stale process PATH, PowerShell/CMD unavailable, managed user account.

Milestone 2/3 automated checks: PATH raw-value and type preservation, rollback after failed verification, refusal to overwrite later PATH changes, PowerShell-to-CMD installer fallback, installer exit code without a verified binary, and explicit child PATH override.

Windows manual acceptance still required: VM-A fresh install, VM-B PATH repair and rollback, VM-C working install with no modification, VM-D blocked network/PowerShell with no false success. Compare User PATH before and after each action.

clasp automated checks: Node 18 versus 20, npm availability, working clasp, npm exit 0 without clasp, and skipped install when Node is unsupported. Windows manual checks: Node missing, npm missing, clasp missing with prerequisites present, clasp already working, and npm global prefix absent from PATH.
