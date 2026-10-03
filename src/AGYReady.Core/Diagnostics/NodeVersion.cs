using System.Text.RegularExpressions;

namespace AGYReady.Core.Diagnostics;

public static class NodeVersion
{
    public static Version? Parse(string output)
    {
        var match = Regex.Match(output, @"\bv?(\d+)\.(\d+)\.(\d+)\b",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        return match.Success && int.TryParse(match.Groups[1].Value, out var major) &&
               int.TryParse(match.Groups[2].Value, out var minor) &&
               int.TryParse(match.Groups[3].Value, out var patch)
            ? new Version(major, minor, patch) : null;
    }

    public static bool IsSupported(string output) => Parse(output)?.Major >= 20;
}
