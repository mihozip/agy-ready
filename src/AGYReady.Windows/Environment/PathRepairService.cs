using AGYReady.Core.Abstractions;
using AGYReady.Core.Diagnostics;

namespace AGYReady.Windows.Environment;

public sealed class PathRepairService(
    IPathService paths, IUserPathStore userPath, IPathBackupStore backups,
    IProcessRunner runner) : IPathRepairService
{
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);
    private string? originalProcessPath;
    private string? changedProcessPath;

    public bool CanRestore => backups.Exists;

    public async Task<PathRepairResult> RepairAsync(string executablePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(executablePath) ||
            !Path.GetFileName(executablePath).Equals("agy.exe", StringComparison.OrdinalIgnoreCase) ||
            (OperatingSystem.IsWindows() && executablePath.StartsWith(@"\\", StringComparison.Ordinal)))
            return new(false, false, "AGY 執行檔路徑無效或位於網路位置。未修改 PATH。");

        var directory = Path.GetDirectoryName(Path.GetFullPath(executablePath))!;
        var before = userPath.Read();
        var addUserPath = !paths.ContainsDirectory(before.Raw, directory) &&
            !paths.ContainsDirectory(paths.MachinePath, directory);
        var after = addUserPath
            ? before with { Existed = true, Raw = Append(before.Raw, directory) }
            : before;

        if (addUserPath)
        {
            if (userPath.Read() != before)
                return new(false, false, "User PATH 已被其他程式變更，請重新檢查。未修改 PATH。");
            await backups.SaveAsync(new(before, after, directory, DateTimeOffset.UtcNow), cancellationToken);
            userPath.Write(after);
            EnvironmentNotifier.Broadcast();
        }

        var freshPath = BuildFreshPath(paths.MachinePath, after.Raw);
        var verified = await VerifyAsync(executablePath, freshPath, cancellationToken);
        if (!verified)
        {
            var restored = false;
            if (addUserPath && userPath.Read() == after)
            {
                userPath.Write(before);
                EnvironmentNotifier.Broadcast();
                backups.Delete();
                restored = true;
            }
            return new(false, addUserPath, restored
                ? "新的 PATH 環境仍無法執行 AGY；本次 User PATH 修改已還原。"
                : addUserPath
                    ? "新的 PATH 環境仍無法執行 AGY；User PATH 已有其他變更，請檢查備份後手動還原。"
                    : "新的 PATH 環境仍無法執行 AGY；未修改 User PATH。");
        }

        if (!paths.ContainsDirectory(paths.CurrentPath, directory))
        {
            originalProcessPath = paths.CurrentPath;
            changedProcessPath = Append(originalProcessPath, directory);
            System.Environment.SetEnvironmentVariable("Path", changedProcessPath, EnvironmentVariableTarget.Process);
        }
        return new(true, addUserPath, addUserPath
            ? "User PATH 已修復，並已在新的程序環境驗證 AGY。"
            : "PATH 已存在；已在新的程序環境驗證 AGY。");
    }

    public async Task<PathRepairResult> RestoreAsync(CancellationToken cancellationToken)
    {
        var backup = await backups.ReadAsync(cancellationToken);
        if (backup is null) return new(false, false, "找不到可還原的 PATH 備份。");
        if (userPath.Read() != backup.After)
            return new(false, false, "User PATH 已有其他變更，為避免覆寫，未執行還原。");

        userPath.Write(backup.Before);
        EnvironmentNotifier.Broadcast();
        if (changedProcessPath is not null && paths.CurrentPath == changedProcessPath)
            System.Environment.SetEnvironmentVariable("Path", originalProcessPath, EnvironmentVariableTarget.Process);
        backups.Delete();
        return new(true, true, "本次 User PATH 修改已還原。舊終端機可能需要重新開啟。");
    }

    private async Task<bool> VerifyAsync(string executablePath, string freshPath,
        CancellationToken cancellationToken)
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Path"] = freshPath
        };
        try
        {
            var where = OperatingSystem.IsWindows()
                ? Path.Combine(System.Environment.SystemDirectory, "where.exe") : "where.exe";
            var found = await runner.RunAsync(where, ["agy.exe"], CheckTimeout,
                cancellationToken, environment);
            if (!found.Succeeded) return false;
            var first = found.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim();
            if (first is null || !string.Equals(Path.GetFullPath(first), Path.GetFullPath(executablePath),
                    StringComparison.OrdinalIgnoreCase)) return false;

            var version = await runner.RunAsync(executablePath, ["--version"], CheckTimeout,
                cancellationToken, environment);
            return version.Succeeded && AgyVersion.Extract(version.StdOut) is not null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or
            UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    private static string Append(string existing, string directory) =>
        string.IsNullOrWhiteSpace(existing) ? directory : existing.TrimEnd(';') + ";" + directory;

    private static string BuildFreshPath(string machine, string user) =>
        string.Join(Path.PathSeparator, new[] { machine, user }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(System.Environment.ExpandEnvironmentVariables));
}
