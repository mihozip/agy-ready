using AGYReady.Core.Abstractions;

namespace AGYReady.Windows.Environment;

public sealed class PathService : IPathService
{
    public string CurrentPath => System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process) ?? "";
    public string UserPath => System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? "";
    public string MachinePath => System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine) ?? "";

    public bool ContainsDirectory(string pathValue, string directory)
    {
        var target = Normalize(directory);
        return pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(Normalize(part), target, StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string path)
    {
        var expanded = System.Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        return expanded.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
