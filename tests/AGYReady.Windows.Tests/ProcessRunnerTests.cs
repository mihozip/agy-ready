using AGYReady.Windows.Processes;

namespace AGYReady.Windows.Tests;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task CapsCapturedOutputWhileAllowingProcessToFinish()
    {
        var runner = new ProcessRunner();
        var windows = OperatingSystem.IsWindows();
        var executable = windows ? "powershell.exe" : "/bin/sh";
        string[] arguments = windows
            ? ["-NoProfile", "-Command", "[Console]::Write('x' * 20000)"]
            : ["-c", "printf '%020000d' 0"];

        var result = await runner.RunAsync(executable, arguments,
            TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(16_384, result.StdOut.Length);
    }

    [Fact]
    public async Task ReportsTimeoutInsteadOfSuccess()
    {
        var runner = new ProcessRunner();
        var windows = OperatingSystem.IsWindows();
        var executable = windows ? "powershell.exe" : "/bin/sh";
        string[] arguments = windows
            ? ["-NoProfile", "-Command", "Start-Sleep -Seconds 5"]
            : ["-c", "sleep 5"];

        var result = await runner.RunAsync(executable, arguments,
            TimeSpan.FromMilliseconds(250), CancellationToken.None);

        Assert.True(result.TimedOut);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task WindowsChildReceivesExplicitFreshPath()
    {
        if (!OperatingSystem.IsWindows()) return;
        var runner = new ProcessRunner();
        var marker = @"C:\AGYReadyVerificationMarker";

        var result = await runner.RunAsync("cmd.exe", ["/d", "/c", "echo %PATH%"],
            TimeSpan.FromSeconds(5), CancellationToken.None,
            new Dictionary<string, string> { ["Path"] = marker });

        Assert.True(result.Succeeded);
        Assert.Equal(marker, result.StdOut.Trim());
    }

    [Fact]
    public async Task WindowsCmdRunsFixedCommandArgument()
    {
        if (!OperatingSystem.IsWindows()) return;
        var result = await new ProcessRunner().RunAsync("cmd.exe", ["/d", "/c", "echo AGYREADY_OK"],
            TimeSpan.FromSeconds(5), CancellationToken.None);
        Assert.True(result.Succeeded);
        Assert.Equal("AGYREADY_OK", result.StdOut.Trim());
    }
}
