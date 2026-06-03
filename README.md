# ApexUI

A minimal desktop UI framework for C#. No markup languages, no code generators, no setup — clone the repo and start building.

## Why

Most C# UI frameworks require a project wizard, a designer, XAML files, or a package you can't read. ApexUI is the opposite:

- **Zero setup** — clone, open, run. `dotnet run` is all you need.
- **Open source core** — the entire framework lives in `lib/`. Read it, understand it, change it.
- **Pure C#** — no XAML, no markup, no DSL. Your UI is just C# code.
- **Cross-platform** — built on Silk.NET (GLFW) + SkiaSharp (OpenGL). Windows, macOS, and Linux.

## Quick start

```bash
git clone https://github.com/Allasstar/ApexUI.git
cd ApexUI
dotnet run
```

## What an app looks like

```csharp
using ApexUI.App.Examples;

var app     = new Application("My App", 900, 700) { Theme = Theme.Dark };
var example = new ScaleExample();
example.Scale.Changed += v => app.UiScale = v;
app.Run(example);
```

The UI tree is plain C# — no designer, no markup:

```csharp
public class CounterExample : Widget
{
    public CounterExample()
    {
        int count = 0;
        var label = new Label().WithText("Count: 0").WithSize(32).AsBold();

        AddChild(new PaddingBox(
            new Column(
                label,
                new Row(
                    new Button("+").OnPress(() => label.WithText($"Count: {++count}")),
                    new Button("-").WithVariant(ButtonVariant.Secondary)
                                  .OnPress(() => label.WithText($"Count: {--count}"))
                ).WithSpacing(8)
            ).WithSpacing(16),
            all: 32
        ));
    }
}
```

## Widgets

| Widget | Description |
|--------|-------------|
| `Label` | Text with size, color, bold |
| `Button` | Three variants: `Primary`, `Secondary`, `Ghost` |
| `TextInput` | Single-line input with placeholder, input modes, validation, password masking |
| `Toggle` | On/off switch with optional label |
| `Slider` | Draggable track — float or integer, optional step snapping |
| `Image` | Raster (PNG/JPG) and SVG; stretch modes: `None`, `Fill`, `Uniform`, `UniformToFill` |

## Layout panels

| Panel | Description |
|-------|-------------|
| `Column` | Vertical stack with configurable spacing |
| `Row` | Horizontal stack with configurable spacing |
| `Stack` | Overlapping layers, last child on top |
| `PaddingBox` | Wraps a single child with padding |

## Two-way binding

`Bindable<T>` connects any two widgets without glue code:

```csharp
var volume = new Bindable<float>(0.5f);

new Slider().WithMin(0f).WithMax(1f).Bind(volume);
new TextInput { Width = 72f }.AsFloat().BindFloat(volume, "F2");
```

Changing either widget updates the other. Cycle-safe: same-value writes are no-ops.

## TextInput validation

```csharp
new TextInput()
    .AsFloat()                                           // only digits, '-', '.'
    .WithValidation(s => string.IsNullOrEmpty(s)
                      || float.TryParse(s, out var v) && v is >= 0 and <= 1)
    .WithPlaceholder("0.0 – 1.0")

new TextInput().AsPassword()                             // value masked as •••
new TextInput().WithAllowedChars("0123456789abcdefABCDEF")  // hex only
new TextInput().WithBlockedChars(" \t")                  // no whitespace
```

`Validate` shows a red border when false — it never blocks typing.

## UI scaling

```csharp
var app   = new Application("App", 900, 700);
var scale = new Bindable<float>(1f);
scale.Changed += v => app.UiScale = v;      // 0.1 – 10, default 1.0
```

A single `canvas.Scale()` transform — every widget scales for free, no code changes needed.

## Theming

```csharp
new Application("App") { Theme = Theme.Dark }.Run(root);

// Custom — override only what you need
var myTheme = new Theme
{
    Primary    = SKColor.FromHex("#6C63FF"),
    FontFamily = "JetBrains Mono",
};
```

## Project structure

```
lib/          ← framework (open, readable, replaceable as a unit)
  Core/       ← Widget, Application, Theme, Bindable, layout primitives
  Layout/     ← Column, Row, Stack, PaddingBox
  Widgets/    ← Label, Button, TextInput, Toggle, Slider, Image
  Extensions/ ← SKColor and SKCanvas helpers (C# 14 extension members)

src/          ← your app code
  Examples/   ← self-contained demos (one widget per example)
  Screens/    ← full-screen views
  Widgets/    ← app-specific widgets
  Models/     ← data models

Program.cs    ← entry point
```

The framework is a single `<Import>` in the `.csproj`. Updating it means replacing the `lib/` folder.

## Distribution

The project is **self-contained** — the .NET runtime is bundled, users need nothing installed.
Platform is auto-detected from the build machine (`win-x64`, `linux-x64`, `osx-arm64`, …).

| Profile | Command | Output |
|---------|---------|--------|
| **Full** (default) | `dotnet publish` | exe + all DLLs (~100 MB) |
| **SingleFile** | `dotnet publish -p:BuildProfile=SingleFile` | one `.exe`, clean root (~100 MB) |
| **MinSize** | `dotnet publish -p:BuildProfile=MinSize` | one `.exe`, trimmed + compressed (~20–40 MB) |

`SingleFile` packs everything (managed DLLs, native libs, runtime) into the exe.
Native libs are extracted to a system temp folder on first launch.

> **MinSize + SkiaSharp**: aggressive trimming may remove reflection-accessed code.
> Add `TrimmerRootDescriptor` entries if you hit `MissingMethodException` at runtime.

## Tech stack

- [Silk.NET](https://github.com/dotnet/Silk.NET) — windowing and input (GLFW)
- [SkiaSharp](https://github.com/mono/SkiaSharp) — 2D GPU rendering
- .NET 10 / C# 14
