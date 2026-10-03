using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Core.Models;

namespace AGYReady.Windows.Shell;

public sealed record TerminalDiagnosticResult(
    bool PackageRegistered, bool PackageQuerySucceeded, bool AliasAvailable,
    IReadOnlyList<CheckResult> Checks);

public sealed class TerminalDiagnosticService(IProcessRunner runner,
    DevelopmentDiagnosticService development)
{
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);
    private const string PackageQuery =
        "Get-AppxPackage -Name Microsoft.WindowsTerminal -ErrorAction Stop | " +
        "Select-Object -First 1 -ExpandProperty PackageFamilyName";

    public async Task<TerminalDiagnosticResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var environment = development.CreateFreshEnvironment();
        var package = await TryRunAsync("powershell.exe",
            ["-NoProfile", "-NonInteractive", "-Command", PackageQuery],
            environment, cancellationToken);
        var alias = await TryRunAsync("where.exe", ["wt.exe"], environment, cancellationToken);
        var querySucceeded = package?.Succeeded == true;
        var registered = querySucceeded && !string.IsNullOrWhiteSpace(package!.StdOut);
        var aliasAvailable = alias?.Succeeded == true && !string.IsNullOrWhiteSpace(alias.StdOut);

        var checks = new List<CheckResult>
        {
            new("TER-001", !querySucceeded ? CheckStatus.Warning
                    : registered ? CheckStatus.Pass : CheckStatus.Fail,
                !querySucceeded ? "無法確認 Windows Terminal 套件狀態"
                    : registered ? "Windows Terminal 已為目前使用者註冊"
                    : "目前使用者未安裝 Windows Terminal"),
            new("TER-002", aliasAvailable ? CheckStatus.Pass : CheckStatus.Warning,
                aliasAvailable ? "wt.exe 命令別名可使用"
                    : "找不到 wt.exe 命令別名；這不等於右鍵選單一定缺失"),
            new("TER-003", CheckStatus.Warning,
                "「在終端中開啟」需在檔案總管的資料夾空白處人工確認")
        };
        return new(registered, querySucceeded, aliasAvailable, checks);
    }

    private async Task<ProcessResult?> TryRunAsync(string executable,
        IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
        try
        {
            return await runner.RunAsync(executable, arguments, CheckTimeout,
                cancellationToken, environment);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
