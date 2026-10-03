using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Core.Models;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Installers;

public sealed record NodeInstallResult(
    bool Succeeded, string Message, DevelopmentDiagnosticResult Diagnostics,
    int? ExitCode, bool OpenOfficialDownload);

public sealed class NodeInstaller(IProcessRunner runner, DevelopmentDiagnosticService diagnostics)
{
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(10);
    public const string OfficialDownloadUrl = "https://nodejs.org/en/download";

    public async Task<NodeInstallResult> InstallOrRepairAsync(CancellationToken cancellationToken = default)
    {
        var before = await diagnostics.RunAsync(cancellationToken);
        if (!before.NeedsNodeRepair)
            return new(true, "Node.js 與 npm 已可使用，可安裝 clasp。", before, null, false);

        // An existing Node installation without npm needs its installer repaired. A new
        // WinGet install may only report that the package is already installed.
        if (before.NodeSupported)
            return new(false, ManualRepairMessage(), before, null, true);

        ProcessResult install;
        try
        {
            install = await runner.RunAsync("winget.exe",
                ["install", "--id", "OpenJS.NodeJS.LTS", "--exact", "--source", "winget",
                    "--silent", "--disable-interactivity", "--accept-package-agreements",
                    "--accept-source-agreements"],
                InstallTimeout, cancellationToken, diagnostics.CreateFreshEnvironment());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            var afterFailure = await diagnostics.RunAsync(cancellationToken);
            return new(false, "無法啟動 WinGet。請從 Node.js 官網下載 LTS Windows Installer，安裝時保留 npm 與 PATH 選項，完成後按「重新檢查」。",
                afterFailure, null, true);
        }

        var after = await diagnostics.RunAsync(cancellationToken);
        if (after.NodeSupported && after.NpmAvailable)
            return new(true, "Node.js 與 npm 安裝完成並通過驗證，現在可以安裝 clasp。", after, install.ExitCode, false);

        var reason = install.TimedOut ? "WinGet 安裝逾時。"
            : install.ExitCode != 0 ? $"WinGet 安裝失敗（Exit Code {install.ExitCode}）。"
            : "WinGet 已結束，但 Node.js 或 npm 尚未通過驗證。";
        return new(false, reason + " 請從 Node.js 官網下載 LTS Windows Installer；若已安裝，執行安裝程式的修復功能。確認 npm 與 PATH 選項後按「重新檢查」。",
            after, install.ExitCode, true);
    }

    private static string ManualRepairMessage() =>
        "Node.js 已可使用，但 npm 無法執行。請從 Node.js 官網下載 LTS Windows Installer，執行修復或重新安裝，確認 npm 與 PATH 選項，完成後按「重新檢查」。";
}
