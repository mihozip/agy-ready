using AGYReady.Core.Abstractions;
using AGYReady.Core.Diagnostics;
using AGYReady.Core.Models;

namespace AGYReady.Core.Remediation;

public sealed class RemediationService(
    DiagnosticService diagnostics, IPathRepairService pathRepair, IAgyInstaller installer)
{
    public bool CanRestorePath => pathRepair.CanRestore;

    public async Task<RemediationOutcome> RepairPathAsync(CancellationToken cancellationToken = default)
    {
        var before = await diagnostics.RunAsync(cancellationToken);
        if (before.ExecutablePath is null)
            return new(false, "找不到 AGY 執行檔，無法修復 PATH。", before);
        if (before.AgyState == AgyState.InstalledWorking)
            return new(true, "AGY 已可正常執行。", before);
        if (before.AgyState is AgyState.ExecutableInvalid or AgyState.ExecutionBlocked)
            return new(false, "AGY 已在 PATH 中，但無法執行；修改 PATH 無法解決此問題。", before);

        var repair = await pathRepair.RepairAsync(before.ExecutablePath, cancellationToken);
        var after = await diagnostics.RunAsync(cancellationToken);
        return new(repair.Succeeded && after.AgyState == AgyState.InstalledWorking,
            repair.Message, after);
    }

    public async Task<RemediationOutcome> InstallAsync(CancellationToken cancellationToken = default,
        IProgress<InstallerProgress>? progress = null)
    {
        progress?.Report(new("檢查現有 AGY 安裝"));
        var before = await diagnostics.RunAsync(cancellationToken);
        if (before.AgyState != AgyState.NotInstalled)
            return new(before.AgyState == AgyState.InstalledWorking,
                "已找到 AGY。請先檢查或修復現有安裝。", before);

        var install = await installer.InstallAsync(cancellationToken, progress);
        progress?.Report(new("驗證 AGY 安裝結果"));
        var afterInstall = await diagnostics.RunAsync(cancellationToken);
        if (afterInstall.AgyState == AgyState.InstalledWorking)
            return new(true, "AGY 安裝並驗證成功。", afterInstall, install);

        if (afterInstall.ExecutablePath is not null &&
            afterInstall.AgyState is AgyState.InstalledNotInPath or
                AgyState.InstalledInUserPathButCurrentProcessStale)
        {
            progress?.Report(new("修復安裝後的 PATH"));
            var repair = await pathRepair.RepairAsync(afterInstall.ExecutablePath, cancellationToken);
            var verified = await diagnostics.RunAsync(cancellationToken);
            return new(repair.Succeeded && verified.AgyState == AgyState.InstalledWorking,
                verified.AgyState == AgyState.InstalledWorking
                    ? "AGY 安裝並修復 PATH 後驗證成功。"
                    : $"安裝後仍無法執行 AGY：{repair.Message}", verified, install);
        }

        return new(false, afterInstall.ExecutablePath is not null
            ? "安裝後找到 AGY 執行檔，但版本驗證未通過。請查看下方狀態。"
            : $"安裝後驗證未通過：{install.Message}", afterInstall, install);
    }

    public async Task<RemediationOutcome> RestorePathAsync(CancellationToken cancellationToken = default)
    {
        var restore = await pathRepair.RestoreAsync(cancellationToken);
        var after = await diagnostics.RunAsync(cancellationToken);
        return new(restore.Succeeded, restore.Message, after);
    }
}
