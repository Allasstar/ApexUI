# ApexUI — Framework Architecture

## File map

```
ApexUI/
├── ApexUI.sln
├── Directory.Build.props          ← shared build settings
├── lib/                           ← library projects only
│   ├── ApexUI/                    ← platform-neutral framework library (ApexUI.csproj)
│   │   ├── Core/                  ← Widget, UiHost, IPlatform, DrawContext, Theme, Rect/Size/Thickness, Overlay, Bindable
│   │   ├── Layout/                ← LinearPanel (Row, Column), Stack, PaddingBox, Spacer, Grid, Wrap, Canvas
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
      ↓ widget reports DesiredSize (MeasureCore sees the space inside Margin and Padding)

2. Arrange(finalRect)
      ↓ parent tells widget its actual bounds (LayoutBounds, including Padding)
      ↓ widget positions its children inside ContentBounds (ArrangeCore)

3. Draw(ctx)
      ↓ widget draws background
      ↓ widget draws content (DrawCore)
      ↓ widget draws children, through its ChildTransform
```

This is the same model used by WPF, Avalonia, Flutter, and HTML/CSS.
Learning it once means understanding all of them.

- **Caching.** `Measure` with the same constraint and `Arrange` with the same rect return at once when the
  widget was not invalidated, so only the branch that changed is laid out again. `ArrangeCore` must arrange
  every visible child; a skipped one keeps its previous slot.
- **Padding** belongs to the widget: every panel and control honors it, `PaddingBox` is only shorthand.
- **Linear layout** is one panel, `LinearPanel` (`Row`, `Column`, `RadioGroup` and the tab bar use it).
  Any child with `Grow > 0` takes a share of the leftover length; `Spacer` is an empty growing widget.
- **Per-child panel data** (grid cell, canvas position) lives on the child through `GetParentData` /
  `SetParentData`, so it disappears with the child.
- **Coordinates.** `LayoutBounds` are absolute in a widget's layout space. A parent's `ChildTransform`
  (offset and uniform scale) maps its children's space into its own; drawing, hit-testing and pointer event
  positions go through it, layout does not. `Scroll` and `VirtualList` scroll this way, so scrolling only
  redraws, and a pan/zoom view gets the same mechanism. `WindowBounds` and `PointFromWindow` convert to and
  from window coordinates; pointer events carry both (`X/Y` for the receiver, `WindowX/WindowY`).
- **Culling.** A widget outside the current clip is not drawn at all.

## Invalidation — when frames are drawn

Nothing is drawn unless something changed. An untouched window renders no frames.

```
Property changes → SetLayoutProperty / SetRenderProperty (no-op if the value is the same)
    ↓
InvalidateLayout() marks the widget and its ancestors layout-dirty (stopping at one already marked); Invalidate() marks nothing
    ↓
Host.RequestFrame() → IPlatform.RequestFrame()   (once per frame, however many changes)
    ↓
UiHost.Render(): animation-frame callbacks → layout if root.IsLayoutDirty → overlays if needed → draw everything
```

Layout starts at the root but returns at once from every clean branch; drawing redraws the whole window,
skipping widgets outside the clip. Theme and font changes bump a host generation that makes every widget
measure again, without walking the tree.
Pointer input lays out a pending change first, so hit-testing matches what the next frame will show.

Widgets never change their own or other widgets' state inside `MeasureCore`, `ArrangeCore` or `DrawCore`:
an invalidation there schedules another frame, and a widget that does it every frame keeps the window busy.
Colors that depend on the theme are picked at draw time through `BackgroundSource` / `Label.ColorSource`.

Things that change over time use `UiHost.Platform.StartTimer` (caret blink, tooltip delay) or
`UiHost.RequestAnimationFrame` (smooth animation, re-requested each frame while it runs).

## Adding a new widget (minimum viable)

```csharp
public class Meter : Widget
{
    public float Value { get; set => SetRenderProperty(ref field, Math.Clamp(value, 0f, 1f)); }

    protected override Size MeasureCore(Size available)
        => new(float.IsInfinity(available.Width) ? 0f : available.Width, Theme.ProgressBarHeight);

    protected override void DrawCore(DrawContext ctx)
    {
        var t = ctx.Theme;
        float radius = LayoutBounds.Height / 2f;
        ctx.FillRoundRect(LayoutBounds, radius, t.Surface);
        ctx.FillRoundRect(LayoutBounds with { Width = LayoutBounds.Width * Value }, radius, t.Primary);
    }
}
```

- Properties use `SetLayoutProperty` (changes size or position) or `SetRenderProperty` (changes looks only).
- Sizes and colors come from `Theme` tokens, never literals.
- Drawing goes through `DrawContext` helpers, which allocate nothing; cache anything else you build.
- Input: override the `On…` methods and set `e.Handled` only for what the widget acts on.
- Add it to the Gallery, add tests (see below) and an `UPDATE.md` entry.

## Testing

`tests/ApexUI.Tests` runs the whole framework without a window. Copy the pattern of the nearest test file.

| Helper | Use |
|---|---|
| `FixedBox(w, h)` | leaf that always asks for the same content size |
| `CountingBox(w, h)` | counts measure, arrange and draw passes; `Resize` invalidates its layout |
| `TestPanel` | container with public `Add` / `Remove` (default stack layout) |
| `TransformPanel` | container whose `ChildTransform` the test sets, like a pan/zoom view |
| `TestPlatform.Show(root)` | real `UiHost` on a fake `IPlatform`: `Click`, `Move`, `Press`, `Release`, `Key`, `Type`, `Render`, `Advance` (timers), `FramesRequested`, `Cursor`, clipboard |
| `TestCanvas.Draw` / `MinAllocatedBytes` | off-screen drawing; bytes allocated by an action after warm-up |

- **Layout** of a single widget or panel: call `Measure` and `Arrange` directly and assert `DesiredSize` and `LayoutBounds`. No host needed.
- **Input, focus, invalidation**: `TestPlatform.Show`, then send events in surface pixels. Call `platform.Render()` (and dispose the bitmap) when a test depends on layout having run.
- **Hot paths** (draw, layout, pointer moves): assert zero allocations with `MinAllocatedBytes`.
- **Every bug fix** gets a regression test named after the behavior, not the bug.

## What to build next

| What | Why |
|------|-----|
| Value animator on `RequestAnimationFrame` | Smooth hover and press transitions |
| File dialogs in `DesktopPlatform` | `OpenFileAsync` / `SaveFileAsync` are declared but not implemented |
| Web backend | A second `IPlatform` for WebAssembly and the browser canvas |

## C# 14 features used and why

| Feature | Where used | Why |
|---------|-----------|-----|
| `field` keyword | widget properties | No separate backing field per property |
| Extension members | `SKColorExtensions.cs` | `SKColor.FromHex()`, `color.WithAlpha()` |
| Records (`record struct`) | `Rect`, `Size`, `Thickness`, `Transform2D` | Value equality and `with` expressions |
| Struct enumerator | `ChildList` | `foreach` over children allocates nothing |

## Running

```bash
dotnet run --project src/Gallery              # debug build, opens the Gallery
dotnet run --project src/Gallery -c Release   # optimized
dotnet test                                       # run ApexUI.Tests
```
