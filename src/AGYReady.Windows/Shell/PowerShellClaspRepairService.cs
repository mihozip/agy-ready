using AGYReady.Core.Abstractions;
using AGYReady.Core.Diagnostics;

namespace AGYReady.Windows.Shell;

public sealed record PowerShellClaspRepairResult(bool Succeeded, bool Changed, string Message,
    string? OriginalPolicy = null);

public sealed class PowerShellClaspRepairService(IProcessRunner runner,
    DevelopmentDiagnosticService diagnostics)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static readonly string PowerShell = OperatingSystem.IsWindows()
        ? Path.Combine(System.Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe")
        : "powershell.exe";

    public async Task<PowerShellClaspRepairResult> RepairAsync(CancellationToken cancellationToken = default)
    {
        var environment = diagnostics.CreateFreshEnvironment();
        var initial = await RunAsync("clasp --version", environment, cancellationToken);
        if (HasVersion(initial))
            return new(true, false, "PowerShell 已可直接執行 clasp。無需變更設定。");
        var resolution = await RunAsync("(Get-Command clasp -ErrorAction Stop).Definition",
            environment, cancellationToken);
        if (!IsPolicyBlock(initial) || resolution?.Succeeded != true ||
            !resolution.StdOut.Trim().EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
            return new(false, false, "無法確認 clasp 是受到 PowerShell 指令碼執行原則阻擋；未變更設定。請檢查 clasp 與 Node.js。");

        var machine = await ReadPolicyAsync("MachinePolicy", environment, cancellationToken);
        var user = await ReadPolicyAsync("UserPolicy", environment, cancellationToken);
        var process = await ReadPolicyAsync("Process", environment, cancellationToken);
        var current = await ReadPolicyAsync("CurrentUser", environment, cancellationToken);
        var local = await ReadPolicyAsync("LocalMachine", environment, cancellationToken);
        var effective = await ReadPolicyAsync(null, environment, cancellationToken);
        if (new[] { machine, user, process, current, local, effective }.Any(value => value is null))
            return new(false, false, "無法讀取 PowerShell 執行原則，未變更設定。");
        if (machine != "Undefined" || user != "Undefined" || process != "Undefined")
            return new(false, false, "PowerShell 執行原則受到群組或目前工作階段設定控制；未變更設定。請洽設備管理員。");
        if (effective != "Restricted" || current is not ("Undefined" or "Restricted") || local == "AllSigned")
            return new(false, false, "目前的 PowerShell 設定不適合自動調整；未變更設定。請使用 clasp.cmd 或洽設備管理員。");

        try
        {
            await RunAsync("Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned -Force -ErrorAction Stop",
                environment, cancellationToken);
            var applied = await ReadPolicyAsync("CurrentUser", environment, CancellationToken.None);
            if (applied == "RemoteSigned" && await ClaspWorksAsync(environment, CancellationToken.None))
                return new(true, true, "PowerShell 已可直接執行 clasp。", current);
        }
        catch (Exception) { /* A process can apply the setting before reporting cancellation or failure. */ }

        // Never overwrite a policy another process may have changed in the meantime.
        try
        {
            var observed = await ReadPolicyAsync("CurrentUser", environment, CancellationToken.None);
            if (observed == current)
                return new(false, false, "PowerShell clasp 驗證未通過；原本的執行原則未變更。");
            if (observed == "RemoteSigned")
            {
                await RunAsync($"Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy {current} -Force -ErrorAction Stop",
                    environment, CancellationToken.None);
                if (await ReadPolicyAsync("CurrentUser", environment, CancellationToken.None) == current)
                    return new(false, false, "PowerShell clasp 驗證未通過；已還原原本的執行原則。");
            }
        }
        catch (Exception) { /* Report the uncertain state and original policy below. */ }
        return new(false, true,
            $"無法確認或還原 PowerShell 執行原則。原本 CurrentUser 為 {current}；請檢查目前設定並視需要手動還原。", current);
    }

    private async Task<bool> ClaspWorksAsync(IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
        var result = await RunAsync("clasp --version", environment, cancellationToken);
        return HasVersion(result);
    }

    private static bool HasVersion(ProcessResult? result) =>
        result?.Succeeded == true &&
            (AgyVersion.Extract(result.StdOut) ?? AgyVersion.Extract(result.StdErr)) is not null;

    private static bool IsPolicyBlock(ProcessResult? result) => result is { Succeeded: false } &&
        result.StdErr.Contains("PSSecurityException", StringComparison.OrdinalIgnoreCase);

    private async Task<string?> ReadPolicyAsync(string? scope,
        IReadOnlyDictionary<string, string>? environment, CancellationToken cancellationToken)
    {
        var command = scope is null ? "Get-ExecutionPolicy" : $"Get-ExecutionPolicy -Scope {scope}";
        var result = await RunAsync(command, environment, cancellationToken);
        if (result?.Succeeded != true) return null;
        var policy = result.StdOut.Trim();
        return policy is "Undefined" or "Restricted" or "AllSigned" or "RemoteSigned" or
            "Unrestricted" or "Bypass" or "Default" ? policy : null;
    }

    private async Task<ProcessResult?> RunAsync(string command,
        IReadOnlyDictionary<string, string>? environment, CancellationToken cancellationToken)
    {
        try
        {
            return await runner.RunAsync(PowerShell,
                ["-NoProfile", "-NonInteractive", "-Command", command], Timeout,
                cancellationToken, environment);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
