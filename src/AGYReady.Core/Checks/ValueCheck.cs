using AGYReady.Core.Models;

namespace AGYReady.Core.Checks;

public sealed class ValueCheck(string id, string displayName, Func<CheckResult> evaluate) : IEnvironmentCheck
{
    public string Id => id;
    public string DisplayName => displayName;
    public Task<CheckResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(evaluate());
    }
}
