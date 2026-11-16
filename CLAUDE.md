# ApexUI — Code Organization Rules

## Projects and namespaces

Folder rule: **`lib/` holds library projects only** (one subfolder per project: `lib/ApexUI/` platform-neutral core, `lib/ApexUI.Desktop/` desktop backend). **`src/` holds only code that uses ApexUI**: examples and apps. Tests live in `tests/`.

| Folder | Project | Namespace |
|--------|---------|-----------|
| `lib/ApexUI/Core/` | `ApexUI` (class library) | `ApexUI.Core` |
| `lib/ApexUI/Layout/` | `ApexUI` | `ApexUI.Layout` |
| `lib/ApexUI/Widgets/` | `ApexUI` | `ApexUI.Widgets` |
| `lib/ApexUI/Extensions/` | `ApexUI` | `ApexUI.Extensions` |
| `lib/ApexUI.Desktop/` | `ApexUI.Desktop` (Silk.NET backend) | `ApexUI.Desktop` |
| `tests/ApexUI.Tests/` | `ApexUI.Tests` (xUnit) | `ApexUI.Tests` |
| `src/Gallery/Examples/` | `Gallery` (exe) | `ApexUI.Gallery.Examples` |

`lib/ApexUI/GlobalUsings.cs` imports all `ApexUI.*` + `SkiaSharp` inside the library only. Consumers (Gallery, tests, apps) declare their own usings.

## Rules

1. **Reusable framework code → `lib/ApexUI/`** (or another `lib/` project) with an `ApexUI.*` namespace. SDK globbing picks up new files; no `<Compile>` entries.
2. **Demo code → `src/Gallery/`**. Every widget appears there with its main states.
3. **New namespace folder in `lib/ApexUI/`**: add `global using ApexUI.NewNs;` to `lib/ApexUI/GlobalUsings.cs` and to the Gallery's and tests' `GlobalUsings.cs`.
4. **NuGet**: framework deps in `lib/ApexUI/ApexUI.csproj` with exact versions; app-only deps in the app's project.
5. **Examples → `src/Gallery/Examples/`**: one `Widget` subclass per file, named `<Topic>Example`, all state via constructor closures.
6. **Tests** for layout, input routing and state changes go in `tests/ApexUI.Tests/`. Layout can be tested without a window.
7. **`UPDATE.md`**: every addition, change and API break gets an entry.

---

## Build profiles

Publish profiles live in `src/Gallery/Gallery.csproj`.

| Profile | Command | Output |
|---|---|---|
| `Full` (default) | `dotnet publish src/Gallery` | `src/Gallery/bin/Publish/Full/` |
| `SingleFile` | `dotnet publish src/Gallery -p:BuildProfile=SingleFile` | `src/Gallery/bin/Publish/SingleFile/` |
| `MinSize` | `dotnet publish src/Gallery -p:BuildProfile=MinSize` | `src/Gallery/bin/Publish/MinSize/` (trimmed) |

Development: `dotnet build`, `dotnet test`, `dotnet run --project src/Gallery`.

---

## Core API

### `Widget` · `lib/ApexUI/Core/Widget.cs`

