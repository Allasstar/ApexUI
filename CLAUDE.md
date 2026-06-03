# ApexUI — Code Organization Rules

## Folder layout

```
ApexUI/
├── ApexUI.csproj             ← imports lib/ApexUI.props; adds src/**/*.cs
├── Program.cs                 ← app entrypoint (no using ApexUI.* needed)
│
├── lib/                       ← framework source (versioned, replaceable as a unit)
│   ├── ApexUI.props          ← auto-includes lib/**/*.cs + all NuGet packages
│   ├── Core/
│   │   ├── Widget.cs          ← base class: Measure/Arrange/Draw
│   │   ├── Application.cs     ← Silk.NET window + event loop
│   │   ├── Theme.cs           ← all colors, fonts, spacing
│   │   ├── Rect.cs            ← Rect value type
│   │   ├── Size.cs            ← Size value type
│   │   ├── Thickness.cs       ← Thickness value type
│   │   ├── DrawContext.cs     ← canvas + theme passed into every Draw()
│   │   ├── Bindable.cs        ← Bindable<T> for two-way widget binding
│   │   ├── FrameworkInfo.cs   ← ApexUI.Version constant
│   │   └── GlobalUsings.cs    ← global using for all ApexUI namespaces + SkiaSharp
│   ├── Layout/
│   │   ├── Stack.cs           ← Stack panel
│   │   ├── Column.cs          ← Column panel
│   │   ├── Row.cs             ← Row panel
│   │   └── PaddingBox.cs      ← PaddingBox panel
│   ├── Widgets/
│   │   ├── Label.cs           ← Label widget
│   │   ├── Button.cs          ← Button widget
│   │   ├── TextInput.cs       ← TextInput widget (+ BindFloat/BindInt)
│   │   ├── Toggle.cs          ← Toggle widget
│   │   ├── Image.cs           ← Image widget (raster + SVG)
│   │   └── Slider.cs          ← Slider widget (float/int, optional step)
│   └── Extensions/
│       ├── SKColorExtensions.cs   ← C# 14 extension members on SKColor
│       └── SKCanvasExtensions.cs  ← C# 14 extension members on SKCanvas
│
└── src/                       ← app-specific code (untouched by framework updates)
    ├── Examples/              ← self-contained example widgets (one class per example)
    │   ├── CounterExample.cs      ← counter + text-input demo
    │   ├── ImageToggleExample.cs  ← image + toggle demo
    │   ├── SliderExample.cs       ← sliders with two-way binding to text inputs
    │   └── ScaleExample.cs        ← UI scale demo; exposes Bindable<float> Scale
    ├── Screens/               ← full-screen views
    ├── Widgets/               ← app-specific widgets (not framework reusable)
    └── Models/                ← app data models
```

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

`lib/Core/GlobalUsings.cs` adds `global using` for every `ApexUI.*` namespace and
`SkiaSharp`, so no file in `src/` or `Program.cs` needs explicit `using` statements
for framework types.

## Rules

### 1. Framework file → `lib/` with `ApexUI.*` namespace

If the code is reusable across apps (a new widget, a layout panel, a theme helper),
it lives in `lib/` under the matching namespace.

Example — adding a `ProgressBar` widget:
- File: `lib/Widgets/ProgressBar.cs`
- Namespace: `ApexUI.Widgets`
- No changes to `ApexUI.csproj` needed — `ApexUI.props` picks it up via wildcard.

### 2. App-specific file → `src/` with `ApexUI.App.*` namespace

If the code belongs to the app (a screen, a custom widget, a data model), it lives
in `src/` under the `ApexUI.App.*` namespace matching its subfolder.

Example — adding a settings screen:
- File: `src/Screens/SettingsScreen.cs`
- Namespace: `ApexUI.App.Screens`
- No changes to `ApexUI.csproj` needed — `src/**/*.cs` glob covers it.

### 3. Never add `<Compile>` entries in `ApexUI.csproj` for lib files

`lib/ApexUI.props` uses `$(MSBuildThisFileDirectory)**/*.cs` to auto-include
everything under `lib/`. Adding an explicit entry causes a duplicate-compile error.

### 4. New `lib/` subfolder — no changes needed anywhere

