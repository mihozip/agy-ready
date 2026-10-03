using AGYReady.Windows.Environment;

namespace AGYReady.Windows.Tests;

public sealed class PathServiceTests
{
    [Fact]
    public void DetectsCaseAndTrailingSeparatorAsSameDirectory()
    {
        var service = new PathService();
        var basePath = Path.Combine(Path.GetTempPath(), "AgyReadyTest");
        var input = basePath + Path.DirectorySeparatorChar;
        Assert.True(service.ContainsDirectory(input, basePath.ToUpperInvariant()));
    }

    [Fact]
    public void DoesNotMatchPrefixDirectory()
    {
        var service = new PathService();
        var basePath = Path.Combine(Path.GetTempPath(), "AgyReadyTest");
        Assert.False(service.ContainsDirectory(basePath + "Other", basePath));
    }
}
