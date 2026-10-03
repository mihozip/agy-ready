using AGYReady.Windows.Shell;

namespace AGYReady.Windows.Tests;

public sealed class TerminalContextMenuServiceTests
{
    private const string Alias = @"C:\Users\Test User\AppData\Local\Microsoft\WindowsApps\wt.exe";

    [Fact]
    public void InstallsVisibleMenuForFolderBackgroundAndRestoresIt()
    {
        var store = new FakeStore();
        var service = new TerminalContextMenuService(store, () => Alias, () => true);

        service.Install();

        Assert.Equal(TerminalContextMenuState.Installed, service.GetState());
        Assert.Equal(TerminalContextMenuService.MenuTitle, store.Entry!.DisplayName);
        Assert.Equal($"\"{Alias}\" -d \"%V\\.\"", store.Entry.Command);
        Assert.False(store.Entry.Extended);
        Assert.Equal(1, store.Notifications);

        service.Restore();
        Assert.Null(store.Entry);
        Assert.Equal(2, store.Notifications);
    }

    [Fact]
    public void DoesNotOverwriteExistingOrModifiedMenu()
    {
        var store = new FakeStore { Entry = new("other", "other.exe", false, null, false) };
        var service = new TerminalContextMenuService(store, () => Alias, () => true);
        Assert.Equal(TerminalContextMenuState.Conflict, service.GetState());
        Assert.Throws<InvalidOperationException>(service.Install);
        Assert.Throws<InvalidOperationException>(service.Restore);

        store.Entry = new(TerminalContextMenuService.MenuTitle, $"\"{Alias}\" -d \"%V\\.\"",
            true, TerminalContextMenuService.Owner, false);
        Assert.Equal(TerminalContextMenuState.Conflict, service.GetState());
        Assert.Throws<InvalidOperationException>(service.Restore);
        Assert.NotNull(store.Entry);
        Assert.Equal(0, store.Notifications);
    }

    [Fact]
    public void MissingAliasPreventsRegistration()
    {
        var store = new FakeStore();
        var service = new TerminalContextMenuService(store, () => Alias, () => false);
        Assert.Equal(TerminalContextMenuState.AliasMissing, service.GetState());
        Assert.Throws<InvalidOperationException>(service.Install);
        Assert.Null(store.Entry);
    }

    [Fact]
    public void CanRestoreOwnedMenuAfterAliasDisappears()
    {
        var store = new FakeStore();
        var aliasPresent = true;
        var service = new TerminalContextMenuService(store, () => Alias, () => aliasPresent);
        service.Install();
        aliasPresent = false;

        Assert.Equal(TerminalContextMenuState.InstalledAliasMissing, service.GetState());
        service.Restore();
        Assert.Null(store.Entry);
    }

    [Fact]
    public void UpdatesPreviousTitleWithoutChangingCommand()
    {
        var store = new FakeStore { Entry = new(TerminalContextMenuService.PreviousMenuTitle,
            $"\"{Alias}\" -d \"%V\\.\"", false, TerminalContextMenuService.Owner, false) };
        var service = new TerminalContextMenuService(store, () => Alias, () => true);

        Assert.Equal(TerminalContextMenuState.LegacyTitle, service.GetState());
        service.UpdateTitle();

        Assert.Equal(TerminalContextMenuState.Installed, service.GetState());
        Assert.Equal("在此資料夾中開啟終端機畫面", store.Entry!.DisplayName);
        Assert.Equal($"\"{Alias}\" -d \"%V\\.\"", store.Entry.Command);
        Assert.Equal(1, store.Notifications);
    }

    [Fact]
    public void RefusesToRenameModifiedPreviousMenu()
    {
        var store = new FakeStore { Entry = new(TerminalContextMenuService.PreviousMenuTitle,
            "other.exe", false, TerminalContextMenuService.Owner, false) };
        var service = new TerminalContextMenuService(store, () => Alias, () => true);

        Assert.Equal(TerminalContextMenuState.Conflict, service.GetState());
        Assert.Throws<InvalidOperationException>(service.UpdateTitle);
        Assert.Equal(TerminalContextMenuService.PreviousMenuTitle, store.Entry!.DisplayName);
    }

    private sealed class FakeStore : ITerminalMenuStore
    {
        public TerminalMenuEntry? Entry { get; set; }
        public int Notifications { get; private set; }
        public TerminalMenuEntry? Read() => Entry;
        public void Create(string displayName, string command, string owner) =>
            Entry = new(displayName, command, false, owner, false);
        public void UpdateDisplayName(string displayName) => Entry = Entry! with { DisplayName = displayName };
        public void Delete() => Entry = null;
        public void NotifyChanged() => Notifications++;
    }
}
