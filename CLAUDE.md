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
// Override in subclasses
protected virtual Size MeasureCore(Size available);
protected virtual void ArrangeCore(Rect finalRect);
protected virtual void DrawCore(DrawContext ctx);

Widget? HitTest(float x, float y);
void Invalidate();        // visual-only dirty
void InvalidateLayout();  // layout+visual dirty

// Child management (protected)
void AddChild(Widget child);
void RemoveChild(Widget child);
void RemoveAllChildren();  // public
protected virtual void OnChildRemoved(Widget child);  // drop per-child bookkeeping
// AddChild moves a child that already has a parent.

// Layout
float Width, Height;           // NaN = auto
float MinWidth, MaxWidth, MinHeight, MaxHeight;
float AspectRatio;             // NaN = no constraint
AspectRatioMode AspectRatioMode;
Thickness Margin, Padding;
HAlign HAlign;  // Left | Center | Right | Stretch
VAlign VAlign;  // Top | Center | Bottom | Stretch

// Visual
SKColor Background;
Func<Theme, SKColor>? BackgroundSource;  // theme-aware, overrides Background
float CornerRadius;
float Opacity;   // 0..1
bool  IsVisible;

bool IsEnabled, IsHovered, IsPressed, IsHitTestVisible;
Size DesiredSize;
Rect LayoutBounds;

// Events
Action<PointerEvent>? OnClick, OnPointerDown, OnPointerUp, OnPointerMove, OnPointerEnter, OnPointerExit;
Action<KeyEvent>?     OnKeyDown, OnKeyUp;
Action<TextInputEvent>? OnTextInput;   // typed text, separate from keys
UiHost? Host;   // host showing this widget's tree; null while not on screen
Action<float, float>? OnScroll;   // (deltaX, deltaY)

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
Theme     Theme      { get; set; }
string    FontFamily { get; set; }
float     DpiScale   { get; }       // display scaling reported by the platform
float     PixelScale { get; }       // DpiScale × UiScale, applied once as the canvas transform
float     UiScale    { get; set; }   // clamped 0.1–10; scales entire tree uniformly

UiHost BindUiScale(Bindable<float>)
UiHost BindTheme(Bindable<string>)
UiHost BindDarkMode(Bindable<bool>)  // true → Dark, false → Light
UiHost BindFontFamily(Bindable<string>)
```

### `IPlatform` · `lib/ApexUI/Core/IPlatform.cs`

The only seam to the OS. Nothing outside `lib/ApexUI.Desktop/` may reference Silk.NET.

```csharp
void Run(IPlatformListener listener)   // call listener.Resize before the first Render
void RequestFrame()
string? GetClipboardText(); void SetClipboardText(string)
void SetCursor(CursorShape)
IDisposable StartTimer(TimeSpan interval, Action tick)   // UI thread; dispose to stop
Task<string?> OpenFileAsync(FileDialogOptions) / SaveFileAsync(FileDialogOptions)   // not implemented on desktop yet

// IPlatformListener: Resize, Render(SKCanvas, dt), PointerMove/Down/Up, Wheel, KeyDown/KeyUp(Key, KeyModifiers), TextEntered(string), Close
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
DrawContext(SKCanvas canvas, Theme theme, float pixelScale, string fontFamily = FontCache.DefaultFamily, FontCache? fonts = null)
// IDisposable: owns reusable paints, a path, blur filters, and the font cache unless one is passed in.
SKCanvas Canvas   { get; }
Theme    Theme    { get; }
float    PixelScale { get; }   // canvas pixels per logical unit; drawing is already scaled, use only for pixel snapping

SKFont  GetFont(float sizePx, bool bold = false)   // cached; owned by the context, never dispose
SKPaint MakePaint(SKColor color, bool antialias = true)   // new object, caller disposes
SKFont  MakeTextFont(float sizePx, bool bold = false)      // new object, caller disposes
SKPaint MakeTextPaint(SKColor color)                       // new object, caller disposes

// FontCache · lib/ApexUI/Core/FontCache.cs
SKFont Get(string family, float sizePx, bool bold = false) // cached; never dispose the result

record struct PointerEvent(float X, float Y, PointerButton Button, bool IsDown)
enum PointerButton { None, Left, Right, Middle }

// InputEvents · lib/ApexUI/Core/InputEvents.cs
record struct KeyEvent(Key Key, KeyModifiers Modifiers, bool IsDown)   // + Ctrl, Shift, Alt, Super
record struct TextInputEvent(string Text)
enum Key { A..Z, D0..D9, F1..F12, Enter, Escape, Left, Right, ... }   // physical keys
[Flags] enum KeyModifiers { None, Shift, Control, Alt, Super }
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
static Theme Theme.Light, Theme.Dark

// SKColor (init-only)
Background, Surface, SurfaceHover, SurfacePressed
OnSurface, OnSurfaceMuted
Primary, PrimaryHover, OnPrimary
Success, Warning, Danger, Border

string FontFamily   // "Segoe UI"
float  FontSizeBase, FontSizeSmall, FontSizeLarge, FontSizeTitle
float  CornerRadiusSm, CornerRadiusMd, CornerRadiusLg
float  SpacingXs, SpacingSm, SpacingMd, SpacingLg
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
// Only rows inside the visible viewport are measured and drawn.
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

- **Tab focus navigation** — focus only moves on click; implement `Tab`/`Shift+Tab` traversal in `UiHost` across focusable widgets (`TextInput` etc.)
- **Animation primitives** — add a simple `Lerp`-based value animator driven by the tick loop for smooth hover/press transitions
