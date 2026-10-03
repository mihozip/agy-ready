using AGYReady.Core.Abstractions;
using AGYReady.Core.Models;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Tests;

public sealed class DevelopmentDiagnosticServiceTests
{
    [Fact]
    public async Task Node18BlocksClaspInstallationEvenWhenNpmExists()
    {
        var result = await new DevelopmentDiagnosticService(new FakeRunner("v18.20.0", true, false))
            .RunAsync();

        Assert.False(result.NodeSupported);
        Assert.False(result.CanInstallClasp);
        Assert.Equal(CheckStatus.Fail, result.Checks.Single(check => check.CheckId == "DEV-002").Status);
    }

    [Fact]
    public async Task Node20AndNpmEnableClaspInstallationWhenMissing()
    {
        var result = await new DevelopmentDiagnosticService(new FakeRunner("v20.0.0", true, false))
            .RunAsync();

        Assert.True(result.CanInstallClasp);
        Assert.Equal(CheckStatus.Fail, result.Checks.Single(check => check.CheckId == "DEV-004").Status);
    }

    [Fact]
    public async Task WorkingClaspPassesAndDoesNotOfferInstall()
    {
        var result = await new DevelopmentDiagnosticService(new FakeRunner("v22.1.0", true, true))
            .RunAsync();

        Assert.True(result.ClaspWorking);
        Assert.Equal("3.4.1", result.ClaspVersion);
        Assert.False(result.CanInstallClasp);
    }

    [Fact]
    public void FreshPathPrioritizesRegisteredEntriesAndRetainsSessionExtras()
    {
        var separator = Path.PathSeparator;
        var service = new DevelopmentDiagnosticService(
            new FakeRunner("v20.0.0", true, false),
            new FakePaths("machine", "user", $"stale{separator}machine"));

        var path = service.CreateFreshEnvironment()!["Path"];

        Assert.Equal($"machine{separator}user{separator}stale", path);
    }

    private sealed class FakePaths(string machine, string user, string current) : IPathService
    {
        public string CurrentPath => current;
        public string UserPath => user;
        public string MachinePath => machine;
        public bool ContainsDirectory(string pathValue, string directory) =>
            pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Contains(directory, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class FakeRunner(string nodeVersion, bool npmAvailable, bool claspAvailable) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            var result = arguments[2] switch
            {
                "node.exe --version" => new ProcessResult(0, nodeVersion, "", false, TimeSpan.Zero),
                "git.exe --version" => new ProcessResult(0, "git version 2.0", "", false, TimeSpan.Zero),
                "npm.cmd --version" =>
                    new ProcessResult(npmAvailable ? 0 : 1, "10.0.0", "", false, TimeSpan.Zero),
                "clasp.cmd --version" =>
                    new ProcessResult(claspAvailable ? 0 : 1, "3.4.1", "", false, TimeSpan.Zero),
                _ => throw new InvalidOperationException("Unexpected command")
            };
            return Task.FromResult(result);
        }
    }
}
