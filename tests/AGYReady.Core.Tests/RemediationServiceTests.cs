using AGYReady.Core.Abstractions;
using AGYReady.Core.Diagnostics;
using AGYReady.Core.Remediation;

namespace AGYReady.Core.Tests;

public sealed class RemediationServiceTests
{
    [Fact]
    public async Task InstallerExitAloneDoesNotCountAsSuccessfulInstallation()
    {
        var runner = new FakeRunner();
        var diagnostics = new DiagnosticService(new FakeSystem(), new FakePaths(),
            new MissingLocator(), runner);
        var service = new RemediationService(diagnostics, new FakeRepair(), new FakeInstaller());

        var outcome = await service.InstallAsync();

        Assert.False(outcome.Succeeded);
        Assert.Equal(AGYReady.Core.Models.AgyState.NotInstalled, outcome.Diagnostics.AgyState);
    }

    private sealed class FakeSystem : ISystemInfoService
    {
        public SystemSnapshot GetSnapshot() => new("Windows 11", "X64", false);
    }

    private sealed class FakePaths : IPathService
    {
        public string CurrentPath => "";
        public string UserPath => "";
        public string MachinePath => "";
        public bool ContainsDirectory(string pathValue, string directory) => false;
    }

    private sealed class MissingLocator : IAgyLocator
    {
        public Task<AgyLocation> LocateAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new AgyLocation(null, null, false, false, false));
    }

    private sealed class FakeRunner : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null) =>
            Task.FromResult(new ProcessResult(0, "ok", "", false, TimeSpan.Zero));
    }

    private sealed class FakeRepair : IPathRepairService
    {
        public bool CanRestore => false;
        public Task<PathRepairResult> RepairAsync(string executablePath, CancellationToken cancellationToken) =>
            Task.FromResult(new PathRepairResult(false, false, "not used"));
        public Task<PathRepairResult> RestoreAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new PathRepairResult(false, false, "not used"));
    }

    private sealed class FakeInstaller : IAgyInstaller
    {
        public Task<InstallerResult> InstallAsync(CancellationToken cancellationToken,
            IProgress<InstallerProgress>? progress = null) =>
            Task.FromResult(new InstallerResult(true, "Exit code 0", []));
    }
}
