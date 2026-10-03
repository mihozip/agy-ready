# AGY Ready

Windows AGY CLI and clasp environment assistant based on [the product plan](AGY_Ready_Product_Design_Plan.md). The current build includes diagnosis, User PATH repair and rollback, AGY installation through Google's official installer, and Git/Node/npm/clasp checks with explicit clasp installation.

The window title is **AGY CLI-CLASP 安裝小幫手（標標版）**. The **作者** tab credits 苗栗縣永貞國民小學彭智標老師 and provides [Facebook](https://www.facebook.com/albert.peng.56?locale=zh_TW) and `mihozip@gmail.com` contact links. The app icon incorporates 「標」.

## Run on Windows

Install the .NET 8 SDK, then run:

```powershell
dotnet build AGYReady.sln --configuration Release
dotnet test AGYReady.sln --configuration Release
dotnet run --project src/AGYReady.App/AGYReady.App.csproj
```

The WPF window checks the environment on launch. If AGY is found outside the active PATH, **修復 User PATH** backs up the current user setting, changes only User PATH, and verifies AGY in a new process. **還原本次 PATH** restores that backup only if the value has not changed since the repair. If AGY is missing, **安裝 AGY** asks for confirmation, downloads the official installer from `https://antigravity.google/cli/`, and verifies the executable and version after installation. The application does not request administrator access or change PowerShell execution policy.

The same window checks Git, Node.js, npm, and clasp. If Node.js is missing or older than 20, or npm cannot run, **下載／安裝 Node.js + npm** appears. For missing or outdated Node.js, it confirms and tries the WinGet `OpenJS.NodeJS.LTS` package. If WinGet is unavailable or verification fails, it opens the official Node.js download page for an LTS Windows installer. If Node.js works but npm does not, it opens the official page with instructions to repair the existing installation and include npm and PATH. **重新檢查** reads the latest system and user PATH, including changes made while the app is open. **安裝 clasp** becomes available when Node.js 20 or later and npm work but clasp does not. It runs Google's documented `npm install @google/clasp -g` command and requires `clasp --version` to pass afterward. Google login remains a separate user action.

The Windows Terminal section checks whether its package is registered for the current user and whether the `wt.exe` command alias is available. If the package is missing, **安裝 Windows Terminal** uses the official `Microsoft.WindowsTerminal` WinGet package and rechecks registration. **修復終端右鍵選單** opens Windows Terminal's app settings for Microsoft's **Repair** action. On Windows 10, **Windows 10：加入免 Shift 選單** registers a separate **在此資料夾中開啟終端機畫面** entry for the current user's folder-background context menu. It uses the current user's Windows Terminal alias, does not add the `Extended` Shift-only flag, and can be removed with **移除 AGY Ready 加入的選單**. For an entry created by the previous release, **更新右鍵選單名稱** changes only its title after checking ownership and the existing command. The app refuses to overwrite an existing or modified entry. The app cannot prove that an Explorer context-menu entry is visible; confirm it by right-clicking a folder background after the change.

The window uses a native desktop layout with setup controls, a check list, a detailed log, and a status bar. During an operation, the status bar shows its current stage and elapsed time. For the AGY official bootstrap script, it shows a real download percentage when the server supplies a content length. The subsequent AGY installer, WinGet, and npm do not provide a reliable percentage to this app, so those stages use an indeterminate progress bar labeled **無可用百分比**. The progress indicator stops on success or failure.

The `win-x64` self-contained EXE is produced with:

```powershell
dotnet publish src/AGYReady.App/AGYReady.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/win-x64
```

The EXE and WPF window both use `src/AGYReady.App/Assets/AGYReady.ico`. This build is portable and does not require an installer or a separately installed .NET runtime; .NET extracts bundled native files to a temporary directory when needed. Code signing is not required to build or launch the EXE, but the current build is unsigned. For public distribution, sign the final published EXE with a trusted publisher certificate and timestamp it; unsigned downloads can trigger Microsoft Defender SmartScreen or be blocked by organization policy. After signing, do not modify the EXE. On Windows, inspect a file with `Get-AuthenticodeSignature .\AGYReady.App.exe`.

Windows manual acceptance of the installation, PATH, Node/npm, and clasp flows is pending. Report export and automatic Git installation remain later work.

## Run on macOS

The native AppKit macOS version is in `macos/` and requires macOS 14 or later. On a Mac with Swift 5.9 or later installed, build and launch it with:

```sh
./script/build_and_run.sh
```

The app bundle is written to `dist/macos/AGYReadyMac.app`. To create the bundle without opening it, use `./script/build_and_run.sh --build-only`; `./script/build_and_run.sh --verify` builds, opens, and confirms the app process starts. It currently builds for the architecture of the Mac doing the build (Apple Silicon/arm64 on this development machine).

The macOS app has environment, AGY CLI, Node.js/clasp, and author pages. It checks AGY, zsh, Git, Node.js 20+, npm, clasp, macOS, and processor architecture. AGY installation downloads and runs the HTTPS installer from Google's official `antigravity.google` host. Node.js setup opens the official download page so the user can choose the appropriate installer. clasp installation runs `npm install @google/clasp -g` after Node.js and npm pass verification. PATH repair adds `~/.local/bin` to the current user's `~/.zprofile`, saves a backup under `~/Library/Application Support/AGYReadyMac/`, verifies AGY in a fresh zsh login shell, and offers guarded restoration. Installation and PATH changes require explicit confirmation in the app.

This locally built app is unsigned and not notarized. For local testing, macOS may require opening it through Finder's **Open** action and confirming the prompt. Public distribution should use Developer ID signing and Apple notarization. Manual macOS acceptance steps are in [docs/MACOS_ACCEPTANCE.md](docs/MACOS_ACCEPTANCE.md).
