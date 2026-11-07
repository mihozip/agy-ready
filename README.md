# AGY Ready — AGY CLI × Google Apps Script (clasp) 環境安裝與修復小幫手（標標版）

<p align="center">
  <img src="https://img.shields.io/badge/Release-v0.2.0-blue.svg" alt="Release v0.2.0" />
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20macOS-informational.svg" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-8.0%20(WPF)-512BD4.svg" alt=".NET 8" />
  <img src="https://img.shields.io/badge/Swift-5.9%2B%20(AppKit)-F05138.svg" alt="Swift" />
  <img src="https://img.shields.io/badge/Architecture-x64%20%7C%20arm64-lightgrey.svg" alt="Architecture" />
  <img src="https://img.shields.io/badge/License-MIT-green.svg" alt="License" />
</p>

> **研習現場「老師，我的不行！」的終極救星！**  
> 一鍵偵測、診斷、修復、驗證並生成去識別化報告，專為教育研習與開發者打造的跨平台 AI / GAS 環境準備工具。

---

## 🎯 為什麼需要 AGY Ready？（現場痛點）

在推廣 **AI 程式設計** 與 **Google Apps Script (GAS)** 研習的課堂上，學員真正卡關的往往**不是程式碼邏輯**，而是命令列工具（CLI）啟動前的系統環境障礙：

- 🔴 **「老師，我明明有裝，但終端機說找不到指令！」**（執行檔存在，但未加入使用者的 PATH 環境變數）。
- 🔴 **「改了環境變數，重新打指令還是不行！」**（既有終端機進程殘留舊 PATH 緩存，需要全新進程驗證）。
- 🔴 **「PowerShell 冒出紅字說禁止執行指令碼！」**（Windows Execution Policy 阻擋了全域安裝的 `clasp.ps1`）。
- 🔴 **「Node.js 版本太舊，或 npm 壞掉裝不了 clasp！」**（缺乏相容版本檢查機制）。
- 🔴 **「老師，畫面只顯示錯誤代碼，這到底是什麼意思？」**（學員無法精確描述問題，講師只能穿梭在 30～50 台電腦間逐台排查）。

**AGY Ready（標標版）** 的誕生，就是為了解決這個課前「痛點」——把繁瑣的人工 troubleshooting 轉化為標準化、可信任且安全的自動化流程！

---

## 💡 核心設計哲學：六步驟防護閉環

傳統腳本往往只是「跑完安裝指令，回傳 Exit Code 0 就宣稱成功」，但實務上常常依然無法執行。AGY Ready 堅持貫徹以下閉環：

```text
🔍 偵測 (Detect)   ➜ 自動盤點作業系統、架構、現有執行檔路徑與環境變數
       ↓
🩺 診斷 (Diagnose) ➜ 比對相容性矩陣，精確定位問題癥結（如 PATH 遺漏、進程緩存）
       ↓
💡 解釋 (Explain)  ➜ 以親切繁體中文向學員說明現狀與修復步驟，消除技術恐懼
       ↓
🛠️ 修復 (Repair)   ➜ 經使用者確認後執行「最小權限、安全可還原」的修復動作
       ↓
✅ 驗證 (Verify)   ➜ 啟動全新子進程（Fresh Process）實測執行，確認命令真正可用
       ↓
📋 報告 (Report)   ➜ 產生去識別化（已遮蔽家目錄與個人隱私）的診斷報告供講師分析
```

---

## ✨ 核心特色與亮點

### 1. 全方位環境健檢 (Full-Stack Diagnostics)
- **系統相容性**：辨識 Windows 10/11 組建版本、macOS（Apple Silicon / Intel）、CPU 架構與使用者身分。
- **AGY CLI 診斷**：主動在當前 PATH 與常見安裝目錄（如 `%LOCALAPPDATA%\Programs\agy\bin`、`~/.local/bin`）搜尋 `agy` 執行檔，確認版本與可用性。
- **全套開發鏈**：一併檢測 **Git**、**Node.js (>= 20.x)**、**npm** 以及 **Google clasp**。
- **Windows Terminal 支援**：檢查 Windows Terminal 註冊狀態，並支援為 Windows 10 加入「在此資料夾中開啟終端機」免 Shift 右鍵選單。

