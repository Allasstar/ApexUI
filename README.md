# ApexUI

A minimal desktop UI framework for C#. No markup languages, no code generators, no setup — clone the repo and start building.

## Why

Most C# UI frameworks require a project wizard, a designer, XAML files, or a package you can't read. ApexUI is the opposite:

- **Zero setup** — clone, open, run. `dotnet run` is all you need.
- **Open source core** — the entire framework lives in `lib/`. Read it, understand it, change it.
- **Pure C#** — no XAML, no markup, no DSL. Your UI is just C# code.
- **Cross-platform future** — built on Silk.NET (GLFW) + SkiaSharp (OpenGL). The groundwork for Windows, macOS, and Linux is already there.

## Quick start

```bash
git clone https://github.com/Allasstar/ApexUI.git
cd ApexUI
dotnet run
```

## What an app looks like

This is the entire entry point for a working counter app:

```csharp
using ApexUI.App.Examples;

new Application("My App", 800, 600)
{
    Theme = Theme.Dark
}
.Run(new CounterExample());
```

And the UI itself is just a widget:

```csharp
namespace ApexUI.App.Examples;

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

## Included widgets

| Widget | Description |
|--------|-------------|
| `Label` | Text with size, color, bold |
| `Button` | Three variants: `Primary`, `Secondary`, `Ghost` |
| `TextInput` | Single-line text field with placeholder |

## Layout panels

| Panel | Description |
|-------|-------------|
| `Column` | Vertical stack with configurable spacing |
| `Row` | Horizontal stack with configurable spacing |
| `Stack` | Overlapping layers |
| `PaddingBox` | Wraps a single child with padding |

## Theming

Two built-in themes, swap with one property:

```csharp
new Application("My App") { Theme = Theme.Dark }.Run(root);
```

Custom theme — override only what you need:

```csharp
var myTheme = new Theme
{
    Primary   = SKColor.FromHex("#6C63FF"),
    FontFamily = "JetBrains Mono",
};
```

## Project structure

```
lib/        ← framework (open, readable, replaceable as a unit)
  Core/     ← Widget base, Application, Theme, layout primitives
  Layout/   ← Column, Row, Stack, PaddingBox
  Widgets/  ← Label, Button, TextInput
  Extensions/

src/        ← your app code
  Examples/ ← self-contained demos (one widget per example)
  Screens/  ← full-screen views
  Widgets/  ← app-specific widgets
  Models/   ← data models

Program.cs  ← entry point (pick a root widget and run)
```

The framework is a single `<Import>` in the `.csproj`. Updating it means replacing the `lib/` folder.

## Tech stack

- [Silk.NET](https://github.com/dotnet/Silk.NET) — windowing and input (GLFW)
- [SkiaSharp](https://github.com/mono/SkiaSharp) — 2D GPU rendering
- .NET 10 / C# 14
