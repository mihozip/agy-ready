using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Installers;

public sealed record TerminalInstallResult(bool Succeeded, string Message,
    TerminalDiagnosticResult Diagnostics, int? ExitCode, bool OpenOfficialInstructions);

public sealed class TerminalInstaller(IProcessRunner runner,
    TerminalDiagnosticService diagnostics, DevelopmentDiagnosticService development)
{
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(10);
    public const string OfficialInstructionsUrl = "https://learn.microsoft.com/windows/terminal/install";

    public async Task<TerminalInstallResult> InstallAsync(CancellationToken cancellationToken = default)
    {
        var before = await diagnostics.RunAsync(cancellationToken);
        if (before.PackageRegistered)
            return new(true, "Windows Terminal 已安裝。若右鍵選單仍缺失，請使用「修復終端右鍵選單」。",
                before, null, false);

        ProcessResult install;
        try
        {
            install = await runner.RunAsync("winget.exe",
                ["install", "--id", "Microsoft.WindowsTerminal", "--exact", "--source", "winget",
                    "--silent", "--disable-interactivity", "--accept-package-agreements",
                    "--accept-source-agreements"],
                InstallTimeout, cancellationToken, development.CreateFreshEnvironment());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            var failed = await diagnostics.RunAsync(cancellationToken);
            return new(false, "無法啟動 WinGet。請依 Microsoft 官方說明安裝 Windows Terminal。",
                failed, null, true);
        }

        var after = await diagnostics.RunAsync(cancellationToken);
        if (after.PackageRegistered)
            return new(true, "Windows Terminal 已安裝。請在檔案總管的資料夾空白處按右鍵確認「在終端中開啟」。",
                after, install.ExitCode, false);
        if (install.Succeeded && !after.PackageQuerySucceeded)
            return new(false, "WinGet 已結束，但目前無法確認套件註冊。請稍後按「重新檢查」，再到檔案總管確認右鍵選單。",
                after, install.ExitCode, false);

        var reason = install.TimedOut ? "WinGet 安裝逾時。"
            : install.ExitCode != 0 ? $"WinGet 安裝失敗（Exit Code {install.ExitCode}）。"
            : "WinGet 已結束，但尚未確認 Windows Terminal 註冊完成。";
        return new(false, reason + " 請依 Microsoft 官方說明安裝或修復 Windows Terminal。",
            after, install.ExitCode, true);
    }
}
