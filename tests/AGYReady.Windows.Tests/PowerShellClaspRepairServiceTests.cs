using AGYReady.Core.Abstractions;
using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Tests;

public sealed class PowerShellClaspRepairServiceTests
{
    [Fact]
    public async Task RestrictedCurrentUserCanBeRepairedAndVerified()
    {
        var runner = new FakeRunner(verifyAfterChange: true);
        var result = await new PowerShellClaspRepairService(runner,
            new DevelopmentDiagnosticService(runner)).RepairAsync();

        Assert.True(result.Succeeded);
        Assert.True(result.Changed);
        Assert.Equal("Undefined", result.OriginalPolicy);
        Assert.Equal("RemoteSigned", runner.CurrentUserPolicy);
    }

    [Fact]
    public async Task GroupPolicyStopsBeforeChangingCurrentUser()
    {
        var runner = new FakeRunner(machinePolicy: "AllSigned");
        var result = await new PowerShellClaspRepairService(runner,
            new DevelopmentDiagnosticService(runner)).RepairAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.Changed);
        Assert.Equal("Undefined", runner.CurrentUserPolicy);
    }

    [Fact]
    public async Task FailedVerificationRestoresPreviousPolicy()
    {
        var runner = new FakeRunner(verifyAfterChange: false);
        var result = await new PowerShellClaspRepairService(runner,
            new DevelopmentDiagnosticService(runner)).RepairAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.Changed);
        Assert.Equal("Undefined", runner.CurrentUserPolicy);
        Assert.Equal(2, runner.SetCalls);
    }

    [Fact]
    public async Task VerificationExceptionAfterPolicyChangeRestoresPreviousPolicy()
    {
        var runner = new FakeRunner(throwOnVerifyAfterChange: true);
        var result = await new PowerShellClaspRepairService(runner,
            new DevelopmentDiagnosticService(runner)).RepairAsync();

        Assert.False(result.Succeeded);
        Assert.False(result.Changed);
        Assert.Equal("Undefined", runner.CurrentUserPolicy);
        Assert.Equal(2, runner.SetCalls);
    }

    [Fact]
    public async Task AlreadyWorkingCommandDoesNotChangePolicy()
    {
        var runner = new FakeRunner(commandAlreadyWorks: true);
        var result = await new PowerShellClaspRepairService(runner,
            new DevelopmentDiagnosticService(runner)).RepairAsync();

        Assert.True(result.Succeeded);
        Assert.False(result.Changed);
        Assert.Equal(0, runner.SetCalls);
    }

    [Fact]
    public async Task ProcessPolicyStopsBeforeChangingCurrentUser()
    {
        var runner = new FakeRunner(processPolicy: "Restricted");
        var result = await new PowerShellClaspRepairService(runner,
            new DevelopmentDiagnosticService(runner)).RepairAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(0, runner.SetCalls);
    }

    [Fact]
    public async Task UnrelatedClaspFailureDoesNotChangePolicy()
    {
        var runner = new FakeRunner(policyBlocked: false);
        var result = await new PowerShellClaspRepairService(runner,
            new DevelopmentDiagnosticService(runner)).RepairAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(0, runner.SetCalls);
    }

    private sealed class FakeRunner(bool verifyAfterChange = false,
        string machinePolicy = "Undefined", string processPolicy = "Undefined",
        bool commandAlreadyWorks = false, bool policyBlocked = true,
        bool throwOnVerifyAfterChange = false) : IProcessRunner
    {
        public string CurrentUserPolicy { get; private set; } = "Undefined";
        public int SetCalls { get; private set; }

        public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments,
            TimeSpan timeout, CancellationToken cancellationToken,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            var command = arguments[^1];
            if (throwOnVerifyAfterChange && command == "clasp --version" &&
                CurrentUserPolicy == "RemoteSigned")
                throw new OperationCanceledException();
            var output = command switch
            {
                "Get-ExecutionPolicy -Scope MachinePolicy" => machinePolicy,
                "Get-ExecutionPolicy -Scope UserPolicy" => "Undefined",
                "Get-ExecutionPolicy -Scope Process" => processPolicy,
                "Get-ExecutionPolicy -Scope CurrentUser" => CurrentUserPolicy,
                "Get-ExecutionPolicy -Scope LocalMachine" => "Restricted",
                "Get-ExecutionPolicy" => "Restricted",
                "(Get-Command clasp -ErrorAction Stop).Definition" => @"C:\Users\Teacher\AppData\Roaming\npm\clasp.ps1",
                "clasp --version" => "3.4.1",
                _ => ""
            };
            if (command.StartsWith("Set-ExecutionPolicy", StringComparison.Ordinal))
            {
                SetCalls++;
                CurrentUserPolicy = command.Contains("RemoteSigned", StringComparison.Ordinal)
                    ? "RemoteSigned" : "Undefined";
            }
            var succeeded = command != "clasp --version" ||
                commandAlreadyWorks || (CurrentUserPolicy == "RemoteSigned" && verifyAfterChange);
            var error = command == "clasp --version" && !succeeded
                ? policyBlocked ? "PSSecurityException" : "Node.js failed" : "";
            return Task.FromResult(new ProcessResult(succeeded ? 0 : 1, output, error, false,
                TimeSpan.Zero));
        }
    }
}
