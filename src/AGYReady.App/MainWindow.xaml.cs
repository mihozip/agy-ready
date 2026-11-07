using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using AGYReady.Core.Abstractions;
using AGYReady.Core.Diagnostics;
using AGYReady.Core.Models;
using AGYReady.Core.Remediation;
using AGYReady.Windows.Environment;
using AGYReady.Windows.Installers;
using AGYReady.Windows.Processes;
using AGYReady.Windows.Shell;

namespace AGYReady.App;

public partial class MainWindow : Window
{
    private readonly DiagnosticService diagnostics;
    private readonly RemediationService remediation;
    private readonly DevelopmentDiagnosticService development;
    private readonly NodeInstaller nodeInstaller;
    private readonly ClaspInstaller claspInstaller;
    private readonly PowerShellClaspRepairService powerShellClaspRepair;
    private readonly TerminalDiagnosticService terminalDiagnostics;
    private readonly TerminalInstaller terminalInstaller;
    private readonly TerminalContextMenuService terminalMenu;
    private readonly CancellationTokenSource lifetime = new();
    private readonly DispatcherTimer activityTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch activityWatch = new();
    private DiagnosticResult? lastResult;
    private DevelopmentDiagnosticResult? lastDevelopment;
    private TerminalDiagnosticResult? lastTerminal;
    private TerminalContextMenuState terminalMenuState;
    private static bool IsWindows10 => OperatingSystem.IsWindowsVersionAtLeast(10) &&
        !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
    private bool busy;
    private int activityId;

    public MainWindow()
    {
        InitializeComponent();
        var runner = new ProcessRunner();
        var paths = new PathService();
        var locator = new AgyLocator(paths, runner);
        diagnostics = new(new SystemInfoService(), paths, locator, runner, new OfficialEndpointCheck());
        var pathRepair = new PathRepairService(paths, new UserPathStore(), new PathBackupStore(), runner);
        remediation = new(diagnostics, pathRepair, new AgyInstaller(runner, locator));
        development = new(runner, paths);
        nodeInstaller = new(runner, development);
        claspInstaller = new(runner, development);
        powerShellClaspRepair = new(runner, development);
        terminalDiagnostics = new(runner, development);
        terminalInstaller = new(runner, terminalDiagnostics, development);
        terminalMenu = TerminalContextMenuService.CreateForCurrentUser();
        activityTimer.Tick += (_, _) => ElapsedText.Text = activityWatch.Elapsed.ToString(@"mm\:ss");
        Closing += (_, _) => lifetime.Cancel();
        Loaded += async (_, _) => await RunChecksAsync();
    }

    private async void RunChecks_Click(object sender, RoutedEventArgs e) => await RunChecksAsync();

