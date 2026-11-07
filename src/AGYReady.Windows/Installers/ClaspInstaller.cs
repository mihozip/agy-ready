using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Core.Models;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Installers;

public sealed record ClaspInstallResult(bool Succeeded, string Message,
    DevelopmentDiagnosticResult Diagnostics, int? ExitCode, bool TimedOut);

public sealed class ClaspInstaller(IProcessRunner runner, DevelopmentDiagnosticService diagnostics)
{
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(5);

    public async Task<ClaspInstallResult> InstallAsync(CancellationToken cancellationToken = default)
    {
        var before = await diagnostics.RunAsync(cancellationToken);
        if (!before.NodeSupported || !before.NpmAvailable)
            return new(false, "請先安裝 Node.js 20 以上及 npm，再安裝 clasp。", before, null, false);
        if (before.ClaspWorking)
            return new(true, "clasp 已可正常使用。可按「開啟 clasp 登入視窗」登入；若要在 PowerShell 直接輸入 clasp，請按「修復 PowerShell clasp 指令」。", before, null, false);

        ProcessResult? install;
        try
        {
            install = await runner.RunAsync(DevelopmentDiagnosticService.CommandShell,
                ["/d", "/c", "npm.cmd install @google/clasp -g"],
                InstallTimeout, cancellationToken, diagnostics.CreateFreshEnvironment());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            var failed = await diagnostics.RunAsync(cancellationToken);
            return new(false, "無法啟動 npm 安裝程序。", failed, null, false);
        }

        var after = await diagnostics.RunAsync(cancellationToken);
        var succeeded = install.Succeeded && after.ClaspWorking;
        var message = succeeded ? "clasp 安裝完成，版本驗證通過。可按「開啟 clasp 登入視窗」登入；若要在 PowerShell 直接輸入 clasp，請按「修復 PowerShell clasp 指令」。"
            : install.TimedOut ? "clasp 安裝程序逾時，請檢查網路後重試。"
            : install.ExitCode != 0 ? "npm 安裝 clasp 失敗；請檢查網路或全域安裝權限。"
            : "npm 安裝已結束，但目前程序找不到可執行的 clasp。請檢查 npm 全域目錄是否在 PATH。";
        return new(succeeded, message, after, install.ExitCode, install.TimedOut);
    }
}
