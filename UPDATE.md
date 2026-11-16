# ApexUI updates

Newest first. **Breaking** marks changes that need edits in apps using ApexUI.

## Unreleased

### Widget lifecycle
- Widgets are attached while they are in a tree a `UiHost` shows. New `protected virtual void OnAttached(UiHost host)` and `OnDetached()`; `Widget.Host` is now set on each widget as it is attached instead of being looked up through the parents.
- New `protected IDisposable Observe(Bindable<T>, Action<T>)` and `Observe(ObservableList<T>, Action)`: the subscription is dropped when the widget is detached and restored (with the current value) when it is attached again. Every built-in `Bind`, `BindInt`, `BindFloat` and `WithItems(ObservableList)` uses it, so removed widgets are no longer kept alive by the bindables and lists they were bound to.
- Moving a widget to another parent in the same window does not detach and re-attach it.
- Hover, press and focus are cleared when the widget holding them is detached, so a removed widget stops receiving input.
- `Image` disposes its decoded image when detached. New `DisposeOnDetach` (default true) and `KeepOnDetach()` for images you remove and add again.
- **Breaking:** a closed `Overlay` stays attached to the host it was opened on, so dialogs and menus keep their state and images between openings. New `Overlay.Detach()` closes it and releases it. `Dropdown` and `Tooltip` detach their overlays when they are removed.
- **Breaking:** `VirtualList` rows are real children: created and removed during layout (not while drawing), attached and detached like any widget, hit-tested and ticked normally. Scrolling a `VirtualList` now re-runs layout.
- **Breaking:** `Widget.Children` is a `ChildList` (read-only, `IReadOnlyList<Widget>`) instead of `IReadOnlyList<Widget>`. `foreach` over it no longer allocates.

### Performance
- `Label` caches one `SKTextBlob` per line and draws it with the new `DrawContext.DrawText(SKTextBlob?, ...)`. Drawing unchanged labels allocates nothing; the blobs are freed on detach.

### Display scaling
- Apps are sharp on scaled displays. The platform reports the surface size in pixels and the display scale; `UiHost` applies `DpiScale × UiScale` once as the canvas transform and converts pointer positions with the same factor. Layout, drawing, fonts and input are in logical units everywhere else.
- `DesktopPlatform` reads the display scale from GLFW, draws to a surface the size of the framebuffer (before: the window size, wrong on Retina), and treats its width and height as logical units: on a 150% display `new DesktopPlatform("App", 900, 700)` opens a 1350×1050 pixel window. It also resizes the window when it moves to a display with a different scale.
- **Breaking:** `IPlatformListener.Resize(width, height)` → `Resize(pixelWidth, pixelHeight, dpiScale)`. Sizes and pointer positions are surface pixels.
- **Breaking:** `DrawContext`'s `dpiScale` parameter and `DpiScale` property are now `pixelScale` / `PixelScale` (canvas pixels per logical unit, display scale × UI scale). Fonts from `GetFont` and `MakeTextFont` are no longer multiplied by it: the canvas is already scaled, so text was going to be scaled twice.
- New `UiHost.PixelScale` (`DpiScale × UiScale`). `UiHost.DpiScale` now holds the real display scale instead of always 1.

### Platform layer
- **Breaking:** `Application` is gone. An app is now a platform-neutral `UiHost` (tree, layout, drawing, input routing, overlays, focus, theme) running on an `IPlatform` backend. The desktop backend, `DesktopPlatform`, lives in the new `ApexUI.Desktop` project, the only code that uses Silk.NET. Migrate:
  ```csharp
  // before
  new Application("App", 900, 700).SetIcon("icon.svg").BindTheme(preset).Run(root);
  // after
  new UiHost(new DesktopPlatform("App", 900, 700).WithIcon("icon.svg")).BindTheme(preset).Run(root);
  ```
- **Breaking:** desktop apps reference `lib/ApexUI.Desktop/ApexUI.Desktop.csproj` (it brings in `ApexUI`) and add `using ApexUI.Desktop;`. `lib/ApexUI` now depends only on SkiaSharp and Svg.Skia.
- `IPlatform` covers the window and GPU surface (`Run`), frame requests, clipboard, cursor shape (`CursorShape`), UI-thread timers (`StartTimer`) and file dialogs (`OpenFileAsync`/`SaveFileAsync`, declared but not implemented on desktop yet). The backend reports events through `IPlatformListener`, which `UiHost` implements; tests can drive a `UiHost` with a fake platform and no window.
- `Widget.Host` is the `UiHost` showing the widget's tree, or null while it is not on screen. `UiHost.Platform` reaches clipboard, cursor and timers.
- **Breaking:** the static `Application.Current` is removed, so overlays need to know which window to open in:
  - `Overlay.Open()` → `Overlay.Open(UiHost host)`. `Overlay.Open(Widget anchor, edge)` uses the anchor's host and throws if the anchor is not on screen.
  - `Dialog.Show()` → `Dialog.Show(UiHost host)`; inside a widget pass `Host!`.
  - `ContextMenu.Show(x, y)` → `ContextMenu.Show(UiHost host, x, y)`. `ContextMenu.Attach` is unchanged.
- SkiaSharp native assets for Windows, macOS and Linux ship with `ApexUI.Desktop` (before: Windows only).