    private async void RepairPath_Click(object sender, RoutedEventArgs e)
    {
        if (lastResult?.ExecutablePath is null || busy) return;
        var answer = MessageBox.Show(this,
            $"將 AGY 目錄加入目前使用者的 PATH，完成後會重新驗證。\n\n{System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(lastResult.ExecutablePath))}",
            "確認修復 User PATH", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        await RunActionAsync(_ => remediation.RepairPathAsync(lifetime.Token),
            "正在修復 User PATH", "更新使用者環境變數，完成後會重新檢查 AGY。", "PATH 修復中…");
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        var answer = MessageBox.Show(this,
            "將從 https://antigravity.google/cli/ 下載並執行 Google 官方 AGY 安裝腳本。驗證成功後會開啟 AGY 登入與色彩設定。是否繼續？",
            "確認安裝 AGY", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        var installed = await RunActionAsync(progress => remediation.InstallAsync(lifetime.Token, progress),
            "正在下載並安裝 AGY CLI", "官方安裝程序執行中，這可能需要幾分鐘。", "AGY 安裝中…");
        if (installed && lastResult?.AgyState == AgyState.InstalledWorking)
            OpenAgySetup();
    }

    private void AgySetup_Click(object sender, RoutedEventArgs e)
    {
        if (!busy) OpenAgySetup();
    }

    private void OpenAgySetup()
    {
        if (lastResult?.AgyState != AgyState.InstalledWorking ||
            lastResult.ExecutablePath is not { } executable || !System.IO.File.Exists(executable)) return;

        try
        {
            // Keep the first-run workspace in a dedicated application directory.
            var workspace = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "AGYReady", "Onboarding");
            System.IO.Directory.CreateDirectory(workspace);
            var start = new ProcessStartInfo(executable)
            {
                UseShellExecute = true,
                WorkingDirectory = workspace
            };
            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("無法開啟 AGY 視窗。");
            OverviewText.Text = "已開啟 AGY 首次設定";
            Output.Text = "請在 AGY 視窗選擇登入方式，完成帳號登入、授權碼、色彩與顯示模式設定。工作資料夾的信任設定仍會依各資料夾個別詢問。";
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MessageBox.Show(this, "無法開啟 AGY 設定視窗。請在終端機執行 agy 完成首次設定。",
                "AGY 首次設定", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void RestorePath_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        var answer = MessageBox.Show(this,
            "將還原 AGY Ready 上次修改前的 User PATH。若 PATH 已被其他程式變更，還原會停止。是否繼續？",
            "確認還原 User PATH", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;
        await RunActionAsync(_ => remediation.RestorePathAsync(lifetime.Token),
            "正在還原 User PATH", "還原備份並重新檢查環境。", "PATH 還原中…");
    }

    private async void InstallClasp_Click(object sender, RoutedEventArgs e)
    {
        if (busy || lastDevelopment?.CanInstallClasp != true) return;
        var answer = MessageBox.Show(this,
            "將使用 npm 全域安裝 Google 的 @google/clasp，完成後執行 clasp.cmd --version 驗證。驗證通過後可按「開啟 clasp 登入視窗」自行登入 Google 帳號。是否繼續？",
            "確認安裝 clasp", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        BeginActivity("正在安裝 clasp", "npm 正在安裝 @google/clasp，完成後會驗證版本。");
        Output.Text = "正在安裝 clasp，請稍候…";
        try
        {
            var install = await claspInstaller.InstallAsync(lifetime.Token);
            ActivityDetail.Text = "安裝程序已結束，正在重新檢查環境。";
            var agy = await diagnostics.RunAsync(lifetime.Token);
            Render(agy, install.Diagnostics, install.Message,
                claspExitCode: install.ExitCode);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Output.Text = "clasp 安裝未完成，請重新檢查。";
            OverviewText.Text = "clasp 安裝未完成";
        }
        finally { SetBusy(false); }
    }

    private void LoginClasp_Click(object sender, RoutedEventArgs e)
    {
        if (busy || lastDevelopment?.ClaspWorking != true) return;

        var start = new ProcessStartInfo(DevelopmentDiagnosticService.CommandShell)
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            WorkingDirectory = System.Environment.SystemDirectory
        };
        start.ArgumentList.Add("/d");
        start.ArgumentList.Add("/k");
        start.ArgumentList.Add("clasp.cmd");
        start.ArgumentList.Add("login");
        var freshEnvironment = development.CreateFreshEnvironment();
        if (freshEnvironment is not null && freshEnvironment.TryGetValue("Path", out var freshPath))
            start.Environment["Path"] = freshPath;

        try
        {
            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("無法建立登入視窗。");
            OverviewText.Text = "已開啟 clasp 登入視窗";
            Output.Text = "請依新視窗與瀏覽器的提示完成 Google 登入。若要在 PowerShell 手動登入，請執行 clasp.cmd login。";
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MessageBox.Show(this, "無法開啟登入視窗。請在 PowerShell 執行 clasp.cmd login。",
                "clasp 登入", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void RepairPowerShellClasp_Click(object sender, RoutedEventArgs e)
    {
        if (busy || lastDevelopment?.ClaspWorking != true) return;
        var answer = MessageBox.Show(this,
            "將檢查 Windows PowerShell 是否能直接執行 clasp。若目前使用者的執行原則為 Restricted，會將 CurrentUser 設為 RemoteSigned；這會允許此帳號執行其他符合該原則的 PowerShell 指令碼。若驗證失敗會嘗試還原。是否繼續？",
            "確認修復 PowerShell clasp 指令", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        BeginActivity("正在檢查 PowerShell clasp 指令", "讀取執行原則並以新程序驗證 clasp。");
        try
        {
            var result = await powerShellClaspRepair.RepairAsync(lifetime.Token);
            OverviewText.Text = result.Succeeded ? "PowerShell clasp 指令已可使用" : "PowerShell clasp 指令尚未可使用";
            Output.Text = result.Message + (result.Changed && result.OriginalPolicy is not null
                ? $"{System.Environment.NewLine}先前的 CurrentUser 設定：{result.OriginalPolicy}。若要手動還原，請執行 Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy {result.OriginalPolicy}。"
                : "");
            MessageBox.Show(this, result.Message, "PowerShell clasp 指令",
                MessageBoxButton.OK, result.Succeeded ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            OverviewText.Text = "PowerShell clasp 指令修復未完成";
            Output.Text = "無法完成檢查或修復；請使用 clasp.cmd，並確認 PowerShell 執行原則。";
        }
        finally { SetBusy(false); }
    }

    private async void InstallNode_Click(object sender, RoutedEventArgs e)
    {
        if (busy || lastDevelopment?.NeedsNodeRepair != true) return;
        var answer = MessageBox.Show(this,
            "若 Node.js 版本不足或尚未安裝，將透過 WinGet 安裝 Node.js LTS（含 npm）。若 npm 缺失或 WinGet 無法完成，會開啟 Node.js 官方下載頁供你安裝或修復。完成後會重新檢查。是否繼續？",
            "確認安裝 Node.js 與 npm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        BeginActivity("正在安裝或修復 Node.js + npm",
            "正在處理 Node.js LTS；若需手動修復，會開啟官方下載頁。");
        Output.Text = "正在處理 Node.js 與 npm，請稍候…";
        try
        {
            var install = await nodeInstaller.InstallOrRepairAsync(lifetime.Token);
            ActivityDetail.Text = "正在重新檢查 Node.js、npm 與 clasp。";
            var agy = await diagnostics.RunAsync(lifetime.Token);
            Render(agy, install.Diagnostics, install.Message);
            if (install.OpenOfficialDownload)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(NodeInstaller.OfficialDownloadUrl)
                        { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Output.AppendText($"{System.Environment.NewLine}無法開啟瀏覽器，請手動前往 {NodeInstaller.OfficialDownloadUrl}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Output.Text = "Node.js／npm 處理未完成，請重新檢查。";
            OverviewText.Text = "Node.js／npm 處理未完成";
        }
        finally { SetBusy(false); }
    }

    private async void InstallTerminal_Click(object sender, RoutedEventArgs e)
    {
        if (busy || lastTerminal?.PackageRegistered != false) return;
        var answer = MessageBox.Show(this,
            "將透過 WinGet 安裝 Microsoft Windows Terminal，完成後重新確認套件註冊。檔案總管的右鍵選單仍需你自行查看。是否繼續？",
            "確認安裝 Windows Terminal", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        BeginActivity("正在安裝 Windows Terminal", "WinGet 正在安裝官方套件，請稍候。");
        Output.Text = "正在安裝 Windows Terminal，請稍候…";
        var completed = false;
        try
        {
            var install = await terminalInstaller.InstallAsync(lifetime.Token);
            completed = install.Succeeded;
            lastTerminal = install.Diagnostics;
            if (IsWindows10) terminalMenuState = terminalMenu.GetState();
            Render(lastResult!, lastDevelopment!, install.Message);
            if (!install.Succeeded) OverviewText.Text = "Windows Terminal 安裝尚未確認 · 請查看詳細資訊";
            if (install.OpenOfficialInstructions)
                OpenExternal(TerminalInstaller.OfficialInstructionsUrl,
                    "無法開啟 Microsoft 安裝說明，請手動前往 ");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Output.Text = "Windows Terminal 安裝未完成，請重新檢查。";
            OverviewText.Text = "Windows Terminal 安裝未完成";
        }
        finally
        {
            SetBusy(false);
            if (!completed) ActivityTitle.Text = "安裝尚未確認";
        }
    }

    private void RepairTerminalMenu_Click(object sender, RoutedEventArgs e)
    {
        if (busy || lastTerminal is null ||
            (!lastTerminal.PackageRegistered && lastTerminal.PackageQuerySucceeded)) return;
        MessageBox.Show(this,
            "將開啟 Windows Terminal 的應用程式設定。若「在終端中開啟」未出現在檔案總管，請按「修復」（不要先按「重設」）。完成後，在資料夾空白處按右鍵重新確認；必要時重新開啟檔案總管。",
            "修復終端右鍵選單", MessageBoxButton.OK, MessageBoxImage.Information);
        OpenExternal("ms-settings:appsfeatures-app?Microsoft.WindowsTerminal_8wekyb3d8bbwe",
            "無法開啟應用程式設定。請手動前往「設定 > 應用程式 > 已安裝的應用程式 > Windows Terminal > 進階選項 > 修復」。");
    }

    private async void AddTerminalMenu_Click(object sender, RoutedEventArgs e)
    {
        if (busy || !IsWindows10 || lastTerminal?.PackageRegistered != true ||
            terminalMenuState is not (TerminalContextMenuState.Missing or
                TerminalContextMenuState.LegacyTitle)) return;
        if (terminalMenuState == TerminalContextMenuState.LegacyTitle)
        {
            if (MessageBox.Show(this,
                    $"將 AGY Ready 先前建立的右鍵選單更名為「{TerminalContextMenuService.MenuTitle}」。是否繼續？",
                    "確認更新選單名稱", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;
            await ChangeTerminalMenuAsync(() => terminalMenu.UpdateTitle(), "正在更新右鍵選單名稱");
            return;
        }
        if (MessageBox.Show(this,
                $"將在目前使用者的資料夾空白處右鍵選單加入「{TerminalContextMenuService.MenuTitle}」，無須按 Shift。只建立 AGY Ready 自己的登錄鍵，可在此移除。是否繼續？",
                "確認加入免 Shift 選單", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;
        await ChangeTerminalMenuAsync(() => terminalMenu.Install(), "正在加入右鍵選單");
    }

    private async void RestoreTerminalMenu_Click(object sender, RoutedEventArgs e)
    {
        if (busy || !IsWindows10 || terminalMenuState is not
            (TerminalContextMenuState.Installed or TerminalContextMenuState.InstalledAliasMissing or
                TerminalContextMenuState.LegacyTitle)) return;
        if (MessageBox.Show(this, "將移除 AGY Ready 建立的右鍵選單項目。是否繼續？",
                "確認移除右鍵選單", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;
        await ChangeTerminalMenuAsync(() => terminalMenu.Restore(), "正在移除右鍵選單");
    }

    private async Task ChangeTerminalMenuAsync(Action change, string title)
    {
        BeginActivity(title, "更新目前使用者的 Windows 10 右鍵選單。" );
        try
        {
            await Task.Run(change, lifetime.Token);
            terminalMenuState = terminalMenu.GetState();
            Render(lastResult!, lastDevelopment!, "右鍵選單設定已更新。請重新開啟檔案總管並在資料夾空白處按右鍵確認。");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            terminalMenuState = terminalMenu.GetState();
            Render(lastResult!, lastDevelopment!, $"右鍵選單更新未完成：{ex.Message}");
        }
        finally { SetBusy(false); }
    }

    private void OpenExternal(string uri, string fallbackMessage)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Output.AppendText($"{System.Environment.NewLine}{fallbackMessage}{uri}");
        }
    }

    private void AuthorLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        OpenExternal(e.Uri.AbsoluteUri, "無法開啟連結：");
        e.Handled = true;
    }

    private async Task RunChecksAsync()
    {
        if (busy) return;
        BeginActivity("正在檢查環境", "檢查 Windows、AGY CLI、Node.js、npm 與 clasp。");
        Output.Text = "檢查中…";
        try
        {
            var result = await diagnostics.RunAsync(lifetime.Token);
            ActivityDetail.Text = "AGY 檢查完成，正在檢查開發工具。";
            var dev = await development.RunAsync(lifetime.Token);
            lastTerminal = await terminalDiagnostics.RunAsync(lifetime.Token);
            if (IsWindows10) terminalMenuState = terminalMenu.GetState();
            Render(result, dev);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Output.Text = "健檢未完成。請查看偵錯輸出。";
            OverviewText.Text = "健檢未完成";
        }
        finally { SetBusy(false); }
    }

    private async Task<bool> RunActionAsync(Func<IProgress<InstallerProgress>, Task<RemediationOutcome>> action,
        string title, string detail, string output)
    {
        var id = BeginActivity(title, detail);
        var acceptingProgress = true;
        var progress = new Progress<InstallerProgress>(update =>
        {
            if (busy && acceptingProgress && activityId == id) ShowInstallerProgress(update);
        });
        Output.Text = output;
        try
        {
            var outcome = await action(progress);
            acceptingProgress = false;
            ActivityTitle.Text = "重新檢查環境";
            ActivityDetail.Text = "操作已結束，正在重新檢查環境。";
            var dev = await development.RunAsync(lifetime.Token);
            Render(outcome.Diagnostics, dev, outcome.Message, outcome.Installer);
            return outcome.Succeeded;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Output.Text = "操作未完成。系統設定可能已部分變更，請重新檢查。";
            OverviewText.Text = "操作未完成";
            return false;
        }
        finally { SetBusy(false); }
    }

    private void Render(DiagnosticResult result, DevelopmentDiagnosticResult dev,
        string? message = null,
        AGYReady.Core.Abstractions.InstallerResult? installer = null,
        int? claspExitCode = null)
    {
        lastResult = result;
        lastDevelopment = dev;
        var allChecks = result.Checks.Concat(dev.Checks)
            .Concat(lastTerminal?.Checks ?? Array.Empty<CheckResult>()).ToList();
        if (IsWindows10)
            allChecks.Add(new CheckResult("TER-004",
                terminalMenuState == TerminalContextMenuState.Installed ? CheckStatus.Pass : CheckStatus.Warning,
                terminalMenuState switch
                {
                    TerminalContextMenuState.Installed => "AGY Ready 免 Shift 右鍵選單已註冊；仍需在檔案總管確認",
                    TerminalContextMenuState.LegacyTitle => "已找到 AGY Ready 舊版右鍵選單；可更新為新名稱",
                    TerminalContextMenuState.InstalledAliasMissing => "免 Shift 選單已註冊，但 wt.exe 別名遺失，請修復 Terminal",
                    TerminalContextMenuState.Conflict => "AGY Ready 選單鍵已被變更；為避免覆蓋，已停止管理",
                    TerminalContextMenuState.AliasMissing => "找不到 wt.exe 別名；請至設定的「應用程式執行別名」啟用後重新檢查",
                    _ => "尚未加入 AGY Ready 免 Shift 右鍵選單"
                }));
        CheckList.ItemsSource = allChecks.Select(check =>
            new DiagnosticRow(check.Status switch
            {
                CheckStatus.Pass => "通過",
                CheckStatus.Warning => "注意",
                _ => "失敗"
            }, check.CheckId, check.Summary, check.Detail)).ToList();
        AgyStatusText.Text = result.AgyState switch
        {
            AgyState.InstalledWorking => $"已安裝 · {result.Version ?? "版本未知"}",
            AgyState.NotInstalled => "尚未安裝 · 可使用官方安裝程序",
            AgyState.InstalledNotInPath or AgyState.InstalledInUserPathButCurrentProcessStale =>
                "已找到 AGY · 需要修復 PATH",
            _ => "需要檢查 · 請查看詳細結果"
        };
        ToolStatusText.Text = dev.NeedsNodeRepair ? "Node.js 或 npm 需要安裝／修復"
            : dev.ClaspWorking ? $"clasp {dev.ClaspVersion} 已可使用"
            : "Node.js 與 npm 已就緒 · 可安裝 clasp";
        ClaspLoginHint.Visibility = dev.ClaspWorking ? Visibility.Visible : Visibility.Collapsed;
        ClaspLoginButton.Visibility = dev.ClaspWorking ? Visibility.Visible : Visibility.Collapsed;
        PowerShellClaspButton.Visibility = dev.ClaspWorking ? Visibility.Visible : Visibility.Collapsed;
        TerminalStatusText.Text = lastTerminal is null ? "尚未檢查"
            : !lastTerminal.PackageQuerySucceeded ? "無法確認套件；可嘗試安裝或開啟修復設定"
            : lastTerminal.PackageRegistered ? "已安裝；右鍵選單需在檔案總管確認"
            : "尚未安裝 Windows Terminal";
        TerminalInstallButton.Visibility = lastTerminal is { PackageRegistered: false }
            ? Visibility.Visible : Visibility.Collapsed;
        TerminalRepairButton.Visibility = lastTerminal is { PackageRegistered: true } or
            { PackageQuerySucceeded: false } ? Visibility.Visible : Visibility.Collapsed;
        TerminalMenuAddButton.Visibility = IsWindows10 && lastTerminal?.PackageRegistered == true &&
            terminalMenuState is (TerminalContextMenuState.Missing or TerminalContextMenuState.AliasMissing or
                TerminalContextMenuState.LegacyTitle)
            ? Visibility.Visible : Visibility.Collapsed;
        TerminalMenuAddButton.Content = terminalMenuState == TerminalContextMenuState.LegacyTitle
            ? "更新右鍵選單名稱" : "Windows 10：加入免 Shift 選單";
        TerminalMenuRestoreButton.Visibility = IsWindows10 && terminalMenuState is
            (TerminalContextMenuState.Installed or TerminalContextMenuState.InstalledAliasMissing or
                TerminalContextMenuState.LegacyTitle)
            ? Visibility.Visible : Visibility.Collapsed;
        OverviewText.Text = message is null ? "檢查完成 · 詳細結果如下" : "操作已結束 · 詳細結果如下";
        NodeButton.Visibility = dev.NeedsNodeRepair ? Visibility.Visible : Visibility.Collapsed;
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(message)) lines.Add(message);
        if (claspExitCode is not null) lines.Add($"npm Exit Code：{claspExitCode}");
        if (installer is not null)
        {
            foreach (var attempt in installer.Attempts)
                lines.Add($"{attempt.Method}：Exit Code {attempt.ExitCode?.ToString() ?? "未知"}" +
                    (attempt.TimedOut ? "（逾時）" : ""));
        }
        lines.Add($"AGY 狀態: {result.AgyState}");
        lines.Add($"版本: {result.Version ?? "未知"}");
        lines.Add("");
        foreach (var check in result.Checks)
        {
            lines.Add($"[{check.Status}] {check.CheckId}: {check.Summary}");
            if (!string.IsNullOrWhiteSpace(check.Detail)) lines.Add($"    {check.Detail}");
        }
        lines.Add("");
        lines.Add("AI + GAS 工具");
        foreach (var check in dev.Checks)
            lines.Add($"[{check.Status}] {check.CheckId}: {check.Summary}");
        if (lastTerminal is not null)
        {
            lines.Add("");
            lines.Add("Windows Terminal 右鍵選單");
            foreach (var check in lastTerminal.Checks)
                lines.Add($"[{check.Status}] {check.CheckId}: {check.Summary}");
            if (IsWindows10)
            {
                var menuCheck = allChecks.Last();
                lines.Add($"[{menuCheck.Status}] {menuCheck.CheckId}: {menuCheck.Summary}");
            }
            lines.Add("右鍵選單是否顯示需在檔案總管的資料夾空白處確認。");
        }
        Output.Text = string.Join(System.Environment.NewLine, lines);
        Debug.WriteLine(Output.Text);
    }

    private void SetBusy(bool value)
    {
        busy = value;
        if (!value)
        {
            activityTimer.Stop();
            activityWatch.Stop();
            ActivityProgress.Visibility = Visibility.Collapsed;
            ActivityTitle.Text = "就緒";
            ActivityDetail.Text = OverviewText.Text;
            PercentText.Text = "";
            ElapsedText.Text = "";
        }
        CheckButton.IsEnabled = !value;
        RepairButton.IsEnabled = !value && lastResult?.ExecutablePath is not null &&
            lastResult.AgyState is AgyState.InstalledNotInPath or
                AgyState.InstalledInUserPathButCurrentProcessStale;
        InstallButton.IsEnabled = !value && lastResult?.AgyState == AgyState.NotInstalled;
        AgySetupButton.IsEnabled = !value && lastResult?.AgyState == AgyState.InstalledWorking;
        RestoreButton.IsEnabled = !value && remediation.CanRestorePath;
        NodeButton.IsEnabled = !value && lastDevelopment?.NeedsNodeRepair == true;
        ClaspButton.IsEnabled = !value && lastDevelopment?.CanInstallClasp == true;
        ClaspLoginButton.IsEnabled = !value && lastDevelopment?.ClaspWorking == true;
        PowerShellClaspButton.IsEnabled = !value && lastDevelopment?.ClaspWorking == true;
        TerminalInstallButton.IsEnabled = !value && lastTerminal is { PackageRegistered: false };
        TerminalRepairButton.IsEnabled = !value && lastTerminal is { PackageRegistered: true } or
            { PackageQuerySucceeded: false };
        TerminalMenuAddButton.IsEnabled = !value && IsWindows10 && lastTerminal?.PackageRegistered == true &&
            terminalMenuState is (TerminalContextMenuState.Missing or TerminalContextMenuState.LegacyTitle);
        TerminalMenuRestoreButton.IsEnabled = !value && IsWindows10 &&
            terminalMenuState is (TerminalContextMenuState.Installed or TerminalContextMenuState.InstalledAliasMissing or
                TerminalContextMenuState.LegacyTitle);
    }

    private int BeginActivity(string title, string detail)
    {
        activityId++;
        ActivityTitle.Text = title;
        ActivityDetail.Text = detail;
        OverviewText.Text = title;
        SetBusy(true);
        ActivityProgress.IsIndeterminate = true;
        ActivityProgress.Value = 0;
        ActivityProgress.Visibility = Visibility.Visible;
        PercentText.Text = "無可用百分比";
        ElapsedText.Text = "00:00";
        activityWatch.Restart();
        activityTimer.Start();
        return activityId;
    }

    private void ShowInstallerProgress(InstallerProgress update)
    {
        ActivityTitle.Text = update.Stage;
        if (update.Percent is >= 0 and <= 100)
        {
            ActivityDetail.Text = "此百分比僅代表官方啟動腳本的下載。";
            ActivityProgress.IsIndeterminate = false;
            ActivityProgress.Value = update.Percent.Value;
            PercentText.Text = $"腳本下載 {update.Percent}%";
        }
        else
        {
            ActivityDetail.Text = "此階段未提供可驗證的安裝百分比。";
            ActivityProgress.IsIndeterminate = true;
            PercentText.Text = "無可用百分比";
        }
    }
}

public sealed record DiagnosticRow(string Status, string Id, string Summary, string? Detail);
