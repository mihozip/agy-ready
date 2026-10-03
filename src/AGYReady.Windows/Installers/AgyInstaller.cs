using System.ComponentModel;
using AGYReady.Core.Abstractions;

namespace AGYReady.Windows.Installers;

public sealed class AgyInstaller : IAgyInstaller
{
    private static readonly HttpClient SharedClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    }) { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(5);
    private static readonly Uri PowerShellUri = new("https://antigravity.google/cli/install.ps1");
    private static readonly Uri CmdUri = new("https://antigravity.google/cli/install.cmd");
    private const int MaxScriptBytes = 1_000_000;
    private readonly IProcessRunner runner;
    private readonly IAgyLocator locator;
    private readonly HttpClient client;

    public AgyInstaller(IProcessRunner runner, IAgyLocator locator, HttpClient? client = null)
    {
        this.runner = runner;
        this.locator = locator;
        this.client = client ?? SharedClient;
    }

    public async Task<InstallerResult> InstallAsync(CancellationToken cancellationToken,
        IProgress<InstallerProgress>? progress = null)
    {
        var attempts = new List<InstallerAttempt>();
        var powerShell = await RunScriptAsync(PowerShellUri, "PowerShell", cancellationToken, progress);
        attempts.Add(powerShell);

        if (await IsBinaryPresentAsync(cancellationToken))
            return new(powerShell.ExitCode == 0 && !powerShell.TimedOut,
                "已找到 AGY 執行檔；正在驗證。", attempts);

        // A nonzero exit, timeout, or zero exit without a binary all warrant the official CMD fallback.
        var cmd = await RunScriptAsync(CmdUri, "CMD", cancellationToken, progress);
        attempts.Add(cmd);
        var found = await IsBinaryPresentAsync(cancellationToken);
        return new(found, found
            ? "已找到 AGY 執行檔；正在驗證。"
            : DescribeFailure(attempts), attempts);
    }

    private async Task<InstallerAttempt> RunScriptAsync(Uri uri, string method,
        CancellationToken cancellationToken, IProgress<InstallerProgress>? progress)
    {
        string? scriptPath = null;
        try
        {
            scriptPath = await DownloadScriptAsync(uri, cancellationToken, progress);
            progress?.Report(new($"執行官方 {method} 安裝程序"));
            var executable = method == "PowerShell" ? "powershell.exe" : "cmd.exe";
            string[] args = method == "PowerShell"
                ? ["-NoProfile", "-NonInteractive", "-File", scriptPath]
                : ["/d", "/c", scriptPath];
            var result = await runner.RunAsync(executable, args, InstallTimeout, cancellationToken);
            return new(method, result.ExitCode, result.TimedOut, result.StdOut, result.StdErr);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or
            IOException or UnauthorizedAccessException or Win32Exception)
        {
            return new(method, null, ex is TaskCanceledException, "", ex.GetType().Name);
        }
        finally
        {
            if (scriptPath is not null)
            {
                try { File.Delete(scriptPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private async Task<string> DownloadScriptAsync(Uri uri, CancellationToken cancellationToken,
        IProgress<InstallerProgress>? progress)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || uri.Host != "antigravity.google")
            throw new InvalidOperationException("Installer source is not approved.");

        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaxScriptBytes)
            throw new IOException("Installer script is too large.");
        var expectedBytes = response.Content.Headers.ContentLength;
        progress?.Report(new("下載 AGY 官方啟動腳本", expectedBytes > 0 ? 0 : null));

        var extension = Path.GetExtension(uri.AbsolutePath);
        var path = Path.Combine(Path.GetTempPath(), "AGYReady-" + Guid.NewGuid().ToString("N") + extension);
        try
        {
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var buffer = new byte[16_384];
            var total = 0;
            var lastPercent = -1;
            int count;
            while ((count = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += count;
                if (total > MaxScriptBytes) throw new IOException("Installer script is too large.");
                await target.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                if (expectedBytes > 0)
                {
                    var percent = (int)Math.Min(100, (long)total * 100 / expectedBytes.Value);
                    if (percent != lastPercent)
                    {
                        progress?.Report(new("下載 AGY 官方啟動腳本", percent));
                        lastPercent = percent;
                    }
                }
            }
            if (expectedBytes > 0 && total != expectedBytes)
                throw new IOException("Installer script download was incomplete.");
            progress?.Report(new("AGY 官方啟動腳本下載完成", 100));
            return path;
        }
        catch
        {
            try { File.Delete(path); } catch (IOException) { }
            throw;
        }
    }

    private async Task<bool> IsBinaryPresentAsync(CancellationToken cancellationToken) =>
        (await locator.LocateAsync(cancellationToken)).CandidatePath is not null;

    private static string DescribeFailure(IEnumerable<InstallerAttempt> attempts)
    {
        if (attempts.Any(attempt => attempt.TimedOut)) return "官方安裝程序逾時。";
        if (attempts.All(attempt => attempt.StdErr is "HttpRequestException" or "TaskCanceledException"))
            return "無法連線至官方安裝來源。";
        if (attempts.Any(attempt => attempt.Method == "PowerShell" &&
            (attempt.StdErr.Contains("execution policy", StringComparison.OrdinalIgnoreCase) ||
             attempt.StdErr.Contains("running scripts is disabled", StringComparison.OrdinalIgnoreCase) ||
             attempt.StdErr.Contains("PSSecurityException", StringComparison.OrdinalIgnoreCase))))
            return "PowerShell 安裝受到執行原則阻擋；CMD 備援也未完成。請聯絡設備管理員。";
        return "PowerShell 與 CMD 安裝均未產生可用的 AGY 執行檔。";
    }
}
