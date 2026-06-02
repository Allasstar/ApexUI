# ApexUI — Framework Architecture

## File map

```
ApexUI/
├── Program.cs                     ← App developer writes ONLY this
├── ApexUI.csproj                 ← imports lib/ApexUI.props; adds src/**/*.cs
│
├── lib/                           ← framework source
│   ├── Core/
│   │   ├── Rect.cs                ← Rect value type
│   │   ├── Size.cs                ← Size value type
│   │   ├── Thickness.cs           ← Thickness value type
│   │   ├── Widget.cs              ← BASE CLASS — every widget inherits this
│   │   ├── DrawContext.cs         ← Canvas + theme passed into every Draw()
│   │   ├── Theme.cs               ← All colors/fonts in one place
│   │   └── Application.cs         ← Window, event loop, input dispatch
│   │
│   ├── Layout/
│   │   ├── Stack.cs               ← Stack panel
│   │   ├── Column.cs              ← Column panel
│   │   ├── Row.cs                 ← Row panel
│   │   └── PaddingBox.cs          ← PaddingBox panel
│   │
│   ├── Widgets/
│   │   ├── Label.cs               ← Label widget
│   │   ├── Button.cs              ← Button widget
│   │   └── TextInput.cs           ← TextInput widget
│   │
│   └── Extensions/
│       ├── SKColorExtensions.cs   ← C# 14 extension members on SKColor
│       └── SKCanvasExtensions.cs  ← C# 14 extension members on SKCanvas
│
└── src/                           ← app-specific code
```

## The three-pass model

Every frame runs three passes in order:

```
1. Measure(availableSize)
      ↓ widget reports DesiredSize

2. Arrange(finalRect)
      ↓ parent tells widget its actual bounds
      ↓ widget positions its children

3. Draw(ctx)
      ↓ widget draws background
      ↓ widget draws content (DrawCore)
      ↓ widget draws children
```

This is the same model used by WPF, Avalonia, Flutter, and HTML/CSS.
Learning it once means understanding all of them.

## Dirty flags — how redraws work

```
Property changes → Invalidate() or InvalidateLayout()
    ↓
Bubbles up to root via Parent?.Invalidate()
    ↓
Application.OnRender() sees root.IsLayoutDirty
    ↓
Runs Measure + Arrange + Draw
```

Only dirty widgets are re-laid-out. Visual-only changes
(color, text) skip Measure/Arrange and go straight to Draw.

## Adding a new widget (minimum viable)

```csharp
public class ProgressBar : Widget
{
    public float Value { get; set { field = Math.Clamp(value,0,1); Invalidate(); } }

    protected override Size MeasureCore(Size available)
        => new Size(available.Width, 8f);  // fixed 8px height, full width

    protected override void DrawCore(DrawContext ctx)
    {
        // Background track
        ctx.Canvas.FillRoundRect(LayoutBounds.ToSKRect(), 4, ctx.Theme.Surface);

        // Fill
        var fill = LayoutBounds with { Width = LayoutBounds.Width * Value };
        ctx.Canvas.FillRoundRect(fill.ToSKRect(), 4, ctx.Theme.Primary);
    }
}
```

That's it. `FillRoundRect` comes from the C# 14 extension member on SKCanvas.

## What to build next (in order)

| Step | What | Why |
|------|------|-----|
| 1 | `ScrollViewer` | Needed for any content taller than the window |
| 2 | `Image` widget | Load + display PNG/JPEG via SkiaSharp |
| 3 | `Grid` panel | 2-axis layout for forms and tables |
| 4 | `Checkbox`, `Toggle` | Standard interactive controls |
| 5 | Hot reload | Re-parse and re-run app code on file save |
| 6 | `Bindable<T>` | Reactive property system, two-way binding |
| 7 | Web target | Compile to Wasm, swap Silk.NET for browser Canvas |

## C# 14 features used and why

| Feature | Where used | Why |
|---------|-----------|-----|
| `field` keyword | `Widget.cs` — every property | No separate backing field per property |
| Extension members | `SKColorExtensions.cs`, `SKCanvasExtensions.cs` | `SKColor.FromHex()`, `canvas.FillRoundRect()` |
| Implicit `Span<T>` | Layout loops | Zero-alloc child iteration |
| `?.=` null-conditional | Event callbacks | `widget.OnClick ??= default` |
| Records (`record struct`) | `Rect`, `Size`, `Thickness` | Value equality + `with` expressions for free |

## Running

```bash
dotnet run           # debug build, opens window immediately
dotnet run -c Release  # optimized, ~15-30% faster layout math (JIT AVX-512)
```
