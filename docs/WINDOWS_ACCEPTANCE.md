# Windows acceptance for PATH repair and installation

Use a Windows 11 x64 VM or a test account. Keep a snapshot before testing installation or PATH changes.

1. Run `AGYReady.App.exe`. Confirm the system and AGY checks complete.
2. On a machine without AGY, confirm `NotInstalled`, then select **安裝 AGY** and approve the official installer. Success requires `InstalledWorking` and a version number. Confirm a separate console opens AGY in the dedicated Onboarding folder. Complete account authentication, authorization code, color scheme, and rendering mode there. Close AGY, then run it in a project and confirm those setup questions do not repeat; project trust should still be asked for that project. In a newly opened PowerShell window, run `agy --version`. For an existing installation, confirm **開啟 AGY 登入與色彩設定** opens AGY.
3. On a machine with `agy.exe` outside PATH, confirm `InstalledNotInPath`, select **修復 User PATH**, and require `InstalledWorking`. Run `agy --version` in a new PowerShell window.
4. Before and after repair, capture `reg query HKCU\Environment /v Path`. Select **還原本次 PATH** and confirm the original User PATH value and registry type return. If another program changes PATH after repair, restore must refuse to overwrite it.
5. With network access unavailable, confirm `NET-001` warns and installation does not report success.

Keep the diagnostic screen, PowerShell output, and installer exit codes for any failed case. The application does not show raw installer stdout/stderr because these can contain private data.

During AGY installation, require the status bar to show the elapsed time and a real percentage only while the official bootstrap script downloads with a known content length. During the installer and verification stages, require **無可用百分比** and an indeterminate progress bar. Node.js/npm and clasp should also show the indeterminate bar while their external installers run. After success or failure, the progress bar must disappear and the buttons must become usable again. Confirm the **檢查項目** list and **詳細資訊** tab remain readable at the minimum window size.

## clasp

1. With Node.js missing or Node.js 18, confirm `DEV-002` fails, **下載／安裝 Node.js + npm** appears, and **安裝 clasp** is disabled. Approve the Node installation. If WinGet works, require `DEV-002` and `DEV-003` to pass after installation. If WinGet is unavailable, require the official Node.js download page to open and follow the LTS Windows installer instructions.
2. With Node.js 20 or later but npm unavailable, confirm the Node/npm button appears and opens the official page with repair instructions. Repair or reinstall Node.js with npm and PATH selected, then press **重新檢查**; require `DEV-003` to pass.
3. With Node.js 20 or later and npm working, but clasp absent, confirm the Node/npm button is hidden and **安裝 clasp** is enabled. Approve installation and require `DEV-004` to pass with a version.
4. Open a new PowerShell window and run `node --version`, `npm.cmd --version`, and `clasp.cmd --version`. All must succeed. Confirm the app displays **開啟 clasp 登入視窗** after clasp passes. Click it and confirm a separate command window runs `clasp.cmd login` and lets the user finish authentication in the browser. Verify that it works even when the app itself was opened from a UNC path, and that a `clasp.cmd` in the user profile directory is not run instead of the PATH command.
5. Run `clasp.cmd login` manually in PowerShell when needed; a bare `clasp` command may select `clasp.ps1` and be blocked by PowerShell's script execution policy. Confirm the app leaves that policy unchanged unless **修復 PowerShell clasp 指令** is explicitly approved. With clasp already working, the install button should be disabled and Google login should not start automatically.
6. On a disposable Windows account where bare `clasp --version` is blocked by `Restricted`, click **修復 PowerShell clasp 指令**. Confirm the warning describes a current-user-wide change, then approve. Require a fresh Windows PowerShell `clasp --version` to pass. Record the previous CurrentUser policy and use the displayed restore command after the test.
7. With a Group Policy or `AllSigned` policy in effect, confirm the repair refuses to change the setting. If a test shim still fails after changing to `RemoteSigned`, confirm the previous CurrentUser policy is restored. PowerShell 7 (`pwsh.exe`) is outside this repair's scope.

## Windows Terminal context menu

1. On Windows 11 without Windows Terminal registered for the test user, confirm `TER-001` fails and **安裝 Windows Terminal** appears. Approve the WinGet installation; require `TER-001` to pass afterward. If WinGet fails, require the official Microsoft installation instructions to open.
2. With Windows Terminal installed, confirm **修復終端右鍵選單** appears. Select it and require Windows Terminal's app settings to open. Use **Repair**, then right-click a folder background in File Explorer and look for **在終端中開啟**. Do not use **Reset** as the first step.
3. If `wt.exe` is not found but the package is registered, require `TER-002` to warn without claiming the Explorer menu is absent. The menu itself must be checked visually; the app must not report it as verified solely from package registration.
4. On Windows 10 with Windows Terminal installed and `%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe` present, confirm **Windows 10：加入免 Shift 選單** appears. Approve it, then right-click a folder background **without Shift**. Require **在此資料夾中開啟終端機畫面** to appear and open Terminal in that folder. `TER-004` may pass for registration, but visual confirmation is still required. If Explorer was already open, reopen it and check again.
5. Confirm `reg query "HKCU\Software\Classes\Directory\Background\shell\AGYReady.OpenInTerminal"` shows the AGY Ready owner and no `Extended` value. Use **移除 AGY Ready 加入的選單**; require that only this custom entry disappears. The Windows Terminal native entry and its package must remain.
6. If the alias is missing, require the add button to be disabled and `TER-004` to warn. Enable the Windows Terminal app execution alias in Windows Settings, press **重新檢查**, and require the add button to become available. If the AGY Ready registry entry has been edited by another program, require the app to refuse removal or replacement.
7. If the previous **在終端中開啟 (AGY Ready)** entry is installed, require **更新右鍵選單名稱** to appear. Approve it and confirm the menu changes to **在此資料夾中開啟終端機畫面** without changing its command. If the old entry's command or ownership marker was modified, require the app to refuse the update.

## Portable EXE and icon

1. Copy only `AGYReady.App.exe` from `dist/win-x64` to a Windows 10/11 x64 test machine without .NET 8 installed. Confirm it opens without an installer or runtime prompt.
2. Confirm the AGY Ready icon appears on the EXE in File Explorer, in the window title bar, and on the taskbar. If Explorer caches the old icon, rename the copied EXE or test in a fresh folder.
3. Run `Get-AuthenticodeSignature .\AGYReady.App.exe` in PowerShell. The current test build should report `NotSigned`. Record any SmartScreen prompt separately from app startup behavior.
4. Confirm the window title reads **AGY CLI-CLASP 安裝小幫手（標標版）**. Open **作者** and verify the school, author name, Facebook link, and contact email. Confirm the app icon displays 「標」 at normal Explorer icon size.
