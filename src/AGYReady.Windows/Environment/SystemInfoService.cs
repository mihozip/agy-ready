using System.Runtime.InteropServices;
using System.Security.Principal;
using AGYReady.Core.Abstractions;

namespace AGYReady.Windows.Environment;

public sealed class SystemInfoService : ISystemInfoService
{
    public SystemSnapshot GetSnapshot()
    {
        var windows = OperatingSystem.IsWindows()
            ? FormatWindowsVersion(System.Environment.OSVersion.Version)
            : RuntimeInformation.OSDescription;
        var admin = false;
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            admin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        return new(windows, RuntimeInformation.OSArchitecture.ToString(), admin);
    }

    public static string FormatWindowsVersion(Version version)
    {
        var name = version.Major == 10 && version.Minor == 0
            ? version.Build >= 22000 ? "Windows 11" : "Windows 10"
            : "Windows";
        return $"{name} (build {version.Build})";
    }
}
