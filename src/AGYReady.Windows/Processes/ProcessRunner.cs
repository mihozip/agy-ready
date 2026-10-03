using System.Diagnostics;
using AGYReady.Core.Abstractions;

namespace AGYReady.Windows.Processes;

public sealed class ProcessRunner : IProcessRunner
{
    private const int MaxOutputCharacters = 16_384;

    public async Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
        TimeSpan timeout, CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        if (environment is not null)
            foreach (var item in environment) start.Environment[item.Key] = item.Value;

        using var process = new Process { StartInfo = start };
        var watch = Stopwatch.StartNew();
        if (!process.Start()) throw new InvalidOperationException("The process did not start.");
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        var stdout = new OutputCapture();
        var stderr = new OutputCapture();
        var stdoutTask = ReadLimitedAsync(process.StandardOutput, stdout, timeoutSource.Token);
        var stderrTask = ReadLimitedAsync(process.StandardError, stderr, timeoutSource.Token);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
            await Task.WhenAll(stdoutTask, stderrTask);
            return new(process.ExitCode, stdout.Text, stderr.Text, false, watch.Elapsed);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            try { await Task.WhenAll(stdoutTask, stderrTask); }
            catch (OperationCanceledException) { }
            return new(null, stdout.Text, stderr.Text, true, watch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    private static async Task ReadLimitedAsync(StreamReader reader, OutputCapture capture,
        CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        int count;
        while ((count = await reader.ReadAsync(buffer, cancellationToken)) > 0)
        {
            capture.Append(buffer, count);
        }
    }

    private sealed class OutputCapture
    {
        private readonly System.Text.StringBuilder output = new();
        public string Text => output.ToString();
        public void Append(char[] buffer, int count)
        {
            var remaining = MaxOutputCharacters - output.Length;
            if (remaining > 0) output.Append(buffer, 0, Math.Min(count, remaining));
        }
    }
}
