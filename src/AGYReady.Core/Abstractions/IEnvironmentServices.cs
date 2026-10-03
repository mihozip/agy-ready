namespace AGYReady.Core.Abstractions;

public sealed record SystemSnapshot(string WindowsVersion, string Architecture, bool IsAdministrator);

public interface ISystemInfoService
{
    SystemSnapshot GetSnapshot();
}

public interface IPathService
{
    string CurrentPath { get; }
    string UserPath { get; }
    string MachinePath { get; }
    bool ContainsDirectory(string pathValue, string directory);
}

public interface IAgyLocator
{
    Task<AgyLocation> LocateAsync(CancellationToken cancellationToken);
}

public sealed record AgyLocation(string? ResolvedPath, string? CandidatePath,
    bool InUserPath, bool InMachinePath, bool InCurrentPath);
