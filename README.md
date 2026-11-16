# ApexUI

A minimal desktop UI framework for C#. No markup languages, no code generators, no setup — clone the repo and start building.

## Why

Most C# UI frameworks require a project wizard, a designer, XAML files, or a package you can't read. ApexUI is the opposite:

- **Zero setup** — clone, open, run. `dotnet run --project src/Gallery` is all you need.
- **Open source core** — the framework lives in `lib/ApexUI/` (platform-neutral) and `lib/ApexUI.Desktop/` (the desktop backend). Read it, understand it, change it.
- **Pure C#** — no XAML, no markup, no DSL. Your UI is just C# code.
- **Portable foundations** — built on Silk.NET (GLFW) + SkiaSharp (OpenGL). Windows is the tested platform today.

---

## Quick start

```bash
git clone https://github.com/Allasstar/ApexUI.git
cd ApexUI
dotnet run --project src/Gallery
```

To use ApexUI in a desktop app, reference the desktop backend (it brings in the core library):

```xml
<ProjectReference Include="path/to/ApexUI/lib/ApexUI.Desktop/ApexUI.Desktop.csproj" />
```

---

## Host and platform

An app is a `UiHost` (widget tree, layout, drawing, input routing, overlays, theme) running on an `IPlatform` backend (window, GPU surface, raw input, clipboard, cursor, timers). `DesktopPlatform` in `ApexUI.Desktop` is the desktop backend.

```csharp
// Program.cs
using ApexUI.Core;
using ApexUI.Desktop;

var example  = new MyWidget();
var platform = new DesktopPlatform("My App", 900, 700)
    .WithIcon("res/icon.svg");     // .svg / .ico / raster — call before Run()

new UiHost(platform)
    .BindUiScale(example.Scale)    // Bindable<float>  — 0.1 – 10
    .BindDarkMode(example.Dark)    // Bindable<bool>   — true = Dark
    .BindTheme(example.Preset)     // Bindable<string> — theme name from ThemeLibrary
    .BindFontFamily(example.Font)  // Bindable<string> — font family name
    .Run(example);
```

Each `Bind*` method wires a `Bindable<T>` to the matching host property — changing the bindable at runtime instantly updates the app. All methods return `this` for chaining.

Inside a widget, `Host` is the `UiHost` showing it (null until the widget is on screen). Dialogs and overlays need it to know which window to open in: `dialog.Show(Host!)`, `overlay.Open(Host!)`. `Host.Platform` gives access to the clipboard, cursor and timers.

---

## Building widgets

Subclass `Widget` and override the three passes:

```csharp
public class MyWidget : Widget
{
    protected override Size MeasureCore(Size available)  => new(200, 40);
    protected override void ArrangeCore(Rect finalRect)  { /* position children */ }
    protected override void DrawCore(DrawContext ctx)    { /* custom drawing */ }
}
```

The UI tree is plain C# — no designer, no markup:

```csharp
public class CounterExample : Widget
{
    public CounterExample()
    {
        int count = 0;
        var label = new Label { Text = "Count: 0", FontSize = 32f, Bold = true };

        AddChild(new PaddingBox(
            new Column(
                label,
                new Row(
                    new Button("+").OnPress(() => label.Text = $"Count: {++count}"),
                    new Button("-").WithVariant(ButtonVariant.Secondary)
                                  .OnPress(() => label.Text = $"Count: {--count}")
                ).WithSpacing(8f)
            ).WithSpacing(16f),
            new Thickness(32f)
        ));
    }
}
```

---

## Widgets

### Display

| Widget | Description |
|--------|-------------|
| `Label` | Text with configurable size, color, bold |
| `Image` | Raster (PNG/JPG) and SVG; stretch modes: `None`, `Fill`, `Uniform`, `UniformToFill` |
| `ProgressBar` | Filled track; variants: `Primary`, `Success`, `Warning`, `Danger` |
| `Separator` | 1 px horizontal or vertical line |
| `Spacer` | Flexible empty space that fills available room |

### Input

| Widget | Description |
|--------|-------------|
| `Button` | Variants: `Primary`, `Secondary`, `Ghost` |
| `TextInput` | Single-line text with placeholder, modes, validation, password masking |
| `NumberInput` | `[−] [input] [+]` with step, min/max, and format string |
| `Toggle` | On/off switch with optional label |
| `Slider` | Draggable track — float or integer, optional step snapping |
| `RadioGroup` | Mutually exclusive option set |
| `Dropdown<T>` | Select-one list that opens a popup menu |

### Navigation & overlay

| Widget | Description |
|--------|-------------|
| `Tabs` | Tab bar (Top / Bottom / Left / Right) with scrollable tab strip |
| `ContextMenu` | Right-click menu; attach to any widget with `ContextMenu.Attach(target, menu)` |
| `Dialog` | Modal panel with title, content area, and configurable button row |
| `Tooltip` | Hover-delay popup; wraps any widget: `new Tooltip(child, "hint")` |
| `Scroll` | Scrollable viewport; direction: `Vertical`, `Horizontal`, `Both` |

