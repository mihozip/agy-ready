using AGYReady.Core.Abstractions;
using AGYReady.Core.Diagnostics;
using AGYReady.Core.Models;

namespace AGYReady.Core.Tests;

public sealed class DiagnosticServiceTests
{
    [Fact]
    public async Task MissingExecutableIsNotInstalled()
    {
        var result = await Create(new(null, null, false, false, false)).RunAsync();
        Assert.Equal(AgyState.NotInstalled, result.AgyState);
        Assert.Equal(CheckStatus.Fail, result.Checks.Single(x => x.CheckId == "AGY-002").Status);
    }

    [Fact]
    public async Task FoundOutsidePathNeedsRepair()
    {
        var result = await Create(new(null, @"C:\Users\Test\agy\bin\agy.exe", false, false, false)).RunAsync();
        Assert.Equal(AgyState.InstalledNotInPath, result.AgyState);
        Assert.Equal(CheckStatus.Pass, result.Checks.Single(x => x.CheckId == "AGY-002").Status);
    }

    [Fact]
    public async Task WorkingExecutableIsVerified()
    {
        var result = await Create(new(@"C:\agy\agy.exe", @"C:\agy\agy.exe", true, false, true),
            new(0, "1.2.3", "", false, TimeSpan.Zero)).RunAsync();
        Assert.Equal(AgyState.InstalledWorking, result.AgyState);
        Assert.Equal("1.2.3", result.Version);
        Assert.Equal(CheckStatus.Pass, result.Checks.Single(x => x.CheckId == "AGY-003").Status);
    }

    [Fact]
    public async Task UserPathWithStaleProcessIsDistinct()
    {
        var result = await Create(new(null, @"C:\agy\agy.exe", true, false, false)).RunAsync();
        Assert.Equal(AgyState.InstalledInUserPathButCurrentProcessStale, result.AgyState);
    }

    [Fact]
    public async Task FailedVersionNeverCountsAsWorking()
    {
        var result = await Create(new(@"C:\agy\agy.exe", @"C:\agy\agy.exe", true, false, true),
            new(1, "", "blocked", false, TimeSpan.Zero)).RunAsync();
        Assert.Equal(AgyState.ExecutableInvalid, result.AgyState);
    }

    [Fact]
    public async Task VersionOutputDoesNotExposeArbitraryProcessText()
    {
        var result = await Create(new(@"C:\agy\agy.exe", @"C:\agy\agy.exe", true, false, true),
            new(0, "agy 1.2.3 token=private", "", false, TimeSpan.Zero)).RunAsync();
        Assert.Equal("1.2.3", result.Version);
        Assert.DoesNotContain("private", result.Checks.Single(x => x.CheckId == "AGY-003").Summary);
    }

    [Fact]
    public async Task ZeroExitWithoutVersionIsNotWorking()
    {
        var result = await Create(new(@"C:\agy\agy.exe", @"C:\agy\agy.exe", true, false, true),
            new(0, "", "", false, TimeSpan.Zero)).RunAsync();
        Assert.Equal(AgyState.ExecutableInvalid, result.AgyState);
        Assert.Equal(CheckStatus.Fail, result.Checks.Single(x => x.CheckId == "AGY-003").Status);
    }

    private static DiagnosticService Create(AgyLocation location, ProcessResult? version = null)
    {
        var runner = new FakeRunner(version);
        return new(new FakeSystem(), new FakePaths(), new FakeLocator(location), runner);
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

    private sealed class FakeLocator(AgyLocation location) : IAgyLocator
    {
        public Task<AgyLocation> LocateAsync(CancellationToken cancellationToken) => Task.FromResult(location);
    }

    private sealed class FakeRunner(ProcessResult? version) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            var result = executable == "cmd.exe" || executable == "powershell.exe"
                ? new ProcessResult(0, "ok", "", false, TimeSpan.Zero)
                : version ?? new ProcessResult(1, "", "failed", false, TimeSpan.Zero);
            return Task.FromResult(result);
        }
    }
}
