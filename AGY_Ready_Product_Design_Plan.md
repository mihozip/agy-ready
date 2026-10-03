# AGY Ready — Windows AI Coding Environment Setup Assistant
## 產品設計規劃書 / Product Design Document
版本：v0.1  
目標平台：Windows 10 / Windows 11 x64  
開發方式：Codex + AGY CLI  
建議技術：C# / .NET 8 / WPF / MVVM  

---

## 1. 產品摘要

### 1.1 產品名稱
暫定名稱：**AGY Ready**

副標題：**AGY CLI 安裝、環境檢測、修復與驗證助手**

### 1.2 問題背景
在 AI / GAS 研習現場，學員真正卡住的往往不是 AGY CLI 的操作，而是 CLI 啟動前的環境問題，例如：

- 不確定 AGY 是否已安裝。
- `agy.exe` 已存在，但終端機輸入 `agy` 找不到。
- User PATH 未加入正確路徑。
- PATH 已修改，但舊終端機尚未重新載入。
- PowerShell / CMD 執行環境不同。
- 權限、Execution Policy、Proxy、防毒或網路環境阻擋安裝。
- Node.js、npm、Git、clasp 未安裝或版本不符合課程需求。
- 學員只能描述「老師，我的不行」，講師難以遠端判斷問題。

AGY Ready 的目的不是再做一個 AGY，而是把「人工 troubleshooting」轉換成可重複的：

> **Detect → Diagnose → Repair → Verify → Report**

---

## 2. 產品目標

### 2.1 核心目標
讓一般 Windows 使用者在不理解 PATH、環境變數或 CLI 細節的情況下，可以完成：

1. 偵測系統環境。
2. 判斷 AGY 是否已安裝。
3. 找出 AGY 真正的執行檔位置。
4. 確認 PATH 是否正確。
5. 自動修復可安全處理的問題。
6. 呼叫官方 AGY 安裝流程。
7. 重新驗證 AGY 是否可以正常執行。
8. 產生可分享的診斷報告。

### 2.2 第二層目標
提供「AI + GAS 開發環境」模式，額外檢查：

- Git
- Node.js
- npm
- `@google/clasp`

完成後讓學員的電腦直接進入可上課狀態。

### 2.3 非目標
MVP 不處理以下事項：

- 不自行重新封裝或散布 AGY binary。
- 不代替使用者完成 Google 帳號登入。
- 不讀取或匯出 Windows Credential Manager 中的 AGY 憑證。
- 不全域放寬 PowerShell Execution Policy。
- 不擅自修改 Machine PATH。
- 不自動關閉防毒、Firewall 或企業安全政策。
- 不修改 Proxy、VPN 或憑證設定。
- 不保證處理學校 / 機關 GPO 限制。
- 第一版不支援 macOS / Linux。
- 第一版不做遠端控制。

---

## 3. 目標使用者

### Persona A：研習學員
特徵：
- 會使用 Windows，但不熟悉 CLI。
- 不理解 PATH。
- 不知道 PowerShell 與 CMD 的差異。
- 希望「按幾個按鈕就完成」。

需求：
- 清楚知道哪裡有問題。
- 可以一鍵修復。
- 修不好時能產生報告交給講師。

### Persona B：研習講師
特徵：
- 同時面對 10–50 台不同狀態的 Windows 電腦。
- 最怕課程前 30–60 分鐘耗在安裝與環境問題。

需求：
- 快速辨識學員環境。
- 統一診斷格式。
- 減少一台一台手動輸入指令。
- 課前可要求學員自行完成健檢。

### Persona C：進階開發者
需求：
- 不需要 GUI 引導。
- 希望可用 CLI 模式快速執行診斷。
- 希望把診斷結果輸出 JSON / Markdown。

此 Persona 可列入 v0.2。

---

## 4. MVP 成功條件

MVP 定義為：**Windows x64 上完成 AGY CLI 的 Detect / Diagnose / Repair / Verify / Report。**

符合以下條件即可視為 MVP 完成：

