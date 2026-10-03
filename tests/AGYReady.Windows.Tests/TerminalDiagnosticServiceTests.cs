using AGYReady.Core.Abstractions;
using AGYReady.Core.Models;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Tests;

public sealed class TerminalDiagnosticServiceTests
{
    [Fact]
    public async Task RegisteredPackageAndAliasAreReportedWithoutClaimingMenuVisibility()
    {
        var runner = new FakeRunner(packagePresent: true, aliasPresent: true);
        var result = await new TerminalDiagnosticService(runner,
            new DevelopmentDiagnosticService(runner)).RunAsync();

        Assert.True(result.PackageRegistered);
        Assert.True(result.AliasAvailable);
        Assert.Equal(CheckStatus.Pass, result.Checks.Single(x => x.CheckId == "TER-001").Status);
        Assert.Equal(CheckStatus.Warning, result.Checks.Single(x => x.CheckId == "TER-003").Status);
        Assert.DoesNotContain(result.Checks, x => x.Summary.Contains("右鍵選單已", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MissingPackageIsFailedPrerequisite()
    {
        var runner = new FakeRunner(packagePresent: false, aliasPresent: false);
        var result = await new TerminalDiagnosticService(runner,
            new DevelopmentDiagnosticService(runner)).RunAsync();

        Assert.False(result.PackageRegistered);
        Assert.True(result.PackageQuerySucceeded);
        Assert.Equal(CheckStatus.Fail, result.Checks.Single(x => x.CheckId == "TER-001").Status);
    }

    private sealed class FakeRunner(bool packagePresent, bool aliasPresent) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            var result = executable switch
            {
                "powershell.exe" => new ProcessResult(0,
                    packagePresent ? "Microsoft.WindowsTerminal_8wekyb3d8bbwe" : "",
                    "", false, TimeSpan.Zero),
                "where.exe" => new ProcessResult(aliasPresent ? 0 : 1,
                    aliasPresent ? @"C:\Users\Test\AppData\Local\Microsoft\WindowsApps\wt.exe" : "",
                    "", false, TimeSpan.Zero),
                _ => throw new InvalidOperationException("Unexpected command")
            };
            return Task.FromResult(result);
        }
    }
}
