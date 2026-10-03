using AGYReady.Core.Abstractions;
using AGYReady.Core.Checks;
using AGYReady.Core.Models;

namespace AGYReady.Core.Diagnostics;

public sealed class DiagnosticService
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(8);
    private readonly ISystemInfoService systemInfo;
    private readonly IPathService paths;
    private readonly IAgyLocator locator;
    private readonly IProcessRunner runner;
    private readonly IEnvironmentCheck? endpointCheck;

    public DiagnosticService(ISystemInfoService systemInfo, IPathService paths,
        IAgyLocator locator, IProcessRunner runner, IEnvironmentCheck? endpointCheck = null)
    {
        this.systemInfo = systemInfo;
        this.paths = paths;
        this.locator = locator;
        this.runner = runner;
        this.endpointCheck = endpointCheck;
    }

    public async Task<DiagnosticResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var system = systemInfo.GetSnapshot();
        var location = await locator.LocateAsync(cancellationToken);
        ProcessResult? version = null;
        if (location.ResolvedPath is not null)
        {
            version = await TryRunAsync(location.ResolvedPath, ["--version"], cancellationToken);
        }

        var checks = new IEnvironmentCheck[]
        {
            new ValueCheck("SYS-001", "Windows version", () =>
                new("SYS-001", system.WindowsVersion.StartsWith("Windows", StringComparison.OrdinalIgnoreCase)
                    ? CheckStatus.Pass : CheckStatus.Fail, system.WindowsVersion)),
            new ValueCheck("SYS-002", "Architecture", () =>
                new("SYS-002", system.Architecture.Equals("X64", StringComparison.OrdinalIgnoreCase)
                    ? CheckStatus.Pass : CheckStatus.Fail, system.Architecture)),
            new ValueCheck("SYS-003", "Privilege", () =>
                new("SYS-003", CheckStatus.Pass,
                    system.IsAdministrator ? "Administrator (not required)" : "Standard user")),
            new CommandCheck("SYS-004", "PowerShell", runner, "powershell.exe", ["-NoProfile", "-Command", "$PSVersionTable.PSVersion.ToString()"]),
            new CommandCheck("SYS-005", "CMD", runner, "cmd.exe", ["/d", "/c", "ver"]),
            new ValueCheck("AGY-001", "AGY on current PATH", () =>
                new("AGY-001", location.ResolvedPath is null ? CheckStatus.Fail : CheckStatus.Pass,
                    location.ResolvedPath is null ? "Not resolved" : "Resolved", location.ResolvedPath)),
            new ValueCheck("AGY-002", "AGY executable search", () =>
                new("AGY-002", location.CandidatePath is null ? CheckStatus.Fail : CheckStatus.Pass,
                    location.CandidatePath is null ? "Not found" : "Found", location.CandidatePath)),
            new ValueCheck("AGY-003", "AGY version", () => VersionCheck(version)),
            new ValueCheck("PATH-001", "User PATH", () => PathCheck("PATH-001", location.InUserPath)),
            new ValueCheck("PATH-002", "Machine PATH", () => PathCheck("PATH-002", location.InMachinePath)),
            new ValueCheck("PATH-003", "Current process PATH", () => PathCheck("PATH-003", location.InCurrentPath))
        };

        var results = new List<CheckResult>(checks.Length);
        foreach (var check in checks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await check.RunAsync(cancellationToken));
        }

        if (endpointCheck is not null && location.CandidatePath is null)
            results.Add(await endpointCheck.RunAsync(cancellationToken));

        return new DiagnosticResult(Classify(location, version),
            location.CandidatePath, version?.Succeeded == true ? AgyVersion.Extract(version.StdOut) : null, results);
    }

    private async Task<ProcessResult> TryRunAsync(string executable, IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        try { return await runner.RunAsync(executable, args, CommandTimeout, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            return new(null, "", ex.GetType().Name, false, TimeSpan.Zero);
        }
    }

    private static CheckResult VersionCheck(ProcessResult? result) => result switch
    {
        null => new("AGY-003", CheckStatus.Fail, "Cannot run AGY from current PATH"),
        { Succeeded: true } when AgyVersion.Extract(result.StdOut) is { } value =>
            new("AGY-003", CheckStatus.Pass, $"Version {value}"),
        { Succeeded: true } => new("AGY-003", CheckStatus.Fail, "AGY launched; version not recognized"),
        { TimedOut: true } => new("AGY-003", CheckStatus.Fail, "AGY version timed out"),
        _ => new("AGY-003", CheckStatus.Fail, "AGY version failed",
            $"Exit code: {result.ExitCode?.ToString() ?? "unknown"}")
    };

    private static CheckResult PathCheck(string id, bool contains) =>
        new(id, contains ? CheckStatus.Pass : CheckStatus.Warning,
            contains ? "AGY directory is present" : "AGY directory is absent");

    public static AgyState Classify(AgyLocation location, ProcessResult? version)
    {
        if (version?.Succeeded == true && AgyVersion.Extract(version.StdOut) is not null)
            return AgyState.InstalledWorking;
        if (location.CandidatePath is null) return AgyState.NotInstalled;
        if (location.ResolvedPath is not null)
            return version?.TimedOut == true || version?.ExitCode is null
                ? AgyState.ExecutionBlocked : AgyState.ExecutableInvalid;
        if (location.InUserPath && !location.InCurrentPath)
            return AgyState.InstalledInUserPathButCurrentProcessStale;
        return AgyState.InstalledNotInPath;
    }
}
