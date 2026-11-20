using System.Runtime.InteropServices;

namespace ApexUI.Desktop;

// GLFW reports held keys as repeats, but Silk.NET drops them, so UiHost repeats keys itself; this gives it the
// user's keyboard settings where the system exposes them. Windows: delay 0..3 is 250 to 1000 ms, speed 0..31 is
// about 2.5 to 30 repeats per second. Other systems return null (UiHost's default).
internal static partial class SystemKeyRepeat
{
    private const uint GetKeyboardSpeed = 0x000A;
    private const uint GetKeyboardDelay = 0x0016;

    public static KeyRepeat? Read()
    {
        if (!OperatingSystem.IsWindows()) return null;
        if (!SystemParametersInfo(GetKeyboardDelay, 0, out uint delay, 0) || !SystemParametersInfo(GetKeyboardSpeed, 0, out uint speed, 0))
            return null;

        double perSecond = 2.5 + Math.Min(speed, 31u) * (27.5 / 31);
        return new KeyRepeat(TimeSpan.FromMilliseconds(250 * (Math.Min(delay, 3u) + 1)), TimeSpan.FromSeconds(1 / perSecond));
    }

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(uint action, uint param, out uint value, uint winIni);
}