```csharp
// Override in subclasses (Widget.Layout.cs). Both work inside Padding: MeasureCore gets the space inside
// Margin and Padding and returns the content size; ArrangeCore gets ContentBounds and must arrange every
// visible child (a skipped one keeps its previous slot). Default: children overlap (Stack).
protected virtual Size MeasureCore(Size available);
protected virtual void ArrangeCore(Rect finalRect);
protected virtual void DrawCore(DrawContext ctx);
// Measure/Arrange are cached: same constraint / rect and not invalidated → return at once.

Widget? HitTest(float x, float y);   // (x, y) in this widget's layout space
void Invalidate();        // redraw on the next frame (asks the host for one)
void InvalidateLayout();  // re-layout and redraw on the next frame; stops at an ancestor already marked

// Per-child data for custom panels (cleared when the child leaves its parent)
protected static object? GetParentData(Widget child);
protected static void SetParentData(Widget child, object? data);

// Coordinates: LayoutBounds are absolute in the widget's layout space.
Transform2D ChildTransform { get; protected set; }   // maps children's space into this one (draw, hit-test, pointer X/Y); render-only
Rect WindowBounds;                                    // LayoutBounds through all ancestors' ChildTransform
(float X, float Y) PointFromWindow(float x, float y);
// Setters: `set => SetLayoutProperty(ref field, value);` or `SetRenderProperty(...)`; both skip unchanged values.
// Never change widget state inside MeasureCore/ArrangeCore/DrawCore: an invalidation there schedules another frame.

// Child management (protected)
void AddChild(Widget child);
void RemoveChild(Widget child);
void RemoveAllChildren();  // public
protected virtual void OnChildRemoved(Widget child);  // drop per-child bookkeeping
// AddChild moves a child that already has a parent (no detach/attach when it stays in the same window).
ChildList Children;   // read-only; foreach uses a struct enumerator, no allocation

// Lifecycle: a widget is attached while it is in a tree shown by a UiHost (root or an overlay).
UiHost? Host;                                        // null while not attached
protected virtual void OnAttached(UiHost host);      // Host already set
protected virtual void OnDetached();                 // Host still set; release resources here
protected IDisposable Observe<T>(Bindable<T> source, Action<T> onChanged);         // use in Bind methods
protected IDisposable Observe<T>(ObservableList<T> source, Action onChanged);
// Observe subscribes now, unsubscribes on detach, re-subscribes and re-syncs on attach. Never write `source.Changed +=` in a widget.

// Layout
float Width, Height;           // NaN = auto; include Padding
float MinWidth, MaxWidth, MinHeight, MaxHeight;
float AspectRatio;             // NaN = no constraint
AspectRatioMode AspectRatioMode;
Thickness Margin;              // outside LayoutBounds
Thickness Padding;             // inside LayoutBounds, applied by Widget for every subclass; DefaultPadding(Theme) until set
float Grow;                    // share of a LinearPanel's leftover length (0 = none)
HAlign HAlign;  // Left | Center | Right | Stretch
VAlign VAlign;  // Top | Center | Bottom | Stretch

// Visual
SKColor Background;
Func<Theme, SKColor>? BackgroundSource;  // theme-aware, overrides Background (Label has the same ColorSource)
float CornerRadius;   // DefaultCornerRadius(Theme) until set
float Opacity;   // 0..1
bool  IsVisible;

bool IsEnabled, IsHitTestVisible;
bool IsHovered;   // pointer over this widget or a descendant
bool IsPressed;   // pressed here, until all buttons are released
bool IsFocused;
bool Focusable;   // takes focus by click, Tab/Shift+Tab, Focus()
CursorShape? Cursor;   // null = parent's cursor
bool HasPointerCapture;
bool Focus();
bool CapturePointer(); void ReleasePointerCapture();
Size DesiredSize;
Rect LayoutBounds;
protected Theme Theme;   // host's theme, Theme.Light while detached; read it in MeasureCore/ArrangeCore/DrawCore
protected virtual Thickness DefaultPadding(Theme theme);      // themed defaults; a set value wins
protected virtual float     DefaultCornerRadius(Theme theme);
// Never hardcode colors, font sizes, radii, paddings or control sizes in a widget: use a Theme token.
Rect ContentBounds;   // LayoutBounds minus Padding

// Input (Widget.Input.cs). Each event: protected virtual On…(args) first (widget behavior), then the public event.
// Routed (bubble from Source to the root until e.Handled):
event Action<PointerEventArgs>?   PointerPressed, PointerReleased, PointerMoved, Click;  // Click = left press+release on the same widget
event Action<WheelEventArgs>?     Wheel;
event Action<KeyEventArgs>?       KeyDown, KeyUp;      // from the focused widget
event Action<TextInputEventArgs>? TextEntered;         // typed text, separate from keys
// Not routed:
event Action<PointerEventArgs>? PointerEntered, PointerExited;   // per widget in the hover chain
event Action? GotFocus, LostFocus, PointerCaptureLost;
protected virtual void DrawFocusRing(DrawContext ctx);   // drawn when focused from the keyboard; override to change or hide
// Built-in widgets override the On… methods; set e.Handled only for what the widget acts on.
// Event args objects are reused by the host: read them during the call, never store them.

enum HAlign          { Left, Center, Right, Stretch }
enum VAlign          { Top, Center, Bottom, Stretch }
enum AspectRatioMode { WidthControlsHeight, HeightControlsWidth }
```

