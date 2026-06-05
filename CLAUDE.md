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

### `SKCanvasExtensions` · `lib/Extensions/SKCanvasExtensions.cs`

```csharp
void canvas.FillRoundRect(SKRect rect, float rx, SKColor color)
void canvas.StrokeRoundRect(SKRect rect, float rx, SKColor color, float strokeWidth = 1f)
void canvas.DrawTextCentered(string text, SKRect bounds, SKFont font, SKPaint paint)
```
