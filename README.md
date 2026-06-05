# ApexUI

A minimal desktop UI framework for C#. No markup languages, no code generators, no setup — clone the repo and start building.

## Why

Most C# UI frameworks require a project wizard, a designer, XAML files, or a package you can't read. ApexUI is the opposite:

- **Zero setup** — clone, open, run. `dotnet run` is all you need.
- **Open source core** — the entire framework lives in `lib/`. Read it, understand it, change it.
- **Pure C#** — no XAML, no markup, no DSL. Your UI is just C# code.
- **Cross-platform** — built on Silk.NET (GLFW) + SkiaSharp (OpenGL). Windows, macOS, and Linux.

---

## Quick start

```bash
git clone https://github.com/Allasstar/ApexUI.git
cd ApexUI
dotnet run
```

---

## Application

```csharp
// Program.cs
var example = new MyWidget();

new Application("My App", 900, 700)
    .SetIcon("res/icon.svg")       // .svg / .ico / raster — call before Run()
    .BindUiScale(example.Scale)    // Bindable<float>  — 0.1 – 10
    .BindDarkMode(example.Dark)    // Bindable<bool>   — true = Dark
    .BindTheme(example.Preset)     // Bindable<string> — theme name from ThemeLibrary
    .BindFontFamily(example.Font)  // Bindable<string> — font family name
    .Run(example);
```

Each `Bind*` method wires a `Bindable<T>` to the matching application property — changing the bindable at runtime instantly updates the app. All methods return `this` for chaining.

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

new Application("App", 900, 700)
    .BindTheme(preset)       // pick the palette
    .BindDarkMode(isDark)    // flip light ↔ dark within that palette
    .Run(root);

// Change at runtime — updates instantly
preset.Value = ThemeLibrary.Space;
isDark.Value = true;
```

### Custom themes

Add a partial class file anywhere in your project (no lib edits required):

```csharp
// src/MyTheme.cs
namespace ApexUI.Core;   // must match ThemeLibrary's namespace

public static partial class ThemeLibrary
{
    public const string Neon = "Neon";

    static partial void RegisterCustom()
        => Register(Neon, new Theme
        {
            Background = new SKColor(0x0A, 0x0A, 0x0A),
            Primary    = new SKColor(0x00, 0xFF, 0x99),
            // ... other tokens
        },
        new Theme { /* dark variant */ });
}
```

`RegisterCustom()` is called once at startup, after all built-ins are registered. The new theme appears automatically in the Settings dropdown and can be referenced by its constant.

To remove a built-in call `Remove(name)` inside `RegisterCustom()`.

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

A single canvas transform scales the entire widget tree — no per-widget code needed:

```csharp
var scale = new Bindable<float>(1f);   // 0.1 – 10, default 1.0

new Application("App", 900, 700)
    .BindUiScale(scale)
    .Run(root);

scale.Value = 1.5f;  // everything scales instantly
```

Font family is also live-swappable:

```csharp
var font = new Bindable<string>("Segoe UI");

new Application("App", 900, 700)
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

    // Raw Skia canvas for anything else
    ctx.Canvas.DrawPath(path, ctx.MakePaint(t.Primary));
}
```

Fonts:

```csharp
using var font  = ctx.MakeTextFont(ctx.Theme.FontSizeBase, bold: false);
using var paint = ctx.MakeTextPaint(ctx.Theme.OnSurface);
ctx.Canvas.DrawText("Hello", x, y, SKTextAlign.Left, font, paint);
```

Add your own shape helpers without touching the library — `DrawContext` is `partial`:

```csharp
// anywhere in src/ — same namespace, same assembly
namespace ApexUI.Core;
public sealed partial class DrawContext
{
    public void DrawStar(float cx, float cy, float size, SKColor color) { ... }
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
lib/          ← framework (open, readable, replaceable as a unit)
  Core/       ← Widget, Application, Theme, ThemeLibrary, Bindable, DrawContext
  Layout/     ← Column, Row, Stack, PaddingBox, Wrap, Grid, Spacer
  Widgets/    ← all widgets
  Extensions/ ← SKColor and SKCanvas helpers

src/          ← your app code
  Examples/   ← self-contained demos (one Widget subclass per file)
  Screens/    ← full-screen views
  Widgets/    ← app-specific widgets
  Models/     ← data models

Program.cs    ← entry point
```

---

## Distribution

The project is **self-contained** — the .NET runtime is bundled, users need nothing installed.

| Profile | Command | Output | Size |
|---------|---------|--------|------|
| **Full** (default) | `dotnet publish` | `bin/Publish/Full/` | ~100 MB |
| **SingleFile** | `dotnet publish -p:BuildProfile=SingleFile` | `bin/Publish/SingleFile/` | ~100 MB (one `.exe`) |
| **MinSize** | `dotnet publish -p:BuildProfile=MinSize` | `bin/Publish/MinSize/` | ~20–40 MB (one `.exe`) |

`SingleFile` packs managed DLLs, native libs, and the runtime into one executable. Native libs are extracted to a temp folder on first launch.

> **MinSize + SkiaSharp**: aggressive trimming may remove reflection-accessed code. Add `TrimmerRootDescriptor` entries if you see `MissingMethodException` at runtime.

---

## Tech stack

- [Silk.NET](https://github.com/dotnet/Silk.NET) — windowing and input (GLFW)
- [SkiaSharp](https://github.com/mono/SkiaSharp) — 2D GPU rendering
- .NET 10 / C# 14