- 可以偵測 Windows 版本與 CPU 架構。
- 可以判斷 `agy` 是否能直接從 PATH 執行。
- 可以搜尋常見 AGY 安裝位置。
- 可以辨識「已安裝但不在 PATH」。
- 可以安全修改目前使用者的 User PATH。
- 修改 PATH 後可用新的 Process Environment 驗證，不要求使用者先手動重開電腦。
- 未安裝時可呼叫 Google 官方安裝流程。
- 官方安裝失敗時保留 stdout / stderr / exit code。
- 可顯示 AGY 版本。
- 可以產生匿名化診斷報告。
- 所有可修復動作都必須再次驗證，不可只因命令 exit code = 0 就宣告成功。

---

## 5. 產品模式

### 5.1 模式 A：AGY CLI 快速安裝
只處理：

- 系統基本檢查
- PowerShell / CMD
- AGY
- PATH
- AGY 啟動驗證

適合一般 AGY 課程。

### 5.2 模式 B：AI + GAS 完整開發環境
在模式 A 之外增加：

- Git
- Node.js
- npm
- clasp

預設建議順序：

1. Windows 環境
2. Git
3. Node.js
4. npm
5. AGY CLI
6. clasp
7. 完整 smoke test

### 5.3 模式 C：只做環境健檢
不修改系統，只執行 Read-Only 檢查。

適合：
- 學校資訊設備。
- 受管制電腦。
- 使用者不願讓工具修改系統。

---

## 6. UX 設計

### 6.1 首頁

畫面：

```text
AGY Ready
讓你的 Windows 準備好使用 AGY CLI

[開始快速健檢]

安裝模式：
(●) AGY CLI
( ) AI + GAS 完整環境
( ) 僅檢查，不修改系統

[開始]
```

### 6.2 健檢畫面

每個檢查只使用四種狀態：

- PASS：綠色勾勾
- WARNING：黃色警告
- FAIL：紅色叉叉
- CHECKING：進度動畫

範例：

```text
系統
✅ Windows 11 x64
✅ PowerShell 可使用
✅ 網路可連線

AGY CLI
✅ 找到 agy.exe
⚠️ AGY 目錄尚未加入 User PATH
❌ 目前終端環境無法執行 agy

[自動修復 PATH]
```

### 6.3 修復後
任何修復完成後必須立即重新執行相關 checks。

禁止：

> 「PATH 已修改，因此顯示成功」

正確：

> 「PATH 已修改 → 建立新的 Process → 執行 `agy --version` → 成功 → 顯示 PASS」

### 6.4 完成畫面

```text
🎉 AGY CLI 已可正常使用

AGY Version: x.x.x
Path: C:\...\agy.exe

[啟動 AGY]
[複製診斷報告]
[儲存報告]
```

完整環境模式：

```text
✅ Git
✅ Node.js
✅ npm
✅ AGY CLI
✅ clasp

AI + GAS 開發環境已準備完成
```

---

## 7. 系統狀態模型

AGY 狀態至少區分：

```text
Unknown
NotInstalled
InstalledNotInPath
InstalledInUserPathButCurrentProcessStale
InstalledWorking
InstallFailed
PathRepairFailed
ExecutableInvalid
ExecutionBlocked
```

不要只使用：

```text
Installed = true / false
```

因為多數教學現場問題發生在「明明已安裝，但不能執行」。

---

## 8. Check Engine 設計

每項健檢實作成獨立 Check。

統一介面概念：

```csharp
public interface IEnvironmentCheck
{
    string Id { get; }
    string DisplayName { get; }

    Task<CheckResult> RunAsync(
        CancellationToken cancellationToken);
}
```

`CheckResult`：

```csharp
public sealed record CheckResult(
    string CheckId,
    CheckStatus Status,
    string Summary,
    string? Detail = null,
    RemediationAction? Remediation = null);
```

### 8.1 MVP Checks

#### SYS-001
Windows Version

#### SYS-002
Architecture

#### SYS-003
User Privilege

#### SYS-004
PowerShell Available

#### SYS-005
CMD Available

#### NET-001
Official AGY endpoint reachable

#### AGY-001
`agy` resolvable from current PATH

#### AGY-002
Search known AGY locations

至少包含：
- `%LOCALAPPDATA%\agy\bin\agy.exe`
- `where.exe agy`
- Current User PATH directories
- Machine PATH directories

不要只依賴固定路徑。

#### AGY-003
Run `agy --version`

