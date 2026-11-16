# ApexUI — Framework Architecture

## File map

```
ApexUI/
├── ApexUI.sln
├── Directory.Build.props          ← shared build settings
├── lib/                           ← library projects only
│   ├── ApexUI/                    ← platform-neutral framework library (ApexUI.csproj)
│   │   ├── Core/                  ← Widget, UiHost, IPlatform, DrawContext, Theme, Rect/Size/Thickness, Overlay, Bindable
│   │   ├── Layout/                ← Row, Column, Stack, PaddingBox, Spacer, Grid, Wrap, Canvas
│   │   ├── Widgets/               ← Label, Button, TextInput, Scroll, Dropdown, Tabs, Dialog, ...
│   │   └── Extensions/            ← C# 14 extension members on SKColor
│   └── ApexUI.Desktop/            ← desktop backend: DesktopPlatform (the only code that uses Silk.NET)
├── src/                           ← apps and examples that use ApexUI
│   └── Gallery/                   ← demo app (Program.cs + Examples/)
└── tests/ApexUI.Tests/            ← xUnit tests
```

## Host and platform

```
IPlatform (DesktopPlatform, later a web backend)
    window, GPU surface, frame requests, raw input, clipboard, cursor, timers, file dialogs
        │  IPlatformListener: Resize, Render(canvas), PointerMove/Down/Up, Wheel, KeyDown/Up, TextEntered, Close
        ▼
UiHost (platform-neutral)
    root widget tree, overlays, layout, drawing, input routing (hover, capture, focus, keys), theme, fonts
```

The platform reports sizes and pointer positions in surface pixels, plus the display scale (`DpiScale`).
`UiHost` applies `DpiScale × UiScale` once, as the canvas transform, and divides pointer positions by the
same factor; layout, drawing, fonts and input everywhere else are in logical units.
Keys arrive as the `Key` enum with `KeyModifiers`; typed characters arrive separately as text, so widgets
can tell typing from shortcuts. Widgets reach their host through `Widget.Host`; there is no static application.
Tests drive a `UiHost` through a fake platform, without a window.

## Widget lifecycle

`Widget.Host` is set on every widget in a tree the host shows (the root and opened overlays) and is
propagated by `AddChild`/`RemoveChild`. Attaching runs `OnAttached` top-down; detaching runs `OnDetached`
bottom-up and clears hover, press and focus references to the widget. Subscriptions made with `Observe`
are dropped on detach and restored, with a re-sync, on attach. `VirtualList` rows are ordinary children,
created and removed during arrange.

## Input routing

Every widget handles input the same way. `UiHost` finds the event's source and walks up the parents:

| Event | Source | Bubbles |
|---|---|---|
| `PointerPressed`, `PointerMoved`, `PointerReleased` | the captured widget, else the widget under the pointer | yes |
| `Click` (left press and release over the same widget) | the pressed widget | yes |
| `Wheel` | the widget under the pointer | yes |
| `KeyDown`, `KeyUp`, `TextEntered` | the focused widget | yes |
| `PointerEntered`, `PointerExited` | each widget the pointer entered or left | no |
| `GotFocus`, `LostFocus`, `PointerCaptureLost` | the widget concerned | no |

On each widget the protected virtual `On…` method runs first (the widget's own behavior), then the public
event (user handlers), so subscribing never replaces built-in behavior. Setting `Handled` stops the event at
that widget. Built-in widgets set it only for what they act on: a `Button` handles `Click`, a `Scroll`
handles `Wheel` only when it actually scrolled, a `TextInput` leaves Tab, Up/Down and unknown shortcuts alone.

- **Hover** is a chain: the widget under the pointer and all its ancestors have `IsHovered`.
- **Capture**: a press captures the pressed widget, so it keeps receiving moves and the release outside its
  bounds. `CapturePointer()` moves capture elsewhere (an ancestor, or a drag without a press); capture ends
  when all buttons are up, and `OnPointerCaptureLost` is the place to reset drag state.
- **Focus**: a press focuses the nearest `Focusable` widget under the pointer, or clears focus. Tab / Shift+Tab
  move through focusable widgets in tree order (inside the topmost modal overlay, if one is open). A focus ring
  is drawn only when focus came from the keyboard (`UiHost.IsFocusVisible`).
- Key presses no widget handled reach `UiHost.UnhandledKeyDown` (app-wide shortcuts), then Tab navigation.
- Pointer events carry modifiers and a `ClickCount` (2 for a double press). `Widget.Cursor` sets the pointer
  shape; the captured widget's cursor wins, then the hovered chain's, deepest first.
- One event-argument object per kind is reused for every event, so routing allocates nothing. Handlers must
  not keep it.

## The three-pass model

A frame runs three passes in order (Measure and Arrange only when layout is dirty):

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

## Invalidation — when frames are drawn

Nothing is drawn unless something changed. An untouched window renders no frames.

```
Property changes → SetLayoutProperty / SetRenderProperty (no-op if the value is the same)
    ↓
InvalidateLayout() marks the widget and its ancestors layout-dirty; Invalidate() marks nothing
    ↓
Host.RequestFrame() → IPlatform.RequestFrame()   (once per frame, however many changes)
    ↓
UiHost.Render(): animation-frame callbacks → layout if root.IsLayoutDirty → overlays if needed → draw everything
```

Layout of a dirty tree re-runs from the root; drawing always redraws the whole window.
Pointer input lays out a pending change first, so hit-testing matches what the next frame will show.

Widgets never change their own or other widgets' state inside `MeasureCore`, `ArrangeCore` or `DrawCore`:
an invalidation there schedules another frame, and a widget that does it every frame keeps the window busy.
Colors that depend on the theme are picked at draw time through `BackgroundSource` / `Label.ColorSource`.

Things that change over time use `UiHost.Platform.StartTimer` (caret blink, tooltip delay) or
`UiHost.RequestAnimationFrame` (smooth animation, re-requested each frame while it runs).

## Adding a new widget (minimum viable)

```csharp
public class ProgressBar : Widget
{
    public float Value { get; set => SetRenderProperty(ref field, Math.Clamp(value, 0f, 1f)); }

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
| Extension members | `SKColorExtensions.cs` | `SKColor.FromHex()`, `color.WithAlpha()` |
| Implicit `Span<T>` | Layout loops | Zero-alloc child iteration |
| Records (`record struct`) | `Rect`, `Size`, `Thickness` | Value equality + `with` expressions for free |

## Running

```bash
dotnet run --project src/Gallery              # debug build, opens the Gallery
dotnet run --project src/Gallery -c Release   # optimized
dotnet test                                       # run ApexUI.Tests
```
