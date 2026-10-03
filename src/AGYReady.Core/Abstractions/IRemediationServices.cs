using AGYReady.Core.Models;

namespace AGYReady.Core.Abstractions;

public sealed record PathRepairResult(bool Succeeded, bool ChangedUserPath, string Message);
public sealed record InstallerAttempt(string Method, int? ExitCode, bool TimedOut,
    string StdOut, string StdErr);
public sealed record InstallerResult(bool Succeeded, string Message,
    IReadOnlyList<InstallerAttempt> Attempts);
public sealed record InstallerProgress(string Stage, int? Percent = null);

public interface IPathRepairService
{
    bool CanRestore { get; }
    Task<PathRepairResult> RepairAsync(string executablePath, CancellationToken cancellationToken);
    Task<PathRepairResult> RestoreAsync(CancellationToken cancellationToken);
}

public interface IAgyInstaller
{
    Task<InstallerResult> InstallAsync(CancellationToken cancellationToken,
        IProgress<InstallerProgress>? progress = null);
}

public sealed record RemediationOutcome(bool Succeeded, string Message,
    DiagnosticResult Diagnostics, InstallerResult? Installer = null);
