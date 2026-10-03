namespace AGYReady.Core.Abstractions;

public sealed record ProcessResult(int? ExitCode, string StdOut, string StdErr, bool TimedOut, TimeSpan Duration)
{
    public bool Succeeded => ExitCode == 0 && !TimedOut;
}

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
        TimeSpan timeout, CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? environment = null);
}