The `**/*.cs` wildcard in `ApexUI.props` picks up new subdirectories automatically.

Example — creating `lib/Animation/`:
- Create the folder and add `.cs` files with `namespace ApexUI.Animation`
- Add `global using ApexUI.Animation;` to `lib/Core/GlobalUsings.cs`
- Nothing else to touch.

### 5. NuGet packages

- Framework dependency (used by `lib/` code) → add to `lib/ApexUI.props`
- App-only dependency → add directly to `ApexUI.csproj`

### 6. `using` rules — framework vs app namespaces

`lib/Core/GlobalUsings.cs` already declares:
```csharp
global using ApexUI.Core;
global using ApexUI.Layout;
global using ApexUI.Widgets;
global using ApexUI.Extensions;
global using SkiaSharp;
```
Do **not** repeat these in `src/` files or `Program.cs`.

`ApexUI.App.*` namespaces are **not** globally imported — add an explicit `using` at
the top of each file that references them. This keeps the imports visible, which is
especially useful in examples where readers need to see where types come from.

Example — `Program.cs` running an example:
```csharp
using ApexUI.App.Examples;

new Application("ApexUI Demo", 800, 600) { Theme = Theme.Light }
    .Run(new CounterExample());
```

### 7. Example → `src/Examples/` with `ApexUI.App.Examples` namespace

A self-contained UI demonstration (one feature, one concept) lives in `src/Examples/`
as a single `Widget` subclass. The example builds its entire widget tree in the
constructor and needs no public API — `Program.cs` just instantiates it and passes it
to `Application.Run`.

Rules:
- One class per file, named `<Topic>Example` (e.g. `CounterExample`, `FormExample`).
- All state is private to the constructor via closures (same as top-level script style).
- No screen-specific logic, no models — keep it minimal and focused on one concept.

Example — adding a `SliderExample`:
- File: `src/Examples/SliderExample.cs`
- Namespace: `ApexUI.App.Examples`
- Switch `Program.cs` to `.Run(new SliderExample())` to run it.

### 8. CLAUDE.md must be updated when adding new scripts

Whenever you add, rename, or delete a `.cs` file anywhere in `lib/` or `src/`, you
**must** update this file in the same change:

1. **Folder layout tree** — add/remove/rename the file entry with a short description.
2. **API Reference section below** — add the new type's key signatures, or remove the
   entry for a deleted type.

This keeps CLAUDE.md usable as a quick-reference index so entire files don't need to
be re-read from scratch every session.

---

## Build profiles

Configured in `ApexUI.csproj` via the `$(BuildProfile)` MSBuild property.
Pass it on the command line — no file edits needed.

| Profile | Command | Output | Use when |
|---|---|---|---|
| `Full` (default) | `dotnet build` / `dotnet publish` | exe + all DLLs in output folder | development, easy debugging |
| `SingleFile` | `dotnet publish -p:BuildProfile=SingleFile` | one self-contained `.exe` | distribution, clean root folder |
| `MinSize` | `dotnet publish -p:BuildProfile=MinSize` | one trimmed + compressed `.exe` | smallest possible distributable |

**Runtime**: always `$(NETCoreSdkRuntimeIdentifier)` — auto-detects the build machine's platform (`win-x64`, `linux-x64`, `osx-arm64`, …). No manual change needed per OS.

**Self-contained**: always `true` — the .NET runtime is bundled so end-users need nothing installed.

**SingleFile note**: native libs (glfw3, SkiaSharp) are embedded and extracted to a temp folder on first launch (`IncludeNativeLibrariesForSelfExtract=true`).

**MinSize note**: uses `TrimMode=link` (aggressive). SkiaSharp relies on reflection — add `TrimmerRootDescriptor` entries if you hit `MissingMethodException` after trimming.

**Libs-subfolder**: .NET self-contained builds cannot redirect managed DLLs to a `libs/` subfolder — the runtime resolver requires them next to the exe. `SingleFile` is the practical equivalent (clean root, one file).

---

## API Reference

Signatures only — read the source for full implementation detail.

### `Widget` · `lib/Core/Widget.cs`

Base class for every UI element.