### Input
- **Breaking:** `KeyEvent` is now `KeyEvent(Key Key, KeyModifiers Modifiers, bool IsDown)` with a `Key` enum (physical keys: `A`–`Z`, `D0`–`D9`, `F1`–`F12`, `Left`, `Enter`, `KeypadEnter`, ...) and `KeyModifiers` flags (`Shift`, `Control`, `Alt`, `Super`). `Ctrl`, `Shift`, `Alt` and `Super` remain as read-only properties. Before, keys were strings such as `"ArrowLeft"` or Silk.NET enum names.
- **Breaking:** typed characters no longer arrive as one-character `KeyDown` events. They arrive in the new `Widget.OnTextInput` (`TextInputEvent(string Text)`), already translated by the keyboard layout; `KeyDown` is sent for every key, including letters.
- Key-up events now carry the modifiers held at the time; before they always reported none.

### Fixes
- `TextInput`: Backspace at the end of the text moved the caret two places back, so the next character was inserted one place too early.

### Performance
- Drawing no longer creates Skia objects per call. `DrawContext` reuses its fill, stroke, text, shadow and layer paints, one path, one rounded rect and cached blur filters; text uses a `FontCache`. In the Gallery, bytes allocated per frame dropped from 8–52 KB to 2.5–16 KB, and per layout pass from 17–210 KB to 0.7–40 KB. What remains in drawing is SkiaSharp's own `DrawText` (about 160 B per call).
- The application keeps one `DrawContext` and one `FontCache` for its lifetime instead of creating a context every frame.
- `Label` caches its measured width and wrapped lines until text, font, size or width change.
- `TextInput` caches its displayed text and caret positions; clicking to place the caret no longer re-measures every prefix of the text.
- `Toggle`, `Checkbox` and `RadioGroup` measure labels with cached fonts.

### Added
- `FontCache`: typefaces and fonts by family, size and weight. Fonts it returns are owned by the cache.
- `DrawContext.GetFont(sizePx, bold)`: cached font for custom text drawing. Do not dispose it.
- `DrawContext.MeasureText(ReadOnlySpan<char>, ...)`: measure part of a string without a substring.
- `DrawContext` constructor takes an optional shared `FontCache`.

### Changed
- `DrawContext` is now `IDisposable`. Dispose contexts you create yourself (for example when rendering off-screen).
- `MakePaint`, `MakeTextFont` and `MakeTextPaint` still return new objects the caller must dispose. `MakeTextFont` no longer leaks its typeface.

### Fixes
- The built-in dark theme now sets `SurfacePressed`, `PrimaryHover`, `Success`, `Warning` and `Danger`; before, it inherited the light values (pressed buttons turned light grey).
- Setting `TextInput.Value` from code to a shorter string no longer crashes drawing when the caret or selection was past the new end (e.g. `NumberInput` re-formatting on Enter).
- `Grid` star columns/rows size like `Auto` under an infinite constraint (inside a horizontal `Scroll` or an unconstrained `Row`) instead of becoming infinitely large.
- `Label` measures with the same default font size it draws with (`Theme.FontSizeBase`), so its box matches its text.
- `RadioGroup` items use the application font family instead of a hardcoded "Segoe UI".
- `Slider` re-clamps `Value` when `Min` or `Max` changes.
- `Toggle`, `Checkbox` and `RadioGroup` no longer leak a typeface on every layout pass.
- Adding a widget that already has a parent moves it: it is removed from the old parent first.
- `Grid` and `Canvas` forget a child's cell or position when it is removed in any way, including `RemoveAllChildren()` and moving it to another parent.

### Changed
- Exceptions thrown while drawing now propagate instead of being drawn as a pink box and logged; they were hiding real bugs.
- New `protected virtual void OnChildRemoved(Widget child)` on `Widget` for panels that keep per-child data.
- The Gallery builds framework-dependent; its self-contained `Full` profile applies only to `dotnet publish`.
- New Gallery smoke test: every tab is laid out and drawn off-screen in the light and dark themes.

### Removed
- **Breaking:** `SKCanvasExtensions` (`canvas.FillRoundRect`, `canvas.StrokeRoundRect`, `canvas.DrawTextCentered`). Use the `DrawContext` helpers.
- **Breaking:** `FrameworkInfo`.

### Project structure
- **Breaking:** ApexUI is now a class library at `lib/ApexUI/ApexUI.csproj`. Reference it with a `ProjectReference` instead of importing `lib/ApexUI.props` (removed).
- **Breaking:** `global using` directives no longer leak into apps. Add `using ApexUI.Core;`, `using ApexUI.Layout;`, `using ApexUI.Widgets;`, `using ApexUI.Extensions;` and `using SkiaSharp;` (or your own `GlobalUsings.cs`) where needed.
- The demo app moved to `src/Gallery` (run with `dotnet run --project src/Gallery`); examples now use the `ApexUI.Gallery.Examples` namespace. Publish profiles moved with it.
- Added `tests/ApexUI.Tests` (xUnit) with the first layout tests for `Row`, `Column`, `Spacer`, margin and alignment.
- Added `ApexUI.sln` and a root `Directory.Build.props` (net10.0, nullable, implicit usings, warnings as errors, latest C#).
- Package versions are pinned: Silk.NET 2.23.0, SkiaSharp 3.119.4, Svg.Skia 2.0.0.8.

### Themes
- **Breaking:** `ThemeLibrary` is no longer `partial` and the `RegisterCustom()` hook is gone (partial classes cannot span assemblies). Call `ThemeLibrary.Register(name, light, dark)` at startup instead.
