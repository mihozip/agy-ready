using System.Runtime.InteropServices;

namespace AGYReady.Windows.Environment;

internal static class EnvironmentNotifier
{
    private const uint WmSettingChange = 0x001A;
    private const uint SmtoAbortIfHung = 0x0002;

    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr window, uint message,
        IntPtr wParam, string lParam, uint flags, uint timeout, out IntPtr result);

    public static void Broadcast()
    {
        if (OperatingSystem.IsWindows())
            _ = SendMessageTimeout(new IntPtr(0xffff), WmSettingChange, IntPtr.Zero,
                "Environment", SmtoAbortIfHung, 1000, out _);
    }
}
