using AGYReady.Core.Models;

namespace AGYReady.Core.Checks;

public interface IEnvironmentCheck
{
    string Id { get; }
    string DisplayName { get; }
    Task<CheckResult> RunAsync(CancellationToken cancellationToken);
}
