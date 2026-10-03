namespace AGYReady.Core.Models;

public enum CheckStatus { Pass, Warning, Fail }

public sealed record CheckResult(string CheckId, CheckStatus Status, string Summary, string? Detail = null);

public enum AgyState
{
    Unknown,
    NotInstalled,
    InstalledNotInPath,
    InstalledInUserPathButCurrentProcessStale,
    InstalledWorking,
    ExecutableInvalid,
    ExecutionBlocked
}

public sealed record DiagnosticResult(
    AgyState AgyState,
    string? ExecutablePath,
    string? Version,
    IReadOnlyList<CheckResult> Checks);
