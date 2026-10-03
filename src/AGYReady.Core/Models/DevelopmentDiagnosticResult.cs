namespace AGYReady.Core.Models;

public sealed record DevelopmentDiagnosticResult(
    bool NodeSupported,
    bool NpmAvailable,
    bool ClaspWorking,
    string? ClaspVersion,
    IReadOnlyList<CheckResult> Checks)
{
    public bool CanInstallClasp => NodeSupported && NpmAvailable && !ClaspWorking;
    public bool NeedsNodeRepair => !NodeSupported || !NpmAvailable;
}