#### AGY-004
Validate executable path

#### PATH-001
AGY directory exists in User PATH

#### PATH-002
AGY directory exists in Machine PATH

#### PATH-003
Current process PATH contains AGY directory

#### DEV-001
Git

#### DEV-002
Node.js

#### DEV-003
npm

#### DEV-004
clasp

---

## 9. AGY 偵測演算法

建議流程：

```text
1. 嘗試 Process 執行：
   agy --version

   成功：
   → InstalledWorking
   → 記錄版本

2. 若失敗，執行：
   where.exe agy

3. 檢查官方預設位置：
   %LOCALAPPDATA%\agy\bin\agy.exe

4. 搜尋 User PATH 中名稱為 agy.exe 的執行檔

5. 搜尋 Machine PATH

6. 若找到 executable，但目前 agy 無法執行：
   → InstalledNotInPath
   或
   → InstalledInUserPathButCurrentProcessStale

7. 若完全找不到：
   → NotInstalled
```

原則：

> **Executable discovery 優先於 hard-coded path。**

---

## 10. PATH 修復策略

### 10.1 只修改 User PATH
MVP 不主動修改 Machine PATH。

### 10.2 不使用 `setx PATH ...`
避免：
- PATH 長度問題。
- 字串截斷。
- 重複路徑。
- 錯誤展開 `%VARIABLE%`。
- 不必要地覆寫既有 PATH。

### 10.3 使用 .NET API

概念：

```csharp
Environment.GetEnvironmentVariable(
    "Path",
    EnvironmentVariableTarget.User);

Environment.SetEnvironmentVariable(
    "Path",
    newPath,
    EnvironmentVariableTarget.User);
```

### 10.4 寫入前
必須：

1. Normalize path。
2. Case-insensitive comparison。
3. 移除尾端 slash 差異。
4. 判斷是否已存在。
5. 備份原始 User PATH。
6. 寫入 log。

### 10.5 寫入後
廣播 Environment Change：

```text
WM_SETTINGCHANGE
Environment
```

再建立新的 Process Environment 進行驗證。

### 10.6 Rollback
若 PATH 修改後產生明顯異常，提供：

> 還原本次 PATH 修改

因此每次修復需保存：

```json
{
  "operation": "AddUserPath",
  "before": "...",
  "after": "...",
  "timestamp": "..."
}
```

---

## 11. AGY 安裝策略

### 原則
AGY Ready **不自行下載、封裝或維護 AGY executable**。

應呼叫 Google 官方 installer。

目前 Windows 官方提供 PowerShell / CMD 安裝方式。

### 安裝流程

```text
未安裝
↓
檢查網路
↓
檢查 PowerShell
↓
執行官方安裝程序
↓
擷取 stdout / stderr / exit code
↓
重新搜尋 agy.exe
↓
重新檢查 PATH
↓
必要時 Repair PATH
↓
執行 agy --version
↓
成功
```

### PowerShell 失敗時
不要自動調整全域 Execution Policy。

可以：

1. 顯示錯誤原因。
2. 嘗試官方 CMD 安裝方式。
3. 若兩者皆失敗，產生診斷報告。

---

## 12. Node.js / clasp 模組

完整開發模式才執行。

Google clasp 文件目前要求 Node.js 20 以上，因此：

```text
node >= 20
```

才視為 PASS。

clasp 安裝：

```text
npm install @google/clasp -g
```

驗證：

```text
clasp --version
```

### Node 狀態

```text
NotInstalled
InstalledUnsupportedVersion
InstalledSupported
```

### clasp 狀態

```text
NotInstalled
InstalledWorking
ExecutionFailed
```

第一版不自動執行：

```text
clasp login
```

因為登入必須保留給使用者互動完成。

---

## 13. 權限策略

整個應用程式預設：

> **AsInvoker**

不要一開程式就要求 Administrator。

原因：
- AGY 官方安裝主要為使用者層級。
- User PATH 不需要 Administrator。
- 減少 UAC 阻力。
- 學校電腦可能沒有管理員密碼。

若特定第三方安裝真正需要 elevation：

1. 只針對該動作要求 elevation。
2. 清楚告知原因。
3. 使用者取消時，系統仍可繼續其他檢查。

