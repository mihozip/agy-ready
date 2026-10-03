using System.Text.Json;

namespace AGYReady.Windows.Environment;

public sealed record PathBackup(UserPathValue Before, UserPathValue After,
    string Directory, DateTimeOffset Timestamp);

public interface IPathBackupStore
{
    bool Exists { get; }
    Task SaveAsync(PathBackup backup, CancellationToken cancellationToken);
    Task<PathBackup?> ReadAsync(CancellationToken cancellationToken);
    void Delete();
}

public sealed class PathBackupStore : IPathBackupStore
{
    private readonly string filePath;

    public PathBackupStore()
    {
        var root = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        filePath = Path.Combine(root, "AGYReady", "backups", "last-path-repair.json");
    }

    public bool Exists => File.Exists(filePath);

    public async Task SaveAsync(PathBackup backup, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(backup), cancellationToken);
            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PathBackup?> ReadAsync(CancellationToken cancellationToken)
    {
        if (!Exists) return null;
        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<PathBackup>(text);
    }

    public void Delete()
    {
        if (Exists) File.Delete(filePath);
    }
}
