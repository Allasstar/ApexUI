# ApexUI updates

Newest first. **Breaking** marks changes that need edits in apps using ApexUI.

## Unreleased

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
