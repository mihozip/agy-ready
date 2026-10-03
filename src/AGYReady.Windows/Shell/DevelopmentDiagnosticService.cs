using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Core.Diagnostics;
using AGYReady.Core.Models;

namespace AGYReady.Windows.Shell;

public sealed class DevelopmentDiagnosticService(IProcessRunner runner, IPathService? paths = null)
{
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);
    public static string CommandShell => OperatingSystem.IsWindows()
        ? Path.Combine(System.Environment.SystemDirectory, "cmd.exe") : "cmd.exe";

    public IReadOnlyDictionary<string, string>? CreateFreshEnvironment()
    {
        var freshPath = BuildFreshPath();
        return freshPath is null ? null : new Dictionary<string, string> { ["Path"] = freshPath };
    }

    public async Task<DevelopmentDiagnosticResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var environment = CreateFreshEnvironment();
        var git = await TryRunAsync("git.exe --version", environment, cancellationToken);
        var node = await TryRunAsync("node.exe --version", environment, cancellationToken);
        var npm = await TryRunAsync("npm.cmd --version", environment, cancellationToken);
        var clasp = await TryRunAsync("clasp.cmd --version", environment, cancellationToken);

        var nodeVersion = node?.Succeeded == true
            ? NodeVersion.Parse(node.StdOut) ?? NodeVersion.Parse(node.StdErr) : null;
        var nodeSupported = nodeVersion?.Major >= 20;
        var npmAvailable = npm?.Succeeded == true;
        var claspVersion = clasp?.Succeeded == true
            ? AgyVersion.Extract(clasp.StdOut) ?? AgyVersion.Extract(clasp.StdErr) : null;
        var claspWorking = claspVersion is not null;

        var checks = new List<CheckResult>
        {
            new("DEV-001", git?.Succeeded == true ? CheckStatus.Pass : CheckStatus.Warning,
                git?.Succeeded == true ? "Git 可使用" : "找不到 Git 或無法執行"),
            new("DEV-002", nodeSupported ? CheckStatus.Pass : CheckStatus.Fail,
                nodeVersion is null ? "找不到 Node.js 或無法辨識版本"
                    : nodeSupported ? $"Node.js {nodeVersion} 可使用" : $"Node.js {nodeVersion}：需要 20 以上"),
            new("DEV-003", npmAvailable ? CheckStatus.Pass : CheckStatus.Fail,
                npmAvailable ? "npm 可使用" : "找不到 npm 或無法執行"),
            new("DEV-004", claspWorking ? CheckStatus.Pass : CheckStatus.Fail,
                claspWorking ? $"clasp {claspVersion} 可使用"
                    : !nodeSupported || !npmAvailable ? "安裝 clasp 需要 Node.js 20 以上與 npm"
                    : "找不到 clasp 或無法執行")
        };
        return new(nodeSupported, npmAvailable, claspWorking,
            claspWorking ? claspVersion : null, checks);
    }

    private async Task<ProcessResult?> TryRunAsync(string command,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
        try { return await runner.RunAsync(CommandShell, ["/d", "/c", command], CheckTimeout,
            cancellationToken, environment); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private string? BuildFreshPath()
    {
        if (paths is null) return null;
        var registered = new[] { paths.MachinePath, paths.UserPath }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(System.Environment.ExpandEnvironmentVariables);
        var directories = string.Join(Path.PathSeparator, registered);
        var extras = paths.CurrentPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !paths.ContainsDirectory(directories, part));
        return string.Join(Path.PathSeparator, new[] { directories }.Concat(extras)
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
