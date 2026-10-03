using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Windows.Installers;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Tests;

public sealed class NodeInstallerTests
{
    [Fact]
    public async Task MissingNodeInstallsLtsAndRequiresNodeAndNpmVerification()
    {
        var runner = new FakeRunner(null, false, true);
        var result = await new NodeInstaller(runner, new DevelopmentDiagnosticService(runner))
            .InstallOrRepairAsync();

        Assert.True(result.Succeeded);
        Assert.False(result.OpenOfficialDownload);
        Assert.Equal(1, runner.WingetCalls);
        Assert.True(result.Diagnostics.CanInstallClasp);
    }

    [Fact]
    public async Task ExistingNodeWithoutNpmOpensManualRepairWithoutWinget()
    {
        var runner = new FakeRunner("v22.0.0", false, true);
        var result = await new NodeInstaller(runner, new DevelopmentDiagnosticService(runner))
            .InstallOrRepairAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.OpenOfficialDownload);
        Assert.Equal(0, runner.WingetCalls);
    }

    [Fact]
    public async Task WingetSuccessWithoutVerifiedNpmStillOpensOfficialDownload()
    {
        var runner = new FakeRunner(null, false, false);
        var result = await new NodeInstaller(runner, new DevelopmentDiagnosticService(runner))
            .InstallOrRepairAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.OpenOfficialDownload);
    }

    [Fact]
    public async Task MissingWingetOpensOfficialDownload()
    {
        var runner = new FakeRunner(null, true, false, wingetMissing: true);
        var result = await new NodeInstaller(runner, new DevelopmentDiagnosticService(runner))
            .InstallOrRepairAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.OpenOfficialDownload);
    }

    private sealed class FakeRunner(string? nodeVersion, bool npmAvailable,
        bool installExposesTools, bool wingetMissing = false) : IProcessRunner
    {
        public int WingetCalls { get; private set; }

        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            if (executable == "winget.exe")
            {
                WingetCalls++;
                Assert.Equal("OpenJS.NodeJS.LTS", arguments[2]);
                Assert.Contains("--silent", arguments);
                Assert.Contains("--disable-interactivity", arguments);
                if (wingetMissing) throw new Win32Exception();
                return Task.FromResult(Success("installed"));
            }

            var available = WingetCalls > 0 && installExposesTools;
            var result = arguments[2] switch
            {
                "git.exe --version" => Success("git version 2.0"),
                "node.exe --version" => nodeVersion is not null ? Success(nodeVersion)
                    : available ? Success("v24.0.0") : Failure(),
                "npm.cmd --version" => npmAvailable || available ? Success("10.0.0") : Failure(),
                "clasp.cmd --version" => Failure(),
                _ => throw new InvalidOperationException("Unexpected command")
            };
            return Task.FromResult(result);
        }

        private static ProcessResult Success(string output) => new(0, output, "", false, TimeSpan.Zero);
        private static ProcessResult Failure() => new(1, "", "", false, TimeSpan.Zero);
    }
}