```csharp
// Override these three in subclasses
protected virtual Size MeasureCore(Size available);   // report desired size
protected virtual void ArrangeCore(Rect finalRect);   // position children
protected virtual void DrawCore(DrawContext ctx);      // render content

// Called by the layout system (do not override)
void Measure(Size available);     // sets DesiredSize
void Arrange(Rect finalRect);     // sets LayoutBounds
void Draw(DrawContext ctx);       // clips, draws background, calls DrawCore, draws children

// Hit-testing
Widget? HitTest(float x, float y);   // deepest visible+enabled widget at point

// Invalidation
void Invalidate();         // visual-only dirty, bubbles to root
void InvalidateLayout();   // layout+visual dirty, bubbles to root

// Child management (protected)
void AddChild(Widget child);
void RemoveChild(Widget child);

// Layout properties
float  Width, Height;           // NaN = auto (size to content)
float  MinWidth, MaxWidth, MinHeight, MaxHeight;
Thickness Margin, Padding;
HAlign HAlign;   // Left | Center | Right | Stretch
VAlign VAlign;   // Top  | Center | Bottom | Stretch

// Visual properties
SKColor Background;
float   CornerRadius;
float   Opacity;       // 0..1
bool    IsVisible;

// Interaction properties (read-only externally, set by Application)
bool IsEnabled, IsHovered, IsPressed, IsHitTestVisible;

// Layout state (set by layout system)
Size DesiredSize;      // set during Measure
Rect LayoutBounds;     // set during Arrange

// Events
Action<PointerEvent>? OnClick, OnPointerDown, OnPointerUp, OnPointerMove, OnPointerEnter, OnPointerExit;
Action<KeyEvent>?     OnKeyDown, OnKeyUp;

// Enums
enum HAlign { Left, Center, Right, Stretch }
enum VAlign { Top, Center, Bottom, Stretch }
```

---

### `Bindable<T>` · `lib/Core/Bindable.cs`

Observable value holder for two-way binding. Cycle-safe: equality guard + re-entrancy
suppression prevent infinite update loops. Add `Bind(Bindable<T>)` to any widget to
participate in the binding graph.

```csharp
Bindable<T>(T initial = default!)
T Value { get; set; }          // set fires Changed; same-value sets are no-ops
event Action<T>? Changed
```

**Pattern — wiring two widgets together:**
```csharp
var volume = new Bindable<float>(0.5f);
new Slider().WithMin(0f).WithMax(1f).Bind(volume);
new TextInput { Width = 72f }.BindFloat(volume, "F2");
```

---

### `Application` · `lib/Core/Application.cs`

```csharp
Application(string title = "ApexUI App", int width = 900, int height = 600)
void Run(Widget root)       // starts the Silk.NET event loop
Theme Theme    { get; set; }   // swap at any time to re-skin
float DpiScale { get; }        // physical pixel density (OS-driven, currently 1f)
float UiScale  { get; set; }   // user zoom level, clamped 0.1–10; default 1f
```

**UiScale** applies a `canvas.Scale(UiScale, UiScale)` transform each frame and shrinks the
logical available size by the same factor, so the entire widget tree scales uniformly without
any widget needing to know about it. Pointer coordinates are divided by `DpiScale × UiScale`
to stay in sync. Changing `UiScale` automatically triggers a re-layout.

**Wiring a slider to UiScale** (Program.cs pattern):
```csharp
var app     = new Application("My App", 900, 700);
var example = new ScaleExample();
example.Scale.Changed += v => app.UiScale = v;
app.Run(example);
```

---

### `DrawContext` · `lib/Core/DrawContext.cs`

Passed into every `DrawCore()` call.

```csharp
SKCanvas Canvas   { get; }
Theme    Theme    { get; }
float    DpiScale { get; }

SKPaint MakePaint(SKColor color, bool antialias = true)
SKFont  MakeTextFont(float sizePx, bool bold = false)
SKPaint MakeTextPaint(SKColor color)

// Input event records (also defined in this file)
record struct PointerEvent(float X, float Y, PointerButton Button, bool IsDown)
record struct KeyEvent(string Key, bool IsDown, bool Ctrl, bool Shift, bool Alt)
enum PointerButton { None, Left, Right, Middle }
```

---

### `Rect` · `lib/Core/Rect.cs`

