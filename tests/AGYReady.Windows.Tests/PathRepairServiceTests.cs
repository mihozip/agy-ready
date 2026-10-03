using AGYReady.Core.Abstractions;
using AGYReady.Windows.Environment;

namespace AGYReady.Windows.Tests;

public sealed class PathRepairServiceTests
{
    [Fact]
    public async Task RepairPreservesRawExpandableUserPathAndCanRestore()
    {
        await WithExecutableAsync(async executable =>
        {
            var directory = Path.GetDirectoryName(executable)!;
            var original = new UserPathValue(true, "%USERPROFILE%\\Tools", UserPathKind.ExpandString);
            var store = new FakeStore(original);
            var backup = new FakeBackup();
            var service = new PathRepairService(new FakePaths(directory), store, backup,
                new FakeRunner(executable, succeeds: true));

            var repair = await service.RepairAsync(executable, CancellationToken.None);

            Assert.True(repair.Succeeded);
            Assert.True(repair.ChangedUserPath);
            Assert.Equal(UserPathKind.ExpandString, store.Value.Kind);
            Assert.StartsWith(original.Raw + ";", store.Value.Raw);
            Assert.True(backup.Exists);

            var restore = await service.RestoreAsync(CancellationToken.None);
            Assert.True(restore.Succeeded);
            Assert.Equal(original, store.Value);
            Assert.False(backup.Exists);
        });
    }

    [Fact]
    public async Task FailedVerificationRollsBackOnlyOwnChange()
    {
        await WithExecutableAsync(async executable =>
        {
            var original = new UserPathValue(true, @"C:\Tools", UserPathKind.String);
            var store = new FakeStore(original);
            var backup = new FakeBackup();
            var service = new PathRepairService(new FakePaths(Path.GetDirectoryName(executable)!),
                store, backup, new FakeRunner(executable, succeeds: false));

            var result = await service.RepairAsync(executable, CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(original, store.Value);
            Assert.False(backup.Exists);
        });
    }

    [Fact]
    public async Task RestoreRefusesToOverwriteLaterPathChange()
    {
        await WithExecutableAsync(async executable =>
        {
            var store = new FakeStore(new(true, @"C:\Tools", UserPathKind.String));
            var backup = new FakeBackup();
            var service = new PathRepairService(new FakePaths(Path.GetDirectoryName(executable)!),
                store, backup, new FakeRunner(executable, succeeds: true));
            Assert.True((await service.RepairAsync(executable, CancellationToken.None)).Succeeded);
            store.Value = store.Value with { Raw = store.Value.Raw + ";C:\\AnotherTool" };

            var result = await service.RestoreAsync(CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.EndsWith("AnotherTool", store.Value.Raw);
        });
    }

    private static async Task WithExecutableAsync(Func<string, Task> run)
    {
        var directory = Path.Combine(Path.GetTempPath(), "agyready-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var executable = Path.Combine(directory, "agy.exe");
        await File.WriteAllTextAsync(executable, "test");
        try { await run(executable); }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private sealed class FakePaths(string current) : IPathService
    {
        private readonly PathService comparer = new();
        public string CurrentPath => current;
        public string UserPath => "";
        public string MachinePath => "";
        public bool ContainsDirectory(string pathValue, string directory) =>
            comparer.ContainsDirectory(pathValue, directory);
    }

    private sealed class FakeStore(UserPathValue value) : IUserPathStore
    {
        public UserPathValue Value { get; set; } = value;
        public UserPathValue Read() => Value;
        public void Write(UserPathValue next) => Value = next;
    }

    private sealed class FakeBackup : IPathBackupStore
    {
        private PathBackup? value;
        public bool Exists => value is not null;
        public Task SaveAsync(PathBackup backup, CancellationToken cancellationToken)
        {
            value = backup;
            return Task.CompletedTask;
        }
        public Task<PathBackup?> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(value);
        public void Delete() => value = null;
    }

    private sealed class FakeRunner(string executable, bool succeeds) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string command, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            Assert.NotNull(environment);
            var output = Path.GetFileName(command).Equals("where.exe", StringComparison.OrdinalIgnoreCase)
                ? executable : "agy 1.2.3";
            return Task.FromResult(new ProcessResult(succeeds ? 0 : 1,
                output, "", false, TimeSpan.Zero));
        }
    }
}
