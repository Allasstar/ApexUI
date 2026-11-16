using Silk.NET.GLFW;
using Silk.NET.Windowing;

namespace ApexUI.Desktop;

// Silk.NET 2.23 does not bind glfwGetWindowContentScale, so it is called through the
// GLFW library Silk already loaded. Returns null when the window is not a GLFW window.
internal sealed unsafe class ContentScale
{
    private readonly delegate* unmanaged[Cdecl]<nint, float*, float*, void> _getWindowContentScale;
    private readonly nint _window;

    private ContentScale(nint window, nint proc)
    {
        _window = window;
        _getWindowContentScale = (delegate* unmanaged[Cdecl]<nint, float*, float*, void>)proc;
    }

    public static ContentScale? For(IWindow window)
    {
        if (window.Native?.Glfw is not { } handle) return null;
        if (!Glfw.GetApi().Context.TryGetProcAddress("glfwGetWindowContentScale", out var proc)) return null;
        return new ContentScale(handle, proc);
    }

    public float Read()
    {
        float x, y;
        _getWindowContentScale(_window, &x, &y);
        return x > 0 ? x : 1f;
    }
}