```csharp
readonly record struct Rect(float X, float Y, float Width, float Height)
static Rect Rect.Zero

float Right, Bottom, CenterX, CenterY   // computed

bool Contains(float px, float py)
bool Intersects(Rect other)
Rect Deflate(Thickness t)
Rect Translate(float dx, float dy)
Rect WithSize(float w, float h)
SKRect ToSKRect()
```

---

### `Size` · `lib/Core/Size.cs`

```csharp
readonly record struct Size(float Width, float Height)
static Size Size.Zero, Size.Infinite

Size Constrain(Size constraint)   // clamps to min of both dimensions
```

---

### `Thickness` · `lib/Core/Thickness.cs`

```csharp
readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
Thickness(float uniform)
Thickness(float horizontal, float vertical)
static Thickness Thickness.Zero

float Horizontal   // Left + Right
float Vertical     // Top + Bottom
```

---

### `Theme` · `lib/Core/Theme.cs`

```csharp
static Theme Theme.Light, Theme.Dark   // pre-built instances

// Colors (all SKColor, init-only)
Background, Surface, SurfaceHover, SurfacePressed
OnSurface, OnSurfaceMuted
Primary, PrimaryHover, OnPrimary
Success, Warning, Danger
Border

// Typography
string FontFamily      // default "Segoe UI"
float  FontSizeBase, FontSizeSmall, FontSizeLarge, FontSizeTitle

// Shape
float CornerRadiusSm, CornerRadiusMd, CornerRadiusLg

// Spacing
float SpacingXs, SpacingSm, SpacingMd, SpacingLg
```

---

### `Label` · `lib/Widgets/Label.cs`

```csharp
Label()
string   Text      { get; set; }
float    FontSize  { get; set; }   // NaN = Theme.FontSizeBase
bool     Bold      { get; set; }
SKColor? Color     { get; set; }   // null = Theme.OnSurface

// Fluent
Label WithText(string text)
Label WithSize(float size)
Label WithColor(SKColor color)
Label AsBold()
```

---

### `Button` · `lib/Widgets/Button.cs`

```csharp
Button(string text = "", Action? onPressed = null)
string        Text    { get; set; }
ButtonVariant Variant { get; set; }   // Primary | Secondary | Ghost
Action?       OnPressed

// Fluent
Button WithText(string text)
Button WithVariant(ButtonVariant v)
Button OnPress(Action action)

enum ButtonVariant { Primary, Secondary, Ghost }
```

---

### `Slider` · `lib/Widgets/Slider.cs`

```csharp
Slider()
float Min    { get; set; }   // default 0
float Max    { get; set; }   // default 1
float Step   { get; set; }   // 0 = continuous; >0 snaps to that interval
float Value  { get; set; }   // clamped to Min..Max, snapped to Step; fires OnChanged

Action<float>? OnChanged

// Fluent
Slider WithMin(float min)
Slider WithMax(float max)
Slider WithStep(float step)
Slider WithValue(float value)
Slider OnChange(Action<float> a)

// Binding
Slider Bind(Bindable<float> source)     // float two-way binding
Slider BindInt(Bindable<int> source)    // int two-way binding (rounds on change)
```

**Int slider:** set `Step = 1` and use `BindInt(Bindable<int>)`.
**Note:** `Application` routes `OnPointerMove` to the pressed widget (pointer capture),
so dragging outside the slider bounds continues to update the value.

---

### `TextInput` · `lib/Widgets/TextInput.cs`

