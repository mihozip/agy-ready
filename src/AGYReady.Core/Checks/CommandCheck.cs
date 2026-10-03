using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Core.Models;

namespace AGYReady.Core.Checks;

public sealed class CommandCheck(string id, string displayName, IProcessRunner runner,
    string executable, IReadOnlyList<string> arguments) : IEnvironmentCheck
{
    public string Id => id;
    public string DisplayName => displayName;

    public async Task<CheckResult> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await runner.RunAsync(executable, arguments, TimeSpan.FromSeconds(8), cancellationToken);
            return result.Succeeded
                ? new(Id, CheckStatus.Pass, $"{DisplayName} available")
                : new(Id, CheckStatus.Fail, $"{DisplayName} unavailable",
                    result.TimedOut ? "Timed out" : $"Exit code: {result.ExitCode?.ToString() ?? "unknown"}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            return new(Id, CheckStatus.Fail, $"{DisplayName} unavailable", ex.GetType().Name);
        }
    }
}
