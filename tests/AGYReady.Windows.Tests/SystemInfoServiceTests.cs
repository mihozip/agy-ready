using AGYReady.Windows.Environment;

namespace AGYReady.Windows.Tests;

public sealed class SystemInfoServiceTests
{
    [Theory]
    [InlineData(19045, "Windows 10 (build 19045)")]
    [InlineData(21999, "Windows 10 (build 21999)")]
    [InlineData(22000, "Windows 11 (build 22000)")]
    [InlineData(22631, "Windows 11 (build 22631)")]
    public void DistinguishesWindows10And11ByBuild(int build, string expected)
    {
        Assert.Equal(expected, SystemInfoService.FormatWindowsVersion(new Version(10, 0, build)));
    }
}
