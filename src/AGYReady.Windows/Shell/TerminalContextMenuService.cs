using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace AGYReady.Windows.Shell;

public enum TerminalContextMenuState { Missing, Installed, InstalledAliasMissing, LegacyTitle, Conflict, AliasMissing }

public sealed record TerminalMenuEntry(string DisplayName, string Command, bool Extended,
    string? Owner, bool HasOtherValuesOrSubkeys);

public interface ITerminalMenuStore
{
    TerminalMenuEntry? Read();
    void Create(string displayName, string command, string owner);
    void UpdateDisplayName(string displayName);
    void Delete();
    void NotifyChanged();
}

public sealed class TerminalContextMenuService(ITerminalMenuStore store, Func<string> aliasPath,
    Func<bool> aliasExists)
{
    public const string MenuTitle = "在此資料夾中開啟終端機畫面";
    public const string PreviousMenuTitle = "在終端中開啟 (AGY Ready)";
    public const string Owner = "AGYReady.TerminalMenu.v1";

    // The trailing dot keeps a drive-root backslash from escaping the closing quote.
    public string Command => $"\"{aliasPath()}\" -d \"%V\\.\"";

    public TerminalContextMenuState GetState()
    {
        var entry = store.Read();
        if (entry is not null)
            return IsOwnedAndUnchanged(entry, PreviousMenuTitle) ? TerminalContextMenuState.LegacyTitle
                : !IsOwnedAndUnchanged(entry, MenuTitle) ? TerminalContextMenuState.Conflict
                : aliasExists() ? TerminalContextMenuState.Installed
                : TerminalContextMenuState.InstalledAliasMissing;
        return aliasExists() ? TerminalContextMenuState.Missing
            : TerminalContextMenuState.AliasMissing;
    }

    public void Install()
    {
        if (GetState() != TerminalContextMenuState.Missing)
            throw new InvalidOperationException("選單不可新增：別名不存在，或選單鍵已存在。");
        store.Create(MenuTitle, Command, Owner);
        if (GetState() is not (TerminalContextMenuState.Installed or
            TerminalContextMenuState.InstalledAliasMissing))
            throw new IOException("無法確認右鍵選單註冊結果。");
        store.NotifyChanged();
    }

    public void Restore()
    {
        if (GetState() is not (TerminalContextMenuState.Installed or
            TerminalContextMenuState.InstalledAliasMissing or TerminalContextMenuState.LegacyTitle))
            throw new InvalidOperationException("選單已被變更，為避免覆蓋其他設定，還原已停止。");
        store.Delete();
        store.NotifyChanged();
    }

    public void UpdateTitle()
    {
        if (GetState() != TerminalContextMenuState.LegacyTitle)
            throw new InvalidOperationException("舊版選單已被變更，無法安全更新名稱。");
        store.UpdateDisplayName(MenuTitle);
        if (GetState() is not (TerminalContextMenuState.Installed or
            TerminalContextMenuState.InstalledAliasMissing))
            throw new IOException("無法確認右鍵選單名稱更新結果。");
        store.NotifyChanged();
    }

    private bool IsOwnedAndUnchanged(TerminalMenuEntry entry, string title) =>
        entry.Owner == Owner && entry.DisplayName == title &&
        string.Equals(entry.Command, Command, StringComparison.OrdinalIgnoreCase) &&
        !entry.Extended && !entry.HasOtherValuesOrSubkeys;

    public static TerminalContextMenuService CreateForCurrentUser()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows 右鍵選單僅支援 Windows。");
        var path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "wt.exe");
        return new(new RegistryTerminalMenuStore(), () => path, () => File.Exists(path));
    }
}

[SupportedOSPlatform("windows")]
public sealed class RegistryTerminalMenuStore : ITerminalMenuStore
{
    private const string KeyPath = @"Software\Classes\Directory\Background\shell\AGYReady.OpenInTerminal";

    public TerminalMenuEntry? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        if (key is null) return null;
        using var command = key.OpenSubKey("command");
        var extraValues = key.GetValueNames().Except(["", "AGYReadyOwner", "Extended"],
            StringComparer.OrdinalIgnoreCase).Any();
        var extraSubkeys = key.GetSubKeyNames().Except(["command"],
            StringComparer.OrdinalIgnoreCase).Any();
        var commandExtra = command is not null && (command.GetValueNames().Except([""],
            StringComparer.OrdinalIgnoreCase).Any() || command.SubKeyCount != 0);
        return new(key.GetValue(null) as string ?? "", command?.GetValue(null) as string ?? "",
            key.GetValueNames().Contains("Extended", StringComparer.OrdinalIgnoreCase),
            key.GetValue("AGYReadyOwner") as string,
            extraValues || extraSubkeys || commandExtra);
    }

    public void Create(string displayName, string command, string owner)
    {
        if (Read() is not null) throw new InvalidOperationException("選單鍵已存在。");
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true)
                ?? throw new IOException("無法建立右鍵選單登錄鍵。");
            key.SetValue("AGYReadyOwner", owner, RegistryValueKind.String);
            key.SetValue(null, displayName, RegistryValueKind.String);
            using var commandKey = key.CreateSubKey("command", true)
                ?? throw new IOException("無法建立右鍵選單命令。");
            commandKey.SetValue(null, command, RegistryValueKind.String);
        }
        catch
        {
            try
            {
                var partial = Read();
                if (partial is { Owner: null, DisplayName: "", Command: "", Extended: false,
                        HasOtherValuesOrSubkeys: false } ||
                    partial?.Owner == owner && !partial.Extended && !partial.HasOtherValuesOrSubkeys &&
                    (partial.DisplayName == "" || partial.DisplayName == displayName) &&
                    (partial.Command == "" || partial.Command == command))
                    Delete();
            }
            catch { /* Keep the original registry error; never remove a key we cannot inspect. */ }
            throw;
        }
    }

    public void Delete()
    {
        Registry.CurrentUser.DeleteSubKeyTree(KeyPath, false);
    }

    public void UpdateDisplayName(string displayName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true)
            ?? throw new IOException("找不到 AGY Ready 右鍵選單登錄鍵。");
        key.SetValue(null, displayName, RegistryValueKind.String);
    }

    public void NotifyChanged()
    {
        SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
}