---

## 14. 安全設計

### 必須遵守
- 不接觸 AGY / Google auth token。
- 不輸出 Credential Manager 內容。
- 不記錄密碼。
- 不記錄 API Key。
- stdout / stderr 顯示前需做 secret redaction。
- 不修改防毒設定。
- 不關閉 Firewall。
- 不永久變更 Execution Policy。
- 不執行未知第三方腳本。
- 所有 installer URL 必須來自受信任官方來源。
- 外部程序必須有 timeout。
- 所有 Process invocation 必須避免 command injection。

### ProcessRunner
禁止組字串後直接交給 shell：

```text
"command " + userInput
```

應使用：
- `ProcessStartInfo`
- `ArgumentList`
- 明確 executable
- 明確 arguments

---

## 15. Log 與診斷報告

### 15.1 本機 Debug Log
可包含詳細資訊，但不得含 credential / token。

建議：

```text
%LOCALAPPDATA%\AGYReady\logs\
```

### 15.2 分享用報告
需自動匿名化：

```text
C:\Users\Albert\AppData\...
```

轉成：

```text
%USERPROFILE%\AppData\...
```

避免直接暴露使用者名稱。

### 15.3 報告格式

```text
AGY Ready Diagnostic Report

App Version:
Timestamp:

SYSTEM
Windows:
Architecture:
Privilege:

SHELL
PowerShell:
CMD:

AGY
Resolved From PATH:
Executable Found:
Executable Path:
Version:
User PATH:
Machine PATH:
Current Process PATH:

DEVELOPMENT
Git:
Node:
npm:
clasp:

RESULT
Primary Diagnosis:
Suggested Action:

LAST ERROR
Exit Code:
stderr:
```

同時支援：

- Copy to Clipboard
- Save `.txt`
- Save `.json`（v0.2）

---

## 16. 專案架構

```text
AGYReady/
│
├── AGYReady.sln
│
├── src/
│   ├── AGYReady.App/
│   │   ├── App.xaml
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   └── Resources/
│   │
│   ├── AGYReady.Core/
│   │   ├── Checks/
│   │   ├── Models/
│   │   ├── Diagnostics/
│   │   ├── Remediation/
│   │   └── Abstractions/
│   │
│   └── AGYReady.Windows/
│       ├── Environment/
│       ├── Processes/
│       ├── Registry/
│       ├── Installers/
│       └── Shell/
│
├── tests/
│   ├── AGYReady.Core.Tests/
│   └── AGYReady.Windows.Tests/
│
├── docs/
│   ├── PRD.md
│   ├── ARCHITECTURE.md
│   ├── TEST_MATRIX.md
│   ├── SECURITY.md
│   └── DECISIONS.md
│
├── AGENTS.md
├── TASKS.md
├── CHANGELOG.md
├── README.md
└── .gitignore
```

---

## 17. 技術選型

### UI
**WPF + MVVM**

原因：
- Windows-only MVP。
- .NET 原生。
- 對系統 API、Registry、Process、Environment Variables 支援成熟。
- Codex / AGY 都容易理解與維護。
- 不需加入 WebView / Node frontend 的額外複雜度。

### Runtime
`.NET 8`

### 發布
第一版：

```text
win-x64
Self-contained
Single-file
```

目標：

> 使用者不需要先安裝 .NET SDK / Runtime。

### Installer
v0.1：
- Portable EXE

v0.3：
- MSIX / Inno Setup / Velopack 擇一。
- 正式公開散布前評估 Code Signing。

---

## 18. 核心服務

### IProcessRunner
統一所有 CLI 呼叫。

回傳：

```csharp
ProcessResult {
    ExitCode,
    StdOut,
    StdErr,
    TimedOut,
    Duration
}
```

### IAgyLocator
負責尋找 `agy.exe`。

### IAgyInstaller
呼叫官方 installer。

### IPathService
- GetUserPath
- GetMachinePath
- Contains
- AddUserPath
- Backup
- Restore

### IDiagnosticService
執行所有 Checks。

### IRemediationService
將 CheckResult 對應修復動作。

### IReportService
輸出匿名化報告。

### ISystemInfoService
Windows / Architecture / Privilege。

---

## 19. Remediation Engine

