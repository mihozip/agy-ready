using System.ComponentModel;
using AGYReady.Core.Abstractions;
using AGYReady.Windows.Environment;

namespace AGYReady.Windows.Shell;

public sealed class AgyLocator(IPathService paths, IProcessRunner runner) : IAgyLocator
{
    public async Task<AgyLocation> LocateAsync(CancellationToken cancellationToken)
    {
        var wherePath = await LocateWithWhereAsync(cancellationToken);
        var resolved = wherePath is not null && paths.ContainsDirectory(paths.CurrentPath, Path.GetDirectoryName(wherePath)!)
            ? wherePath : null;
        var candidate = resolved ?? wherePath ?? DefaultPath();
        candidate ??= FindInPath(paths.UserPath);
        candidate ??= FindInPath(paths.MachinePath);
        candidate ??= FindInPath(paths.CurrentPath);
        if (candidate is null) return new(null, null, false, false, false);

        var directory = Path.GetDirectoryName(candidate)!;
        return new(resolved, candidate,
            paths.ContainsDirectory(paths.UserPath, directory),
            paths.ContainsDirectory(paths.MachinePath, directory),
            paths.ContainsDirectory(paths.CurrentPath, directory));
    }

    private async Task<string?> LocateWithWhereAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await runner.RunAsync("where.exe", ["agy.exe"],
                TimeSpan.FromSeconds(5), cancellationToken);
            if (!result.Succeeded) return null;
            return result.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(File.Exists);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? DefaultPath()
    {
        var local = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(local)) return null;
        var path = Path.Combine(local, "agy", "bin", "agy.exe");
        return File.Exists(path) ? path : null;
    }

    private static string? FindInPath(string pathValue)
    {
        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(PathService.Normalize(directory), "agy.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or UnauthorizedAccessException)
            {
                // An invalid or inaccessible PATH entry does not hide other candidates.
            }
        }
        return null;
    }
}
