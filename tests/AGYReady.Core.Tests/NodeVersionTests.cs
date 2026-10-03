using AGYReady.Core.Diagnostics;

namespace AGYReady.Core.Tests;

public sealed class NodeVersionTests
{
    [Theory]
    [InlineData("v18.20.0", false)]
    [InlineData("v20.0.0", true)]
    [InlineData("v22.11.0", true)]
    [InlineData("unexpected output", false)]
    public void RequiresNode20OrLater(string output, bool expected)
    {
        Assert.Equal(expected, NodeVersion.IsSupported(output));
    }
}