不要把修復邏輯寫死在 ViewModel。

模型：

```text
Check
↓
CheckResult
↓
RemediationAction
↓
User Confirmation
↓
Execute
↓
Re-check
```

範例：

```text
AGY-002:
Executable Found

PATH-001:
FAIL

Remediation:
Add <agy directory> to User PATH

After execution:
Run PATH-001
Run AGY-001
Run AGY-003
```

---

## 20. Error Taxonomy

錯誤應分類，不只顯示 raw stderr。

### NETWORK
- DNS_FAILURE
- TLS_FAILURE
- TIMEOUT
- PROXY_BLOCKED
- ENDPOINT_UNREACHABLE

### INSTALL
- INSTALLER_DOWNLOAD_FAILED
- INSTALLER_EXECUTION_FAILED
- EXECUTION_BLOCKED
- PERMISSION_DENIED

### PATH
- PATH_NOT_CONFIGURED
- PATH_WRITE_FAILED
- PATH_STALE_SESSION
- PATH_DUPLICATE

### AGY
- AGY_NOT_FOUND
- AGY_INVALID_EXECUTABLE
- AGY_VERSION_FAILED
- AGY_LAUNCH_FAILED

### DEV
- NODE_NOT_FOUND
- NODE_VERSION_UNSUPPORTED
- NPM_NOT_FOUND
- CLASP_NOT_FOUND

使用者看到友善訊息；
詳細 code 寫入 log。

---

## 21. 測試策略

### 21.1 Unit Tests
優先測：

- PATH normalization
- PATH duplicate detection
- Version parsing
- Redaction
- CheckResult state transition
- Error classification

### 21.2 Integration Tests
- ProcessRunner
- Registry / Environment
- `where.exe`
- User PATH write + rollback
- New process environment

### 21.3 手動測試矩陣

#### CASE 01
乾淨 Windows，沒有 AGY。

預期：
- NotInstalled
- 可以安裝
- 安裝後 agy --version PASS

#### CASE 02
AGY 已安裝且 PATH 正常。

預期：
- 不進行任何修改
- InstalledWorking

#### CASE 03
`agy.exe` 存在，User PATH 沒有目錄。

預期：
- InstalledNotInPath
- 顯示「修復 PATH」
- 修復後 PASS

#### CASE 04
User PATH 已有，但程式啟動時 Current Process PATH 尚未更新。

預期：
- InstalledInUserPathButCurrentProcessStale
- 不重複寫 PATH
- 使用 fresh environment 驗證

#### CASE 05
PowerShell installer 被阻擋。

預期：
- 不修改 Execution Policy
- 顯示原因
- 可走官方 CMD fallback

#### CASE 06
無管理員權限。

預期：
- AGY 核心功能仍可工作。

#### CASE 07
學校 Proxy / Firewall 阻擋。

預期：
- 不誤判為 PATH 問題。
- Network error 明確分類。

#### CASE 08
User PATH 已含重複大小寫 / slash 不同路徑。

預期：
- 不再新增重複 path。

#### CASE 09
Node 18。

預期：
- InstalledUnsupportedVersion

#### CASE 10
Node >= 20 + clasp 正常。

預期：
- 完整開發模式全部 PASS。

---

## 22. UI 頁面

### Page 1 — Welcome
選擇模式。

### Page 2 — System Check
即時顯示 checks。

### Page 3 — Repair
只列出：
- 可以安全自動修復的項目。
- 需要使用者處理的項目。

### Page 4 — Verify
重新跑完整 smoke test。

### Page 5 — Complete
完成 / 報告 / 啟動 AGY。

### Page 6 — Diagnostics
進階資訊。

---

## 23. 設定

`settings.json`

只保存：
- UI preference
- Selected mode
- Last application version
- 是否顯示 advanced diagnostics

不得保存：
- Password
- Google credentials
- API Key
- Auth token

---

## 24. 開發階段

# Milestone 0 — Repository Bootstrap
Codex 建立：

- solution
- projects
- test projects
- docs
- CI
- `.editorconfig`
- nullable enable
- warnings
- AGENTS.md
- TASKS.md

驗收：
- `dotnet build`
- `dotnet test`
全部成功。

---

# Milestone 1 — Read-Only Diagnostic Engine

