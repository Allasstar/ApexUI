# ApexUI — Code Organization Rules

## Namespaces

| Folder | Namespace |
|--------|-----------|
| `lib/Core/` | `ApexUI.Core` |
| `lib/Layout/` | `ApexUI.Layout` |
| `lib/Widgets/` | `ApexUI.Widgets` |
| `lib/Extensions/` | `ApexUI.Extensions` |
| `src/Examples/` | `ApexUI.App.Examples` |
| `src/Screens/` | `ApexUI.App.Screens` |
| `src/Widgets/` | `ApexUI.App.Widgets` |
| `src/Models/` | `ApexUI.App.Models` |

`GlobalUsings.cs` globally imports all `ApexUI.*` + `SkiaSharp` — do **not** repeat in `src/` or `Program.cs`.
`ApexUI.App.*` are **not** global — add explicit `using` per file.

## Rules

1. **Reusable code → `lib/`** with `ApexUI.*` namespace. No `<Compile>` entries needed — wildcard picks it up.
2. **App-specific code → `src/`** with `ApexUI.App.*` namespace. No csproj changes needed.
3. **Never add `<Compile>` for lib files** — causes duplicate-compile error.
4. **New `lib/` subfolder**: add `.cs` files + `global using ApexUI.NewNs;` to `GlobalUsings.cs`. Nothing else.
5. **NuGet**: framework dep → `lib/ApexUI.props`; app-only dep → `ApexUI.csproj`.
6. **Examples → `src/Examples/`**: one `Widget` subclass per file, named `<Topic>Example`, all state via constructor closures.

---

## Build profiles

| Profile | Command | Output |
|---|---|---|
| `Full` (default) | `dotnet publish` | `bin/Publish/Full/` |
| `SingleFile` | `dotnet publish -p:BuildProfile=SingleFile` | `bin/Publish/SingleFile/` |
| `MinSize` | `dotnet publish -p:BuildProfile=MinSize` | `bin/Publish/MinSize/` (trimmed) |

Development: `dotnet run` / `dotnet build`. Self-contained publish always true.

---

## Core API

### `Widget` · `lib/Core/Widget.cs`

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
Action<float, float>? OnScroll;   // (deltaX, deltaY)

enum HAlign          { Left, Center, Right, Stretch }
enum VAlign          { Top, Center, Bottom, Stretch }
enum AspectRatioMode { WidthControlsHeight, HeightControlsWidth }
```

---

### `Bindable<T>` · `lib/Core/Bindable.cs`

```csharp
Bindable<T>(T initial = default!)
T Value { get; set; }
event Action<T>? Changed
```

---

### `Application` · `lib/Core/Application.cs`

```csharp
Application(string title = "ApexUI App", int width = 900, int height = 600)
void Run(Widget root)
Theme Theme    { get; set; }
float DpiScale { get; }
float UiScale  { get; set; }   // clamped 0.1–10; scales entire tree uniformly

Application SetIcon(string path)          // .svg / .ico / raster; call before Run()
Application BindUiScale(Bindable<float>)
Application BindDarkMode(Bindable<bool>)  // true → Dark, false → Light
```

---

### `DrawContext` · `lib/Core/DrawContext.cs`

```csharp
SKCanvas Canvas   { get; }
Theme    Theme    { get; }
float    DpiScale { get; }

SKPaint MakePaint(SKColor color, bool antialias = true)
SKFont  MakeTextFont(float sizePx, bool bold = false)
SKPaint MakeTextPaint(SKColor color)

record struct PointerEvent(float X, float Y, PointerButton Button, bool IsDown)
record struct KeyEvent(string Key, bool IsDown, bool Ctrl, bool Shift, bool Alt)
enum PointerButton { None, Left, Right, Middle }
```

---

### `Rect` · `lib/Core/Rect.cs`

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

### `Size` · `lib/Core/Size.cs`

```csharp
readonly record struct Size(float Width, float Height)
static Size Size.Zero, Size.Infinite
Size Constrain(Size constraint)
```

---

### `Thickness` · `lib/Core/Thickness.cs`

```csharp
readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
Thickness(float uniform)
Thickness(float horizontal, float vertical)
static Thickness Thickness.Zero
float Horizontal, Vertical
```

---

### `Theme` · `lib/Core/Theme.cs`

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

### `SKColorExtensions` · `lib/Extensions/SKColorExtensions.cs`

```csharp
static SKColor SKColor.FromHex(string hex)   // "#RRGGBB" or "#RRGGBBAA"
bool    color.IsTransparent
SKColor color.WithAlpha(float alpha)         // 0..1
SKColor color.Lighten(float ratio)
SKColor color.Darken(float ratio)
```

---

### `DrawContext` shapes · `lib/Core/DrawContext.Shapes.cs`

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

### `DrawContext` text · `lib/Core/DrawContext.Text.cs`

```csharp
void  DrawText(string text, Rect bounds, SKColor color, float sizePx, bool bold = false, SKTextAlign align = Left)
float MeasureText(string text, float sizePx, bool bold = false)
```

### `DrawContext` images · `lib/Core/DrawContext.Images.cs`

```csharp
void DrawImage(SKImage image, Rect dest)
void DrawImage(SKImage image, SKRect src, Rect dest)
void DrawPicture(SKPicture picture, Rect dest, float naturalW, float naturalH)
```

### `DrawContext` clip · `lib/Core/DrawContext.Clip.cs`

```csharp
ClipScope PushClip(Rect r)              // rectangular clip
ClipScope PushClip(Rect r, float radius) // rounded-rect clip
// ClipScope is IDisposable — use with `using` to auto-restore canvas state
```

### `Checkbox` · `lib/Widgets/Checkbox.cs`

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

### `Wrap` · `lib/Layout/Wrap.cs`

```csharp
Wrap(params Widget[] children)
float HorizontalSpacing { get; set; }
float VerticalSpacing   { get; set; }

Wrap WithSpacing(float horizontal, float vertical)
```

### `Canvas` · `lib/Layout/Canvas.cs`

```csharp
// Children positioned at explicit X/Y coordinates (relative to Canvas origin).
Canvas Add(Widget child, float x, float y)                              // natural child size
Canvas Add(Widget child, float x, float y, float width, float height)  // explicit size
Canvas Move(Widget child, float x, float y)    // reposition an existing child
Canvas RemoveChild(Widget child)               // remove from canvas

// Canvas auto-sizes to the bounding box of all children unless Width/Height are set explicitly.
```

### `SKCanvasExtensions` · `lib/Extensions/SKCanvasExtensions.cs`

```csharp
void canvas.FillRoundRect(SKRect rect, float rx, SKColor color)
void canvas.StrokeRoundRect(SKRect rect, float rx, SKColor color, float strokeWidth = 1f)
void canvas.DrawTextCentered(string text, SKRect bounds, SKFont font, SKPaint paint)
```

---

## TODO

- **Virtualized list** — `Dropdown`/`MenuList` render all items; add a `VirtualList<T>` that only draws visible rows for large datasets
- **Tab focus navigation** — focus only moves on click; implement `Tab`/`Shift+Tab` traversal in `Application` across focusable widgets (`TextInput` etc.)
- **Collection binding** — add `ObservableList<T>` with a `Changed` event so `Dropdown`/`MenuList` can bind to live collections
- **Animation primitives** — add a simple `Lerp`-based value animator driven by the tick loop for smooth hover/press transitions