```csharp
TextInput(string placeholder = "")
string Value       { get; set; }   // setting fires OnChanged
string Placeholder { get; set; }
bool   IsFocused   { get; }        // set by Application

// Validation state
InputMode InputMode  { get; set; }          // Any | Integer | Float  (char-level filter)
bool      IsPassword { get; set; }          // masks display as '•'; Value holds real text
string?   AllowedChars { get; set; }        // whitelist of accepted chars (after InputMode)
string?   BlockedChars { get; set; }        // blacklist of rejected chars (after InputMode)
Func<string, bool>? Validate { get; set; } // full-string predicate → red border when false
bool IsValid { get; }                       // = Validate is null || Validate(Value)

Action<string>? OnChanged   // fires on every keystroke
Action<string>? OnSubmit    // fires on Enter

// Fluent
TextInput WithPlaceholder(string ph)
TextInput WithValue(string v)
TextInput OnChange(Action<string> a)
TextInput AsPassword()                          // IsPassword = true
TextInput AsInteger()                           // InputMode = Integer
TextInput AsFloat()                             // InputMode = Float
TextInput WithAllowedChars(string chars)
TextInput WithBlockedChars(string chars)
TextInput WithValidation(Func<string, bool> fn) // fn(Value) → false shows red border

void Tick(float deltaSeconds)   // called by Application each frame (cursor blink)

// Binding — uses InvariantCulture; source.Changed skipped while IsFocused (no typing interruption)
TextInput BindFloat(Bindable<float> source, string format = "F2")
TextInput BindInt(Bindable<int> source)

enum InputMode { Any, Integer, Float }
```

**Integer mode:** digits + optional leading `-`. **Float mode:** digits + optional leading `-` + one `.`.
`AllowedChars`/`BlockedChars` are applied *after* `InputMode`, so they can further restrict an `AsFloat()` field.
`Validate` only controls the border color — it never blocks typing. Pair with `AsFloat()` for both filtering and feedback.

**Binding note:** `source.Changed` → TextInput update is suppressed while `IsFocused` is true, so dragging the bound slider does not interrupt the user mid-type. After every external value write (initial bind + slider update), `_cursorPos` is set to `Value.Length` so the caret is always at the end when the user next focuses the field.

---

### `Toggle` · `lib/Widgets/Toggle.cs`

```csharp
Toggle(bool isChecked = false, string label = "", Action<bool>? onChanged = null)
bool   IsChecked { get; set; }
string Label     { get; set; }
Action<bool>? OnChanged

// Fluent
Toggle WithLabel(string label)
Toggle WithChecked(bool value)
Toggle OnChange(Action<bool> action)
```

---

### `Image` · `lib/Widgets/Image.cs`

```csharp
// Static factories (constructor is private)
static Image FromFile(string path)               // auto-detects raster vs .svg by extension
static Image FromBitmap(SKBitmap bitmap)
static Image FromSvgString(string svgXml)

ImageStretch Stretch { get; set; }   // None | Fill | Uniform | UniformToFill

// Fluent
Image WithStretch(ImageStretch s)
Image WithSize(float w, float h)

// Implements IDisposable — call Dispose() when removing from tree
enum ImageStretch { None, Fill, Uniform, UniformToFill }
```

---

### `Stack` · `lib/Layout/Stack.cs`

```csharp
Stack(params Widget[] children)
// All children occupy the same bounds; drawn in order (last child = on top)
```

---

### `Column` · `lib/Layout/Column.cs`

```csharp
Column(params Widget[] children)
float Spacing { get; set; }   // gap between children

Column WithSpacing(float spacing)
```

---

### `Row` · `lib/Layout/Row.cs`

```csharp
Row(params Widget[] children)
float Spacing { get; set; }   // gap between children

Row WithSpacing(float spacing)
```

---

### `PaddingBox` · `lib/Layout/PaddingBox.cs`

```csharp
PaddingBox(Widget child, Thickness padding)
PaddingBox(Widget child, float all)   // uniform padding shorthand
// Wraps exactly one child; applies Padding on all sides
```

---

### `SKColorExtensions` · `lib/Extensions/SKColorExtensions.cs`

C# 14 extension members on `SKColor`.

```csharp
static SKColor SKColor.FromHex(string hex)   // "#RRGGBB" or "#RRGGBBAA"
bool    color.IsTransparent                  // Alpha == 0
SKColor color.WithAlpha(float alpha)         // float 0..1
SKColor color.Lighten(float ratio)           // 0..1
SKColor color.Darken(float ratio)            // 0..1
```

---

### `SKCanvasExtensions` · `lib/Extensions/SKCanvasExtensions.cs`

C# 14 extension members on `SKCanvas`.

```csharp
void canvas.FillRoundRect(SKRect rect, float rx, SKColor color)
void canvas.StrokeRoundRect(SKRect rect, float rx, SKColor color, float strokeWidth = 1f)
void canvas.DrawTextCentered(string text, SKRect bounds, SKFont font, SKPaint paint)
```
