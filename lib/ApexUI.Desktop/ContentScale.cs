using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using Silk.NET.Windowing;

namespace ApexUI.Desktop;

// On Windows the scale is the window's own DPI: it changes together with the WM_DPICHANGED resize, while
// glfwGetWindowContentScale follows the monitor holding most of the window and runs ahead of it mid-drag.
// Elsewhere glfwGetWindowContentScale, which Silk.NET 2.23 does not bind, is called through the GLFW library
// Silk already loaded. Returns null when the window is neither.
internal abstract partial class ContentScale
{
    public abstract float Read();

    public static ContentScale? For(IWindow window)
    {
        if (window.Native?.Win32 is { } win32) return new Win32Scale(win32.Hwnd);
        if (window.Native?.Glfw is not { } handle) return null;
        if (!Glfw.GetApi().Context.TryGetProcAddress("glfwGetWindowContentScale", out var proc)) return null;
        return new GlfwScale(handle, proc);
    }

    private sealed partial class Win32Scale(nint hwnd) : ContentScale
    {
        private const float DefaultDpi = 96f;

        public override float Read()
        {
            uint dpi = GetDpiForWindow(hwnd);
            return dpi > 0 ? dpi / DefaultDpi : 1f;
        }

        [LibraryImport("user32.dll")]
        private static partial uint GetDpiForWindow(nint hwnd);
    }

    private sealed unsafe class GlfwScale(nint window, nint proc) : ContentScale
    {
        private readonly delegate* unmanaged[Cdecl]<nint, float*, float*, void> _getWindowContentScale =
            (delegate* unmanaged[Cdecl]<nint, float*, float*, void>)proc;

        public override float Read()
        {
            float x, y;
            _getWindowContentScale(window, &x, &y);
            return x > 0 ? x : 1f;
        }
    }
}
