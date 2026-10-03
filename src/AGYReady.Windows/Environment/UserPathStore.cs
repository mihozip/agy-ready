using Microsoft.Win32;
using System.Runtime.Versioning;

namespace AGYReady.Windows.Environment;

public enum UserPathKind { String, ExpandString }

public sealed record UserPathValue(bool Existed, string Raw, UserPathKind Kind);

public interface IUserPathStore
{
    UserPathValue Read();
    void Write(UserPathValue value);
}

[SupportedOSPlatform("windows")]
public sealed class UserPathStore : IUserPathStore
{
    public UserPathValue Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey("Environment");
        if (key is null || !key.GetValueNames().Contains("Path", StringComparer.OrdinalIgnoreCase))
            return new(false, "", UserPathKind.String);

        var kind = key.GetValueKind("Path");
        if (kind is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
            throw new InvalidOperationException("User PATH has an unsupported registry value type.");
        return new(true, (string?)key.GetValue("Path", "", RegistryValueOptions.DoNotExpandEnvironmentNames) ?? "",
            kind == RegistryValueKind.ExpandString ? UserPathKind.ExpandString : UserPathKind.String);
    }

    public void Write(UserPathValue value)
    {
        using var key = Registry.CurrentUser.CreateSubKey("Environment", writable: true)
            ?? throw new UnauthorizedAccessException("Cannot open User environment settings.");
        if (value.Existed) key.SetValue("Path", value.Raw,
            value.Kind == UserPathKind.ExpandString ? RegistryValueKind.ExpandString : RegistryValueKind.String);
        else key.DeleteValue("Path", throwOnMissingValue: false);
    }
}
