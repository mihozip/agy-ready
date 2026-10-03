using AGYReady.Core.Abstractions;
using AGYReady.Windows.Installers;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Tests;

public sealed class ClaspInstallerTests
{
    [Fact]
    public async Task SuccessfulNpmInstallRequiresClaspVersionVerification()
    {
        var runner = new FakeRunner(nodeVersion: "v20.0.0", exposeClaspAfterInstall: true);
        var result = await new ClaspInstaller(runner, new DevelopmentDiagnosticService(runner)).InstallAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(1, runner.InstallCalls);
    }

    [Fact]
    public async Task ZeroExitWithoutClaspStillFails()
    {
        var runner = new FakeRunner(nodeVersion: "v20.0.0", exposeClaspAfterInstall: false);
        var result = await new ClaspInstaller(runner, new DevelopmentDiagnosticService(runner)).InstallAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.False(result.Diagnostics.ClaspWorking);
    }

    [Fact]
    public async Task UnsupportedNodeDoesNotRunNpmInstall()
    {
        var runner = new FakeRunner(nodeVersion: "v18.20.0", exposeClaspAfterInstall: false);
        var result = await new ClaspInstaller(runner, new DevelopmentDiagnosticService(runner)).InstallAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(0, runner.InstallCalls);
    }

    private sealed class FakeRunner(string nodeVersion, bool exposeClaspAfterInstall) : IProcessRunner
    {
        public int InstallCalls { get; private set; }

        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            ProcessResult result;
            if (arguments[2] == "node.exe --version") result = Success(nodeVersion);
            else if (arguments[2] == "git.exe --version") result = Success("git version 2.0");
            else if (arguments[2] == "npm.cmd --version") result = Success("10.0.0");
            else if (arguments[2] == "clasp.cmd --version")
                result = InstallCalls > 0 && exposeClaspAfterInstall ? Success("3.4.1") : Failure();
            else if (arguments[2] == "npm.cmd install @google/clasp -g")
            {
                InstallCalls++;
                result = Success("added packages");
            }
            else throw new InvalidOperationException("Unexpected command");
            return Task.FromResult(result);
        }

        private static ProcessResult Success(string output) => new(0, output, "", false, TimeSpan.Zero);
        private static ProcessResult Failure() => new(1, "", "", false, TimeSpan.Zero);
    }
}
