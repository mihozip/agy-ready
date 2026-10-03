using AGYReady.Core.Abstractions;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Tests;

public sealed class AgyLocatorTests
{
    [Fact]
    public async Task WhereHitOutsideCurrentPathIsOnlyCandidate()
    {
        var directory = Path.Combine(Path.GetTempPath(), "agyready-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var executable = Path.Combine(directory, "agy.exe");
        File.WriteAllText(executable, "test");
        try
        {
            var paths = new FakePaths();
            var result = await new AgyLocator(paths, new FakeRunner(executable)).LocateAsync(CancellationToken.None);
            Assert.Null(result.ResolvedPath);
            Assert.Equal(executable, result.CandidatePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FakePaths : IPathService
    {
        public string CurrentPath => "";
        public string UserPath => "";
        public string MachinePath => "";
        public bool ContainsDirectory(string pathValue, string directory) => false;
    }

    private sealed class FakeRunner(string executable) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string command, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null) =>
            Task.FromResult(new ProcessResult(0, executable, "", false, TimeSpan.Zero));
    }
}