---

## Layout panels

| Panel | Description |
|-------|-------------|
| `Column` | Vertical stack with configurable spacing |
| `Row` | Horizontal stack with configurable spacing |
| `Stack` | Overlapping layers — last child on top |
| `PaddingBox` | Wraps a single child with uniform or per-side padding |
| `Wrap` | Flows children into rows/columns, wrapping on overflow |
| `Grid` | Fixed column grid with even column widths |

---

## Two-way binding

`Bindable<T>` connects any two controls without glue code:

```csharp
var volume = new Bindable<float>(0.5f);

new Slider().WithMin(0f).WithMax(1f).Bind(volume);
new TextInput { Width = 72f }.AsFloat().BindFloat(volume, "F2");
```

Changing either widget updates the other. Same-value writes are no-ops — no cycles.

```csharp
// Listen to changes directly
volume.Changed += v => Console.WriteLine($"Volume: {v}");

// Push a value programmatically
volume.Value = 0.8f;  // notifies all listeners
```

---

## Theming

### Built-in themes

ApexUI ships five themes, each with a light and dark variant:

| Name | Palette |
|------|---------|
| `ThemeLibrary.Default` | Clean blue — framework default |
| `ThemeLibrary.Contrast` | Pure black/white, max readability |
| `ThemeLibrary.Forest` | Warm cream surfaces, deep green accent |
| `ThemeLibrary.Desert` | Sandy warmth, terracotta primary |
| `ThemeLibrary.Space` | Soft lavender / deep cosmic dark |

```csharp
var preset  = new Bindable<string>(ThemeLibrary.Default);
var isDark  = new Bindable<bool>(false);

new UiHost(new DesktopPlatform("App", 900, 700))
    .BindTheme(preset)       // pick the palette
    .BindDarkMode(isDark)    // flip light ↔ dark within that palette
    .Run(root);

// Change at runtime — updates instantly
preset.Value = ThemeLibrary.Space;
isDark.Value = true;
```

### Custom themes

Register it at startup, before building UI that lists the available themes:

```csharp
ThemeLibrary.Register("Neon", new Theme
{
    Background = new SKColor(0x0A, 0x0A, 0x0A),
    Primary    = new SKColor(0x00, 0xFF, 0x99),
    // ... other tokens
},
new Theme { /* dark variant */ });
```

The new theme appears in `ThemeLibrary.Names`, so it shows up in any theme picker built afterwards. To remove a built-in call `ThemeLibrary.Remove(name)`. See `src/Gallery/Examples/SolAndLunaTheme.cs`.

### Theme tokens

| Token | Purpose |
|-------|---------|
| `Background` | Window / app background |
| `Surface` | Card and widget surface |
| `SurfaceHover` / `SurfacePressed` | Interactive surface states |
| `OnSurface` / `OnSurfaceMuted` | Primary and muted text |
| `Primary` / `PrimaryHover` | Accent / interactive color |
| `OnPrimary` | Text on primary-colored backgrounds |
| `Success` / `Warning` / `Danger` | Semantic feedback colors |
| `Border` | Borders and separators |
| `FontFamily` | Default font family name |
| `FontSizeBase` / `Small` / `Large` / `Title` | Logical pixel sizes |
| `CornerRadiusSm` / `Md` / `Lg` | Rounded corner radii |
| `SpacingXs` / `Sm` / `Md` / `Lg` | Standard spacing steps |

---

## UI scaling

Layout, drawing and input are in logical units. A single canvas transform of display scale × UI scale maps them to pixels, so the app is sharp on scaled displays (150% Windows, Retina) with no per-widget code. `DesktopPlatform` sizes the window in logical units and follows it between displays with different scales.

On top of that, a UI scale zooms the entire widget tree:

```csharp
var scale = new Bindable<float>(1f);   // 0.1 – 10, default 1.0

new UiHost(new DesktopPlatform("App", 900, 700))
    .BindUiScale(scale)
    .Run(root);

scale.Value = 1.5f;  // everything scales instantly
```

Font family is also live-swappable:

```csharp
var font = new Bindable<string>("Segoe UI");

new UiHost(new DesktopPlatform("App", 900, 700))
    .BindFontFamily(font)
    .Run(root);

font.Value = "Consolas";  // all text re-measures and redraws
```

---

## Drawing shapes

Override `DrawCore(DrawContext ctx)` to draw custom visuals. All shape helpers are on `DrawContext`:

```csharp
protected override void DrawCore(DrawContext ctx)
{
    var t = ctx.Theme;

    // Rectangles
    ctx.FillRect(LayoutBounds, t.Surface);
    ctx.FillRoundRect(LayoutBounds, cornerRadius: 8f, t.Primary);
    ctx.StrokeRoundRect(LayoutBounds, cornerRadius: 8f, t.Border, thickness: 1f);

    // Circles & ovals
    ctx.FillCircle(cx, cy, radius: 10f, t.Primary);
    ctx.StrokeCircle(cx, cy, radius: 10f, t.Border, thickness: 2f);
    ctx.FillOval(new Rect(x, y, width, height), t.Surface);

    // Lines
    ctx.DrawLine(x0, y0, x1, y1, t.Border, thickness: 1f, cap: SKStrokeCap.Round);

    // Triangles
    ctx.FillTriangle(x0, y0, x1, y1, x2, y2, t.Primary);
    ctx.StrokeTriangle(x0, y0, x1, y1, x2, y2, t.Border);

    // Drop shadow (blurred, behind content)
    ctx.DrawShadow(rect.Translate(0, 4), cornerRadius: 12f, blur: 10f, new SKColor(0, 0, 0, 55));

    // Indicators
    ctx.DrawChevron(cx, cy, size: 4f, pointUp: false, t.OnSurfaceMuted, thickness: 1.5f);
    ctx.DrawCheckmark(cx, cy, size: 8f, t.Success, thickness: 2f);
    ctx.DrawCross(cx, cy, size: 6f, t.Danger, thickness: 2f);

    // Raw Skia canvas for anything else (Make* objects are yours to dispose)
    using var paint = ctx.MakePaint(t.Primary);
    ctx.Canvas.DrawPath(path, paint);
}
```

Text: `ctx.DrawText` and `ctx.MeasureText` use cached fonts. For raw Skia text, `GetFont` returns a cached font owned by the context (do not dispose it):

```csharp
var font = ctx.GetFont(ctx.Theme.FontSizeBase, bold: false);
using var paint = ctx.MakeTextPaint(ctx.Theme.OnSurface);
ctx.Canvas.DrawText("Hello", x, y, SKTextAlign.Left, font, paint);
```

The built-in helpers reuse their paints, paths and fonts, so drawing with them creates no Skia objects per frame.

Add your own shape helpers without touching the library with C# 14 extension members:

```csharp
public static class MyDrawing
{
    extension(DrawContext ctx)
    {
        public void DrawStar(float cx, float cy, float size, SKColor color) { ... }
    }
}
```

---

## TextInput

```csharp
new TextInput()
    .AsFloat()                                             // only digits, '-', '.'
    .WithValidation(s => float.TryParse(s, out var v)
                      && v is >= 0f and <= 1f)             // red border when false
    .WithPlaceholder("0.0 – 1.0")

new TextInput().AsPassword()                               // masked as •••
new TextInput().WithAllowedChars("0123456789abcdefABCDEF") // hex only
new TextInput().WithBlockedChars(" \t")                    // no whitespace
```

`Validate` shows a red border on failure — it never blocks typing.

---

## Project structure

```
ApexUI.sln
Directory.Build.props   ← shared settings (net10.0, nullable, warnings as errors)
UPDATE.md               ← changelog, including API breaks
lib/                    ← library projects only
  ApexUI/               ← the platform-neutral framework library (SkiaSharp only)
    Core/               ← Widget, UiHost, IPlatform, Theme, ThemeLibrary, Bindable, DrawContext
    Layout/             ← Column, Row, Stack, PaddingBox, Wrap, Grid, Spacer
    Widgets/            ← all widgets
    Extensions/         ← SKColor helpers
  ApexUI.Desktop/       ← desktop backend: DesktopPlatform (Silk.NET window, OpenGL surface, input)
src/                    ← apps and examples that use ApexUI
  Gallery/              ← demo app showing every widget
    Examples/           ← self-contained demos (one Widget subclass per file)
    res/                ← icons copied to the output folder
tests/ApexUI.Tests/     ← xUnit tests (layout runs without a window)
```

---

## Distribution

The Gallery is published **self-contained** (profiles live in `src/Gallery/Gallery.csproj`; copy them into your app) — the .NET runtime is bundled, users need nothing installed.

| Profile | Command | Output | Size |
|---------|---------|--------|------|
| **Full** (default) | `dotnet publish src/Gallery` | `src/Gallery/bin/Publish/Full/` | ~100 MB |
| **SingleFile** | `dotnet publish src/Gallery -p:BuildProfile=SingleFile` | `src/Gallery/bin/Publish/SingleFile/` | ~100 MB (one `.exe`) |
| **MinSize** | `dotnet publish src/Gallery -p:BuildProfile=MinSize` | `src/Gallery/bin/Publish/MinSize/` | ~20–40 MB (one `.exe`) |

`SingleFile` packs managed DLLs, native libs, and the runtime into one executable. Native libs are extracted to a temp folder on first launch.

> **MinSize + SkiaSharp**: aggressive trimming may remove reflection-accessed code. Add `TrimmerRootDescriptor` entries if you see `MissingMethodException` at runtime.

---

## Tech stack

- [Silk.NET](https://github.com/dotnet/Silk.NET) — windowing and input (GLFW)
- [SkiaSharp](https://github.com/mono/SkiaSharp) — 2D GPU rendering
- .NET 10 / C# 14
