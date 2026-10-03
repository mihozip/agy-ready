using System.Net;
using AGYReady.Core.Abstractions;
using AGYReady.Windows.Installers;

namespace AGYReady.Windows.Tests;

public sealed class AgyInstallerTests
{
    [Fact]
    public async Task FallsBackToOfficialCmdWhenPowerShellDoesNotInstall()
    {
        var runner = new FakeRunner();
        using var client = new HttpClient(new FakeHandler());
        var installer = new AgyInstaller(runner, new FakeLocator(runner), client);
        var progress = new CaptureProgress();

        var result = await installer.InstallAsync(CancellationToken.None, progress);

        Assert.True(result.Succeeded);
        Assert.Equal(["powershell.exe", "cmd.exe"], runner.Commands);
        Assert.Equal(2, result.Attempts.Count);
        Assert.All(runner.ScriptPaths, path => Assert.False(File.Exists(path)));
        Assert.Contains(progress.Updates, update => update.Percent == 100 &&
            update.Stage.Contains("下載", StringComparison.Ordinal));
        Assert.Contains(progress.Updates, update => update.Percent is null &&
            update.Stage.Contains("安裝程序", StringComparison.Ordinal));
    }

    private sealed class CaptureProgress : IProgress<InstallerProgress>
    {
        public List<InstallerProgress> Updates { get; } = [];
        public void Report(InstallerProgress value) => Updates.Add(value);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("antigravity.google", request.RequestUri?.Host);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("test script")
            });
        }
    }

    private sealed class FakeRunner : IProcessRunner
    {
        public List<string> Commands { get; } = [];
        public List<string> ScriptPaths { get; } = [];
        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            Commands.Add(executable);
            ScriptPaths.Add(arguments[^1]);
            Assert.True(File.Exists(arguments[^1]));
            return Task.FromResult(new ProcessResult(executable == "powershell.exe" ? 1 : 0,
                "", "", false, TimeSpan.Zero));
        }
    }

    private sealed class FakeLocator(FakeRunner runner) : IAgyLocator
    {
        public Task<AgyLocation> LocateAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new AgyLocation(null,
                runner.Commands.Count >= 2 ? @"C:\agy\bin\agy.exe" : null, false, false, false));
    }
}
