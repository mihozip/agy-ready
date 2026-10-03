using System.Text.RegularExpressions;

namespace AGYReady.Core.Diagnostics;

public static class AgyVersion
{
    public static string? Extract(string output)
    {
        var match = Regex.Match(output, @"\b\d+\.\d+(?:\.\d+)?(?:[-+][0-9A-Za-z.-]+)?\b",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        return match.Success ? match.Value : null;
    }
}