---

### `Bindable<T>` · `lib/ApexUI/Core/Bindable.cs`

```csharp
Bindable<T>(T initial = default!)
T Value { get; set; }
event Action<T>? Changed
```

---

### `UiHost` · `lib/ApexUI/Core/UiHost.cs`

Platform-neutral app host: tree, layout, drawing, input routing, overlays, focus, theme. Implements `IPlatformListener`.

```csharp
UiHost(IPlatform platform)
void Run(Widget root)          // sets Root, then platform.Run(this)
IPlatform Platform   { get; }
Widget?   Root       { get; set; }
Theme     Theme      { get; set; }   // re-measures everything when changed
ThemeRegistry Themes { get; }       // per host; starts with BuiltInThemes; register before building theme pickers
float     DpiScale   { get; }       // display scaling reported by the platform
float     PixelScale { get; }       // DpiScale × UiScale, applied once as the canvas transform
float     UiScale    { get; set; }   // clamped 0.1–10; scales entire tree uniformly

UiHost BindUiScale(Bindable<float>)
UiHost BindTheme(Bindable<string>)      // name in Themes
UiHost BindDarkMode(Bindable<bool>)     // dark or light variant of that name
UiHost BindFontFamily(Bindable<string>) // overrides the selected theme's FontFamily

void RequestAnimationFrame(Action<TimeSpan> callback)   // once, before the next frame; re-request to animate. Cache the delegate.
// Frames are drawn only when something invalidates; an idle window renders nothing.

Widget? FocusedWidget { get; }
Widget? PointerCapture { get; }
bool    IsFocusVisible { get; }    // focus came from the keyboard → focus rings are drawn
bool    MoveFocus(bool forward);   // Tab order; stays inside the topmost modal overlay
void    ClearFocus();
event Action<KeyEventArgs>? UnhandledKeyDown;   // keys no widget handled (app shortcuts); runs before Tab navigation
```

### `IPlatform` · `lib/ApexUI/Core/IPlatform.cs`

The only seam to the OS. Nothing outside `lib/ApexUI.Desktop/` may reference Silk.NET.

```csharp
void Run(IPlatformListener listener)   // call listener.Resize before the first Render
void RequestFrame()            // render once, soon; render only when asked (and after Resize)
TimeSpan Now { get; }          // monotonic clock (same as Render's frameTime); used for double-click timing
string? GetClipboardText(); void SetClipboardText(string)
void SetCursor(CursorShape)
IDisposable StartTimer(TimeSpan interval, Action tick)   // UI thread; dispose to stop
Task<string?> OpenFileAsync(FileDialogOptions) / SaveFileAsync(FileDialogOptions)   // not implemented on desktop yet

// IPlatformListener: Resize, Render(SKCanvas, TimeSpan frameTime), PointerMove/Down/Up(…, KeyModifiers), Wheel(dx, dy, KeyModifiers),
//                    KeyDown/KeyUp(Key, KeyModifiers) (repeat KeyDown while held), TextEntered(string), Close
// Resize(pixelWidth, pixelHeight, dpiScale). Sizes and pointer positions are surface pixels; UiHost converts to logical units.
// Everything above the platform (layout, drawing, fonts, input) is in logical units.
```

### `DesktopPlatform` · `lib/ApexUI.Desktop/DesktopPlatform.cs`

```csharp
DesktopPlatform(string title = "ApexUI App", int width = 900, int height = 600)
DesktopPlatform WithIcon(string path)   // .svg / .ico / raster; call before Run()
```

---

### `DrawContext` · `lib/ApexUI/Core/DrawContext.cs`