### 2. 安全無害的 User PATH 修復與還原
- **零管理員權限需求**：僅修復目前使用者的 `User PATH`，**絕不竄改系統級 `Machine PATH`**，不觸動機關學校的群組原則（GPO）。
- **完整備份與防呆還原**：變更前自動備份登錄檔原始字串與類型；若修復後 PATH 被其他程式更動，還原功能會智慧防呆，避免覆蓋外部修改。
- **免重開機、免登出**：自動向系統廣播 `WM_SETTINGCHANGE`，修復完立即用乾淨環境變數拉起全新進程驗證。

### 3. Google 官方正版管線安裝
- **不私自打包二進位檔**：透過 HTTPS 直接取用 Google 官方發布腳本 (`https://antigravity.google/cli/`) 與 Node.js 官方 LTS 來源。
- **官方引導式登入 (Onboarding Flow)**：安裝驗證通過後，小幫手會自動開啟獨立終端機，引導學員完成 Google 帳號授權、Token 綁定與色彩介面偏好設定。
- **工作區信任隔離**：初次引導在獨立的 Onboarding 暫存沙盒進行，不污染未來的專案資料夾，保留專案獨立 Workspace Trust 彈性。

### 4. Windows PowerShell `clasp` 執行原則安全修復
- **智慧排解腳本限制**：當全域安裝的 `clasp` 命中 `clasp.ps1` 並遭 Windows PowerShell 原則（`Restricted`）阻擋時，提供受管制的 `RemoteSigned` 提議。
- **嚴格範圍限制**：僅作用於 `CurrentUser` 範圍，不覆蓋群組原則或 `AllSigned`；若切換後驗證仍未通過，立即復原舊設定。
- **免修改原則替代方案**：提供**「開啟 clasp 登入視窗」**按鈕，以底層 `clasp.cmd login` 獨立執行，不需變更任何系統 PowerShell 原則也能順暢登入。

### 5. 隱私優先的去識別化診斷報告 (Privacy-First Report)
- **一鍵複製／存檔**：若遇特殊環境問題，學員可一鍵將檢測結果複製到剪貼簿或另存 `.txt`。
- **自動隱私過濾**：自動將使用者路徑替換為 `$HOME`，剔除全路徑、Token 與機密環境變數，安心上傳研習討論群尋求協助。

---

## 🖥️ 支援平台與軟體型態

| 平台 | 適用環境 | 交付型態 | 技術架構 | 特點 |
| :--- | :--- | :--- | :--- | :--- |
| **Windows** | Windows 10 / 11 (x64) | 單一免安裝執行檔 (`.exe`) | .NET 8 / WPF / MVVM | 綠色軟體、內建 Runtime、不需安裝 .NET |
| **macOS** | macOS 14+ (Apple Silicon / Intel) | 原生 App Bundle (`.app`) | Swift 5.9+ / AppKit | 原生極速啟動、自動配置 `~/.zprofile` |

---

## 🚀 快速下載與使用 (Quick Start)

### 📥 方式一：直接下載預編譯版本（推薦學員與講師）