功能：
- OS
- architecture
- privilege
- PowerShell
- CMD
- `where agy`
- default path
- `agy --version`
- User PATH
- Machine PATH

此階段：
**禁止修改系統。**

驗收：
能正確區分：
- NotInstalled
- InstalledNotInPath
- InstalledWorking
- StaleEnvironment

---

# Milestone 2 — PATH Repair

功能：
- Add User PATH
- Deduplicate
- Backup
- Restore
- WM_SETTINGCHANGE
- Fresh process verification

驗收：
將「AGY 已存在、PATH 錯誤」的測試機自動修好。

---

# Milestone 3 — Official AGY Installer

功能：
- 網路 pre-check
- PowerShell official installer
- CMD fallback
- stdout / stderr
- timeout
- re-detect
- re-verify

驗收：
乾淨 Windows 可以從：
`NotInstalled`
走到：
`InstalledWorking`

---

# Milestone 4 — GUI Wizard

功能：
- Welcome
- Checks
- Repair
- Verify
- Complete
- Diagnostics

驗收：
一般教師不看 command line 也能完成流程。

---

# Milestone 5 — Diagnostic Report

功能：
- Clipboard
- txt
- redaction
- last error

驗收：
講師收到報告後，可以知道問題發生在哪一層。

---

# Milestone 6 — AI + GAS Development Profile

增加：
- Git
- Node.js >= 20
- npm
- clasp

第一版：
Node / Git 安裝是否全自動，可依測試結果再決定。

驗收：

```text
git --version
node --version
npm --version
agy --version
clasp --version
```

全部可正常執行。

---

# Milestone 7 — Release

產出：
- win-x64 self-contained portable EXE
- SHA-256
- README
- CHANGELOG
- Quick Start

建立 GitHub Release。

---

## 25. Codex + AGY 協作方式

這個專案刻意避免「兩個 Agent 同時亂改」。

### Codex：Primary Implementer
負責：
- repo scaffold
- architecture
- domain model
- WPF / MVVM
- services
- unit tests
- refactor
- CI
- documentation synchronization

### AGY：Independent Verifier / Windows Integration Agent
負責：
- 實際 Windows shell 行為驗證
- installer flow review
- PATH edge cases
- PowerShell / CMD fallback
- integration test
- smoke test
- security review
- code review
- acceptance test

### 開發迴圈

```text
PRD
↓
Codex 拆任務
↓
Codex 實作一個 Vertical Slice
↓
dotnet build / test
↓
AGY 獨立 Review
↓
AGY 執行 Windows Integration Test
↓
發現問題
↓
Codex 修正
↓
AGY Re-test
↓
通過
↓
更新 TASKS / DECISIONS
↓
Commit
```

原則：

> **Codex 不因為「程式可以編譯」就宣告功能完成。  
> AGY 不負責重新設計架構，而負責用真實 Windows 行為驗證假設。**

---

## 26. Git 工作流程

```text
main
└── develop
    ├── feat/diagnostics
    ├── feat/path-repair
    ├── feat/agy-installer
    ├── feat/gui
    └── feat/report
```

Commit 建議：

```text
feat:
fix:
test:
docs:
refactor:
chore:
```

每一 Milestone 建議至少一個 tag：

```text
v0.1.0-m1
v0.1.0-m2
...
```

---

## 27. Definition of Done

一個功能只有同時符合以下條件才算完成：

- Build PASS
- Unit Tests PASS
- Integration Tests PASS
- Manual acceptance PASS
- Error state 有處理
- Log 可追蹤
- UI 不顯示 raw exception
- 不暴露 credential
- docs 已更新
- AGY review completed

---

## 28. MVP 最終驗收情境

準備四台 VM：

### VM-A
Windows 11，完全沒安裝 AGY。

結果：
一鍵安裝 → verify PASS。

### VM-B
Windows 11，AGY 已安裝但 PATH 缺失。

結果：
自動找到 executable → 修 PATH → verify PASS。

### VM-C
PATH 正常、AGY 正常。

結果：
不修改任何設定 → PASS。

### VM-D
網路 / PowerShell 安裝被阻擋。

結果：
不誤報成功 → 顯示清楚原因 → 能輸出診斷報告。