```csharp
DrawContext(SKCanvas canvas, Theme theme, float pixelScale, FontCache? fonts = null)
// IDisposable: owns reusable paints, a path, blur filters, and the font cache unless one is passed in.
SKCanvas Canvas   { get; }
Theme    Theme    { get; }
string   FontFamily { get; }   // Theme.FontFamily
float    PixelScale { get; }   // canvas pixels per logical unit; drawing is already scaled, use only for pixel snapping

SKFont  GetFont(float sizePx, bool bold = false)   // cached; owned by the context, never dispose
SKPaint MakePaint(SKColor color, bool antialias = true)   // new object, caller disposes
SKFont  MakeTextFont(float sizePx, bool bold = false)      // new object, caller disposes
SKPaint MakeTextPaint(SKColor color)                       // new object, caller disposes

// FontCache · lib/ApexUI/Core/FontCache.cs
SKFont Get(string family, float sizePx, bool bold = false) // cached; never dispose the result

// InputEvents · lib/ApexUI/Core/InputEvents.cs
abstract class InputEventArgs { Widget Source; bool Handled; KeyModifiers Modifiers; bool Ctrl, Shift, Alt, Super; }
abstract class PositionedEventArgs : InputEventArgs { float X, Y; float WindowX, WindowY; }
// X, Y: in the receiving widget's layout space (re-mapped while bubbling past a ChildTransform).
// WindowX, WindowY: logical window coordinates (open overlays with these).
sealed class PointerEventArgs : PositionedEventArgs { PointerButton Button; int ClickCount; }
sealed class WheelEventArgs   : PositionedEventArgs { float DeltaX, DeltaY; }   // notches; +DeltaY = up
sealed class KeyEventArgs       { Key Key; bool IsRepeat; }
sealed class TextInputEventArgs { string Text; }
enum PointerButton { None, Left, Right, Middle }
enum Key { A..Z, D0..D9, F1..F12, Enter, Escape, Left, Right, ... }   // physical keys
[Flags] enum KeyModifiers { None, Shift, Control, Alt, Super }
```

---

### `Transform2D` · `lib/ApexUI/Core/Transform2D.cs`

```csharp
readonly record struct Transform2D(float OffsetX, float OffsetY, float Scale)   // p → p × Scale + Offset; Scale > 0
static Transform2D Identity
static Transform2D Translation(float x, float y)
bool IsIdentity
(float X, float Y) Apply(float x, float y);  (float X, float Y) Invert(float x, float y);  Rect Apply(Rect r)
```

---

### `LinearPanel`, `Row`, `Column`, `Spacer` · `lib/ApexUI/Layout/`

```csharp
enum Orientation { Horizontal, Vertical }
abstract class LinearPanel : Widget { Orientation Orientation { get; }  float Spacing; }   // Spacing: DefaultSpacing(Theme) until set
// Children get their desired length along the line and the full thickness across it; leftover length goes
// to children with Grow > 0 in proportion. Subclasses: Row, Column, RadioGroup<T>, Tabs' bar.
Row(params Widget[] children)    Row Add(Widget)    Row WithSpacing(float)
Column(params Widget[] children) Column Add(Widget) Column WithSpacing(float)
Spacer(float grow = 1f)          // empty, not hit-testable, Grow = grow
```

---

### `Rect` · `lib/ApexUI/Core/Rect.cs`

```csharp
readonly record struct Rect(float X, float Y, float Width, float Height)
static Rect Rect.Zero
float Right, Bottom, CenterX, CenterY
bool  Contains(float px, float py)
bool  Intersects(Rect other)
Rect  Deflate(Thickness t)
Rect  Translate(float dx, float dy)
Rect  WithSize(float w, float h)
SKRect ToSKRect()
```

---

### `Size` · `lib/ApexUI/Core/Size.cs`

```csharp
readonly record struct Size(float Width, float Height)
static Size Size.Zero, Size.Infinite
Size Constrain(Size constraint)
```

---

### `Thickness` · `lib/ApexUI/Core/Thickness.cs`

