using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Windows.Installers;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Tests;

public sealed class TerminalInstallerTests
{
    [Fact]
    public async Task InstallsOnlyMissingPackageAndVerifiesRegistration()
    {
        var runner = new FakeRunner(packageInitiallyPresent: false);
        var development = new DevelopmentDiagnosticService(runner);
        var diagnostics = new TerminalDiagnosticService(runner, development);
        var result = await new TerminalInstaller(runner, diagnostics, development).InstallAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(1, runner.WingetCalls);
        Assert.True(result.Diagnostics.PackageRegistered);
    }

    [Fact]
    public async Task ExistingPackageDoesNotReinstall()
    {
        var runner = new FakeRunner(packageInitiallyPresent: true);
        var development = new DevelopmentDiagnosticService(runner);
        var diagnostics = new TerminalDiagnosticService(runner, development);
        var result = await new TerminalInstaller(runner, diagnostics, development).InstallAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(0, runner.WingetCalls);
    }

    [Fact]
    public async Task MissingWingetProvidesOfficialInstructions()
    {
        var runner = new FakeRunner(packageInitiallyPresent: false, wingetMissing: true);
        var development = new DevelopmentDiagnosticService(runner);
        var diagnostics = new TerminalDiagnosticService(runner, development);
        var result = await new TerminalInstaller(runner, diagnostics, development).InstallAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.OpenOfficialInstructions);
    }

    [Fact]
    public async Task SuccessfulWingetWithUnverifiablePackageDoesNotClaimFailure()
    {
        var runner = new FakeRunner(packageInitiallyPresent: false, postInstallQueryFails: true);
        var development = new DevelopmentDiagnosticService(runner);
        var diagnostics = new TerminalDiagnosticService(runner, development);
        var result = await new TerminalInstaller(runner, diagnostics, development).InstallAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.OpenOfficialInstructions);
        Assert.False(result.Diagnostics.PackageQuerySucceeded);
        Assert.Contains("無法確認", result.Message);
    }

    private sealed class FakeRunner(bool packageInitiallyPresent, bool wingetMissing = false,
        bool postInstallQueryFails = false)
        : IProcessRunner
    {
        public int WingetCalls { get; private set; }

        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            if (executable == "winget.exe")
            {
                WingetCalls++;
                Assert.Equal("Microsoft.WindowsTerminal", arguments[2]);
                if (wingetMissing) throw new Win32Exception();
                return Task.FromResult(new ProcessResult(0, "installed", "", false, TimeSpan.Zero));
            }

            var registered = packageInitiallyPresent || WingetCalls > 0 && !wingetMissing;
            var result = executable switch
            {
                "powershell.exe" when postInstallQueryFails && WingetCalls > 0 =>
                    new ProcessResult(1, "", "query failed", false, TimeSpan.Zero),
                "powershell.exe" => new ProcessResult(0,
                    registered ? "Microsoft.WindowsTerminal_8wekyb3d8bbwe" : "",
                    "", false, TimeSpan.Zero),
                "where.exe" => new ProcessResult(registered ? 0 : 1,
                    registered ? @"C:\Users\Test\AppData\Local\Microsoft\WindowsApps\wt.exe" : "",
                    "", false, TimeSpan.Zero),
                _ => throw new InvalidOperationException("Unexpected command")
            };
            return Task.FromResult(result);
        }
    }
}