請至 👉 **[GitHub Releases 最新發布頁面](https://github.com/mihozip/agy-ready/releases)** 下載最新版本：

1. **Windows 使用者**：
   - 下載 `AGYReady.App.exe`。
   - 直接雙擊執行（單一可攜式執行檔，免安裝、免安裝 .NET Runtime）。
   - 點擊「**重新檢查**」進行一鍵檢測，根據提示點擊修復或安裝按鈕。

2. **macOS 使用者**：
   - 下載 `AGYReadyMac-0.2.0-arm64.zip`。
   - 解壓縮後，將 `AGYReadyMac.app` 拖曳至「應用程式」目錄。
   - 若遇到系統未受信任提示，請按住 `Control` 點擊應用程式選擇「打開」即可。

---

### 🛠️ 方式二：從原始碼自行編譯 (Build from Source)

#### 在 Windows 上編譯與執行
需要安裝 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

```powershell
# 1. 複製專案庫
git clone https://github.com/mihozip/agy-ready.git
cd agy-ready

# 2. 建置與執行單元測試
dotnet build AGYReady.sln --configuration Release
dotnet test AGYReady.sln --configuration Release

# 3. 啟動 WPF 應用程式
dotnet run --project src/AGYReady.App/AGYReady.App.csproj

# 4. 發布單一免安裝執行檔 (Self-Contained Portable EXE)
dotnet publish src/AGYReady.App/AGYReady.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/win-x64
```

#### 在 macOS 上編譯與執行
需要 macOS 14 或更新版本，並安裝 Xcode 或 Command Line Tools (Swift 5.9+)。

```sh
# 1. 複製專案庫
git clone https://github.com/mihozip/agy-ready.git
cd agy-ready

# 2. 建置並直接執行 macOS 原生應用程式
./script/build_and_run.sh

# 參數選項：
# ./script/build_and_run.sh --build-only   # 僅編譯成 dist/macos/AGYReadyMac.app
# ./script/build_and_run.sh --verify       # 編譯並啟動驗證進程
```

---

## 📋 診斷檢測項目表 (Check Items)

小幫手包含完整診斷代碼矩陣，確保各項指標透明可追蹤：

| 代碼 | 檢測項目 | 判定標準 | 修復支援 |
| :--- | :--- | :--- | :--- |
| **SYS-001** | 作業系統相容性 | 檢查 Windows 10/11 組建版本或 macOS 14+ | 顯示系統建議 |
| **SYS-002** | 處理器架構 | 確認為 x64 或 arm64 | 提示架構限制 |
| **SYS-003** | 執行權限 | 確保在標準使用者身分下運作（不需 Admin） | 避免不當提權 |
| **SYS-004** | PowerShell 環境 | 取得 `$PSVersionTable.PSVersion` | 核心 shell 檢查 |
| **SYS-005** | CMD / zsh 環境 | 檢查預設終端機環境回應 | 核心 shell 檢查 |
| **AGY-001** | PATH 內 AGY 執行檔 | 在目前執行路徑中解析 `agy` | 呼叫 PATH 修復 |
| **AGY-002** | 實體檔案搜尋 | 掃描硬碟標準目錄尋找 `agy` 實體 | 定位既有安裝 |
| **AGY-003** | AGY 版本與可用性 | 執行 `agy --version` 確認輸出與退出碼 | 重新安裝或驗證 |
| **PATH-001** | 使用者 User PATH | 檢查目前使用者註冊表 / `~/.zprofile` | 備份並安全注入 |
| **DEV-001** | Git 環境 | 檢測 `git --version` | 提示安裝指引 |
| **DEV-002** | Node.js (>=20.0) | 檢查 Node.js 是否安裝且版本符合研習需求 | 呼叫 WinGet / 官方頁 |
| **DEV-003** | npm 套件管理器 | 檢測 `npm --version` | 提供修復指引 |
| **DEV-004** | Google clasp 工具 | 檢測全域 `@google/clasp` 可用性與版本 | 一鍵執行 npm 安裝 |
| **WT-001** | Windows Terminal | 檢測 Windows Terminal 套件註冊狀態 | WinGet 官方安裝 |

---

## 🔒 安全性與隱私承諾 (Security & Privacy)

本專案專為校園、教育訓練機構與個人開發者設計，安全第一：

1. **唯讀檢測優先**：所有健檢動作皆為唯讀操作，未經使用者明確點擊確認，絕不改動任何系統設定。
2. **無憑證截留**：本小幫手**不讀取、不收集、不儲存**使用者的 Google 帳號、密碼或 OAuth 授權權杖；所有登入操作皆由 Google 官方視窗與終端獨立處理。
3. **無提權風險**：不需要也不要求「以系統管理員身分執行」，徹底規避修改系統目錄的風險。
4. **透明開源**：所有修復指令皆透明公開於原始碼中，清楚可知執行的每一行指令。

---

## 👨‍🏫 作者與聯絡資訊 (Author & Credits)

- **作者**：苗栗縣竹南鎮永貞國民小學 **彭智標 老師** (Albert Peng)
- **Email**：[mihozip@gmail.com](mailto:mihozip@gmail.com)
- **Facebook**：[Albert Peng 臉書個人主頁](https://www.facebook.com/albert.peng.56?locale=zh_TW)
- **軟體圖示**：以「標」字印章與現代開發風格融合設計。

> 歡迎各大中小學校園教師、社群同好於研習課程中自由採用！若有功能建議或錯誤回報，歡迎隨時透過 Issue 或郵件交流。

---

## 📄 授權條款 (License)

本專案採用 [MIT License](LICENSE) 開源授權，歡迎自由分享、修改與推廣教育使用。