```csharp
readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
Thickness(float uniform)
Thickness(float horizontal, float vertical)
static Thickness Thickness.Zero
float Horizontal, Vertical
```

---

### `Theme` · `lib/ApexUI/Core/Theme.cs`

```csharp
sealed record Theme   // immutable; make variants with `with`
static Theme Theme.Light, Theme.Dark

// SKColor (* = derived from another token unless set)
Background, Surface, SurfaceHover, SurfacePressed
OnSurface, OnSurfaceMuted
Primary, PrimaryHover, OnPrimary, PrimaryHoverGhost*
Success, Warning, Danger
Border, BorderSubtle*
Scrim, Shadow (+ float ShadowBlur, ShadowOffsetY)
FocusRing* (+ float FocusRingWidth), Selection*, Knob
ScrollbarTrack*, ScrollbarThumb*, ScrollbarThumbActive* (+ float ScrollbarSize, ScrollbarMinThumb)

string FontFamily   // "Segoe UI"
float  FontSizeSmall, FontSizeBody, FontSizeSubheading, FontSizeHeading, FontSizeTitle, LineHeight (multiplier)
float  FontSize(TextRole role)   // enum TextRole { Body, Small, Subheading, Heading, Title }; Label.Role
float  CornerRadiusSmall, CornerRadiusControl, CornerRadiusPopup, CornerRadiusDialog
float  SpacingXs, SpacingSm, SpacingMd, SpacingLg
Thickness ButtonPadding, InputPadding, MenuItemPadding, PopupPadding, TooltipPadding, DialogPadding, TabPadding
float  CheckboxSize, RadioSize, ToggleWidth, ToggleHeight, SliderThumbSize, SliderTrackHeight, ProgressBarHeight, TabBarSize
```

### `ThemeRegistry`, `BuiltInThemes` · `lib/ApexUI/Core/`

```csharp
ThemeRegistry(bool includeBuiltIns = true)
IReadOnlyList<string> Names; string DefaultName   // first registered
void  Register(string name, Theme light, Theme dark)   // replacing keeps the position
void  Remove(string name)
Theme Get(string name, bool dark)                      // unknown name → first theme

static class BuiltInThemes   // names Default, Contrast, Forest, Desert, Space; palettes ContrastLight, ForestDark, ...
```

---

### `SKColorExtensions` · `lib/ApexUI/Extensions/SKColorExtensions.cs`

```csharp
static SKColor SKColor.FromHex(string hex)   // "#RRGGBB" or "#RRGGBBAA"
bool    color.IsTransparent
SKColor color.WithAlpha(float alpha)         // 0..1
SKColor color.Lighten(float ratio)
SKColor color.Darken(float ratio)
```

---

### `DrawContext` shapes · `lib/ApexUI/Core/DrawContext.Shapes.cs`

```csharp
void FillRect(Rect r, SKColor color)
void FillRoundRect(Rect r, float radius, SKColor color)
void StrokeRoundRect(Rect r, float radius, SKColor color, float thickness = 1f)  // inset stroke
void FillCircle(float cx, float cy, float radius, SKColor color)
void StrokeCircle(float cx, float cy, float radius, SKColor color, float thickness = 1f)  // inset stroke
void FillOval(Rect r, SKColor color)
void DrawLine(float x0, float y0, float x1, float y1, SKColor color, float thickness = 1f, SKStrokeCap cap = Round)
void FillTriangle(float x0, float y0, float x1, float y1, float x2, float y2, SKColor color)
void StrokeTriangle(...)
void DrawShadow(Rect r, float cornerRadius, float blur, SKColor color)
void DrawChevron(float cx, float cy, float size, bool pointUp, SKColor color, float thickness = 1.5f)
void DrawCheckmark(float cx, float cy, float size, SKColor color, float thickness = 2f)
void DrawCross(float cx, float cy, float size, SKColor color, float thickness = 2f)
```

### `DrawContext` text · `lib/ApexUI/Core/DrawContext.Text.cs`

