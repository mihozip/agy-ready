using AGYReady.Core.Checks;
using AGYReady.Core.Models;

namespace AGYReady.Windows.Installers;

public sealed class OfficialEndpointCheck : IEnvironmentCheck
{
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    }) { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly Uri Endpoint = new("https://antigravity.google/cli/install.ps1");

    public string Id => "NET-001";
    public string DisplayName => "Official AGY installer endpoint";

    public async Task<CheckResult> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Client.GetAsync(Endpoint,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.IsSuccessStatusCode
                ? new(Id, CheckStatus.Pass, "Google 安裝來源可連線")
                : new(Id, CheckStatus.Warning, "Google 安裝來源無法連線",
                    $"HTTP {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(Id, CheckStatus.Warning, "Google 安裝來源無法連線", ex.GetType().Name);
        }
    }
}