MVP 必須四種情境全部通過。

---

## 29. 後續 Roadmap

### v0.2
- CLI headless mode
- `--diagnose`
- `--repair`
- JSON report
- Silent check
- ARM64 評估

### v0.3
- Git / Node / clasp 一鍵安裝成熟化
- Auto update
- Signed installer
- Release channel

### v0.4
- macOS support
- zsh PATH detection
- Apple Silicon

### v0.5
- 教師模式 Dashboard
- 班級診斷碼
- 學員匯出報告
- 講師快速彙整常見錯誤

不建議一開始做遠端控制。

---

## 30. 第一輪開發任務

Codex 第一輪只做 Milestone 0 + 1。

### Task 001
建立 .NET 8 Solution。

### Task 002
建立：
- App
- Core
- Windows
- Core.Tests
- Windows.Tests

### Task 003
建立 `ProcessRunner`。

### Task 004
建立 SystemInfoService。

### Task 005
建立 PathService Read-Only API。

### Task 006
建立 AgyLocator。

### Task 007
建立 checks：
- SYS-001
- SYS-002
- SYS-003
- SYS-004
- SYS-005
- AGY-001
- AGY-002
- AGY-003
- PATH-001
- PATH-002
- PATH-003

### Task 008
建立 DiagnosticService。

### Task 009
建立 Console / debug output，先不要急著做完整 GUI。

### Task 010
建立至少三組 automated tests：
- AGY missing
- AGY found but path missing
- AGY working

完成後才進 Milestone 2。

---

## 31. 給 Codex 的第一個 Prompt

```text
請閱讀 docs/PRD.md 與 AGENTS.md。

你是 AGY Ready 的 Primary Implementer。

目前只執行 Milestone 0 與 Milestone 1：
Repository Bootstrap + Read-Only Diagnostic Engine。

限制：
1. 不得修改 PATH。
2. 不得安裝 AGY。
3. 不得修改 Registry。
4. 不得要求 Administrator。
5. 所有 shell invocation 必須經 IProcessRunner。
6. domain logic 放在 Core，不可綁死 WPF。
7. Windows-specific implementation 放在 AGYReady.Windows。
8. 必須建立 unit tests。
9. 每完成一個 task 更新 TASKS.md。
10. 若設計決策與 PRD 不一致，先記錄到 docs/DECISIONS.md，不可默默改規格。

先檢視 repo，提出 Milestone 0/1 執行計畫，再開始實作。
```

---

## 32. 給 AGY 的第一個 Prompt

```text
你是 AGY Ready 的 Independent Verifier。

請閱讀：
- docs/PRD.md
- docs/ARCHITECTURE.md
- AGENTS.md
- TASKS.md

你的主要任務不是重寫整個專案，而是驗證 Codex 的實作是否符合真實 Windows 行為。

目前檢查 Milestone 0/1：

1. dotnet build 是否成功。
2. dotnet test 是否成功。
3. ProcessRunner 是否安全處理 executable / arguments。
4. AGY detection 是否同時考慮：
   - current PATH
   - where.exe
   - %LOCALAPPDATA%\agy\bin
   - User PATH
   - Machine PATH
5. 是否能區分：
   - NotInstalled
   - InstalledNotInPath
   - InstalledWorking
   - CurrentProcessPathStale
6. 是否存在 hard-coded assumption 導致 false positive。
7. 是否有 credential / privacy 風險。
8. 請提出可以重現的 edge cases 與測試方法。

若發現問題，先在 review 中描述：
- 問題
- 重現方式
- 預期結果
- 實際結果
- 建議修正

不要在沒有說明原因的情況下大規模重構 Codex 的架構。
```

---

## 33. 核心產品原則

AGY Ready 最重要的不是「幫使用者執行安裝命令」。

真正的產品價值是：

> **把 CLI 環境問題轉成可以被程式辨識的狀態，把狀態轉成明確修復動作，再用重新驗證證明問題真的解決。**

因此所有功能都應遵守：

```text
Detect
↓
Diagnose
↓
Explain
↓
Repair
↓
Verify
↓
Report
```

而不是：

```text
Run installer
↓
顯示「安裝完成」
```

這就是本產品最核心的設計邏輯。