```csharp
void  DrawText(string text, Rect bounds, SKColor color, float sizePx, bool bold = false, SKTextAlign align = Left)
void  DrawText(SKTextBlob? blob, Rect bounds, SKColor color, float sizePx, bool bold = false)   // left-aligned; no allocation, cache the blob
float MeasureText(string text, float sizePx, bool bold = false)
float MeasureText(ReadOnlySpan<char> text, float sizePx, bool bold = false)  // no substring needed
```

### `DrawContext` images · `lib/ApexUI/Core/DrawContext.Images.cs`

```csharp
void DrawImage(SKImage image, Rect dest)
void DrawImage(SKImage image, SKRect src, Rect dest)
void DrawPicture(SKPicture picture, Rect dest, float naturalW, float naturalH)
```

### `DrawContext` clip · `lib/ApexUI/Core/DrawContext.Clip.cs`

```csharp
ClipScope PushClip(Rect r)              // rectangular clip
ClipScope PushClip(Rect r, float radius) // rounded-rect clip
// ClipScope is IDisposable — use with `using` to auto-restore canvas state
```

### `Checkbox` · `lib/ApexUI/Widgets/Checkbox.cs`

```csharp
Checkbox(bool isChecked = false, string label = "", Action<bool>? onChanged = null)
bool     IsChecked  { get; set; }
string   Label      { get; set; }
Action<bool>? OnChanged

Checkbox WithLabel(string label)
Checkbox WithChecked(bool value)
Checkbox OnChange(Action<bool> action)
Checkbox Bind(Bindable<bool> source)
```

### `Wrap` · `lib/ApexUI/Layout/Wrap.cs`

```csharp
Wrap(params Widget[] children)
float HorizontalSpacing { get; set; }
float VerticalSpacing   { get; set; }

Wrap WithSpacing(float horizontal, float vertical)
```

### `Canvas` · `lib/ApexUI/Layout/Canvas.cs`

```csharp
// Children positioned at explicit X/Y coordinates (relative to Canvas origin).
Canvas Add(Widget child, float x, float y)                              // natural child size
Canvas Add(Widget child, float x, float y, float width, float height)  // explicit size
Canvas Move(Widget child, float x, float y)    // reposition an existing child
Canvas RemoveChild(Widget child)               // remove from canvas

// Canvas auto-sizes to the bounding box of all children unless Width/Height are set explicitly.
```

---

### `ObservableList<T>` · `lib/ApexUI/Core/ObservableList.cs`

```csharp
ObservableList<T>()
ObservableList<T>(IEnumerable<T> items)

event Action? Changed   // fires after any structural change

T    this[int i]         { get }
int  Count               { get }
bool Contains(T item)
int  IndexOf(T item)

void Add(T item)
void Insert(int i, T item)
bool Remove(T item)
void RemoveAt(int i)
void Set(int i, T item)
void AddRange(IEnumerable<T> items)
void Clear()
```

### `VirtualList<T>` · `lib/ApexUI/Widgets/VirtualList.cs`

```csharp
// Only rows near the visible viewport are built and laid out; scrolling moves them with ChildTransform
// and runs layout only when it reaches rows that are not built yet.
// Row height must be uniform. Set Height (or constrain via parent) to make the list scrollable;
// if Height=NaN and available height is ∞, sizes to full content height.

VirtualList<T>()
float RowHeight  { get; set; }   // default 36

VirtualList<T> WithItems(ObservableList<T> source)   // subscribes to Changed
VirtualList<T> WithItems(IReadOnlyList<T> source)
VirtualList<T> WithTemplate(Func<T, Widget> t)
VirtualList<T> WithTemplate(Func<T, int, Widget> t)  // (item, rowIndex) → Widget
VirtualList<T> WithRowHeight(float h)
```

### `Dropdown<T>` — live collection overloads

```csharp
Dropdown<T> WithItems(ObservableList<T> source, Func<T, string> labelOf)
Dropdown<T> WithItems(ObservableList<T> source)   // uses ToString()
```

---

## TODO

- **Animation primitives** — add a simple `Lerp`-based value animator driven by `UiHost.RequestAnimationFrame` for smooth hover/press transitions
