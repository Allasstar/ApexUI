# ApexUI — Code Organization Rules

## Folder layout

```
ApexUI/
├── ApexUI.csproj             ← imports lib/ApexUI.props; adds src/**/*.cs
├── Program.cs                 ← app entrypoint (no using ApexUI.* needed)
│
├── res/                       ← runtime assets; every file copied to output preserving subfolders
│   ├── ApexIcon.svg           ← window icon source (SVG, loaded at runtime via SetIcon)
│   └── AppIcon.ico            ← exe file icon (embedded at compile time via <ApplicationIcon>)
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
│   │   ├── Overlay.cs         ← Overlay: floating z-layer above all other widgets
│   │   ├── FrameworkInfo.cs   ← ApexUI.Version constant
│   │   └── GlobalUsings.cs    ← global using for all ApexUI namespaces + SkiaSharp
│   ├── Layout/
│   │   ├── Stack.cs           ← Stack panel
│   │   ├── Column.cs          ← Column panel (Spacer-aware)
│   │   ├── Row.cs             ← Row panel (Spacer-aware)
│   │   ├── PaddingBox.cs      ← PaddingBox panel
│   │   ├── Spacer.cs          ← Flexible gap; fills leftover space in Row/Column
│   │   ├── Grid.cs            ← 2D grid with Auto/Fixed/Star column+row sizing
│   │   └── Wrap.cs            ← Wrapping row; children reflow to next line on overflow
│   ├── Widgets/
│   │   ├── Label.cs           ← Label widget
│   │   ├── Button.cs          ← Button widget
│   │   ├── TextInput.cs       ← TextInput widget (+ BindFloat/BindInt)
│   │   ├── Toggle.cs          ← Toggle widget
│   │   ├── Image.cs           ← Image widget (raster + SVG)
│   │   ├── Slider.cs          ← Slider widget (float/int, optional step)
│   │   ├── ProgressBar.cs     ← ProgressBar widget (float 0..1, four variants)
│   │   ├── Separator.cs       ← Separator widget (1 px horizontal or vertical line)
│   │   ├── Scroll.cs          ← Scroll widget (vertical/horizontal/both, optional scrollbar)
│   │   ├── Tabs.cs            ← Tabs widget (Top/Bottom/Left/Right, scrollable tab bar)
│   │   ├── RadioGroup.cs      ← RadioGroup<T>: mutually-exclusive options + Bindable<T>
│   │   └── NumberInput.cs     ← NumberInput: TextInput + −/+ buttons, Bindable<float/int>
│   └── Extensions/
│       ├── SKColorExtensions.cs   ← C# 14 extension members on SKColor
│       └── SKCanvasExtensions.cs  ← C# 14 extension members on SKCanvas
│
└── src/                       ← app-specific code (untouched by framework updates)
    ├── Examples/              ← self-contained example widgets (one class per example)
    │   ├── CounterExample.cs      ← counter + text-input demo
    │   ├── ImageToggleExample.cs  ← image + toggle demo
    │   ├── SliderExample.cs       ← sliders with two-way binding to text inputs
    │   ├── ScaleExample.cs        ← UI scale demo; exposes Bindable<float> Scale
    │   ├── PrimitivesExample.cs   ← ProgressBar, Separator, and Overlay demo
    │   ├── LayoutExample.cs       ← Spacer, Grid, Wrap, and AspectRatio demo
    │   ├── InputsExample.cs       ← RadioGroup<T> and NumberInput demo
    │   └── TabsExample.cs         ← all examples as tabs; exposes Scale + DarkMode bindables
    ├── Screens/               ← full-screen views
    ├── Widgets/               ← app-specific widgets (not framework reusable)
    └── Models/                ← app data models
```

## Planned widgets (TODO)

Widgets not yet implemented, in priority order.

### Composite widgets (depend on Overlay)
| Widget | File | Notes |
|--------|------|-------|
| `Dropdown` / `ComboBox` | `lib/Widgets/Dropdown.cs` | Pick one item from a list; opens an Overlay with a ListView |
| `Tooltip` | `lib/Widgets/Tooltip.cs` | Overlay shown on hover after a short delay |
| `Dialog` / `Modal` | `lib/Widgets/Dialog.cs` | Blocking Overlay with a content widget and an optional backdrop |
| `ContextMenu` | `lib/Widgets/ContextMenu.cs` | Right-click Overlay with a list of actions |

---

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

var example = new TabsExample();
new Application("ApexUI Demo", 900, 700)
    .BindUiScale(example.Scale)
    .BindDarkMode(example.DarkMode)
    .Run(example);
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

| Profile | Command | Publish output | Use when |
|---|---|---|---|
| `Full` (default) | `dotnet publish` | `bin/Publish/Full/` — exe + all DLLs | distribution, easy debugging |
| `SingleFile` | `dotnet publish -p:BuildProfile=SingleFile` | `bin/Publish/SingleFile/` — one `.exe` | clean single-file distribution |
| `MinSize` | `dotnet publish -p:BuildProfile=MinSize` | `bin/Publish/MinSize/` — one trimmed + compressed `.exe` | smallest possible distributable |

All publish outputs land in `bin/Publish/<Profile>/`, separate from `bin/Debug/` and `bin/Release/` build artifacts.

**Development**: use `dotnet run` / `dotnet build` — framework-dependent, fast, no RID suffix.

**Runtime**: always `$(NETCoreSdkRuntimeIdentifier)` — auto-detects the build machine's platform (`win-x64`, `linux-x64`, `osx-arm64`, …). No manual change needed per OS.

**Self-contained**: always `true` for publish — the .NET runtime is bundled so end-users need nothing installed.

**SingleFile note**: native libs (glfw3, SkiaSharp) are embedded and extracted to a temp folder on first launch (`IncludeNativeLibrariesForSelfExtract=true`).

**MinSize note**: uses `TrimMode=link` (aggressive). SkiaSharp relies on reflection — add `TrimmerRootDescriptor` entries if you hit `MissingMethodException` after trimming.

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
float  AspectRatio;             // NaN = no constraint; enforced in both Measure and Arrange
AspectRatioMode AspectRatioMode; // WidthControlsHeight (default) | HeightControlsWidth
Thickness Margin, Padding;
HAlign HAlign;   // Left | Center | Right | Stretch
VAlign VAlign;   // Top  | Center | Bottom | Stretch

// Visual properties
SKColor Background;
Func<Theme, SKColor>? BackgroundSource;   // overrides Background at draw time; use for theme-aware colors
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
Action<float, float>? OnScroll;   // (deltaX, deltaY); Application bubbles up the tree to first handler

// Enums
enum HAlign          { Left, Center, Right, Stretch }
enum VAlign          { Top, Center, Bottom, Stretch }
enum AspectRatioMode { WidthControlsHeight, HeightControlsWidth }
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

// Icon — call before Run(); path is relative to AppContext.BaseDirectory if not rooted
// .svg  → rendered at 16/32/48 px and passed as three RawImage sizes to Silk.NET
// .ico  → all frames extracted via SKCodec (each size in the file passed separately)
// other → single raster frame (PNG, JPG, …)
Application SetIcon(string path)

// Exe file icon (shown in Explorer / taskbar pinning) is separate — set in ApexUI.csproj:
//   <ApplicationIcon>res\AppIcon.ico</ApplicationIcon>
// Must be a .ico file; drop it in res/ and the build picks it up automatically.

// Fluent binding — syncs initial value then subscribes; returns this for chaining
Application BindUiScale(Bindable<float> source)   // source.Value → UiScale; changes forwarded
Application BindDarkMode(Bindable<bool> isDark)   // true → Theme.Dark, false → Theme.Light
```

**UiScale** applies a `canvas.Scale(UiScale, UiScale)` transform each frame and shrinks the
logical available size by the same factor, so the entire widget tree scales uniformly without
any widget needing to know about it. Pointer coordinates are divided by `DpiScale × UiScale`
to stay in sync. Changing `UiScale` automatically triggers a re-layout.

**Program.cs pattern:**
```csharp
var example = new TabsExample();
new Application("My App", 900, 700)
    .BindUiScale(example.Scale)
    .BindDarkMode(example.DarkMode)
    .Run(example);
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

### `ProgressBar` · `lib/Widgets/ProgressBar.cs`

Read-only filled bar driven by a `float` value in `0..1`. Four color variants.

```csharp
ProgressBar()
float               Value   { get; set; }   // clamped 0..1
ProgressBarVariant  Variant { get; set; }   // Primary | Success | Warning | Danger

// Fluent
ProgressBar WithValue(float value)
ProgressBar WithVariant(ProgressBarVariant v)

// One-way binding (source → bar only)
ProgressBar Bind(Bindable<float> source)

enum ProgressBarVariant { Primary, Success, Warning, Danger }
```

---

### `Separator` · `lib/Widgets/Separator.cs`

1 px horizontal or vertical divider line. Non-interactive (`IsHitTestVisible = false`).

```csharp
Separator()
SeparatorOrientation Orientation { get; set; }   // Horizontal (default) | Vertical
SKColor?             Color       { get; set; }   // null = Theme.Border

// Fluent
Separator AsHorizontal()
Separator AsVertical()
Separator WithColor(SKColor color)

enum SeparatorOrientation { Horizontal, Vertical }
```

**Sizing:** Horizontal reports `(available.Width, 1f)`; Vertical reports `(1f, available.Height)`.
For a vertical separator in a Row, set an explicit `Height` to prevent unbounded measurement:
```csharp
new Separator { Height = 24f }.AsVertical()
```

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
Toggle Bind(Bindable<bool> source)    // two-way binding
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

`Spacer` children are detected at arrange time; each gets an equal share of the height
remaining after all non-Spacer children and gaps are measured.

---

### `Row` · `lib/Layout/Row.cs`

```csharp
Row(params Widget[] children)
float Spacing { get; set; }   // gap between children

Row WithSpacing(float spacing)
```

`Spacer` children are detected at arrange time; each gets an equal share of the width
remaining after all non-Spacer children and gaps are measured.

---

### `Spacer` · `lib/Layout/Spacer.cs`

Flexible gap for Row and Column. Reports zero size during Measure (so the container's
natural size is unaffected), then receives an equal share of the leftover space during
Arrange. Multiple Spacers in the same container split the remainder equally.

```csharp
Spacer()
// No properties — place it between siblings in Row or Column.
```

**Example — push button to right edge:**
```csharp
new Row(new Label { Text = "File: foo.txt" }, new Spacer(), new Button("Close")) { Spacing = 8f }
```

---

### `Grid` · `lib/Layout/Grid.cs`

2D layout panel. Children are placed by explicit (col, row) coordinate. Columns and rows
support `Auto` (size to content), `Fixed` (px), and `Star` (fill remaining proportionally)
sizing. Rows auto-detected from cell coordinates when `DefineRows` is not called.

```csharp
Grid()
Grid DefineColumns(params GridLength[] columns)   // call before Add(); defaults to [Auto]
Grid DefineRows(params GridLength[] rows)         // optional; auto-detected when omitted
Grid WithColumns(int count)                       // shorthand: N equal Star(*) columns
Grid WithRows(int count)                          // shorthand: N equal Star(*) rows
Grid WithSpacing(float columnSpacing, float rowSpacing)
Grid Add(Widget child, int col = 0, int row = 0, int colSpan = 1, int rowSpan = 1)

float ColumnSpacing { get; set; }
float RowSpacing    { get; set; }
```

```csharp
// GridLength factories
GridLength.Auto                  // size to max of cells in that track
GridLength.Fixed(float px)       // fixed pixel width/height
GridLength.Star(float factor=1f) // proportion of remaining space (like CSS fr)
```

**Form layout example:**
```csharp
new Grid()
    .DefineColumns(GridLength.Auto, GridLength.Star())
    .WithSpacing(12f, 8f)
    .Add(new Label { Text = "Name",  VAlign = VAlign.Center }, 0, 0)
    .Add(new TextInput("Enter name…"), 1, 0)
    .Add(new Label { Text = "Email", VAlign = VAlign.Center }, 0, 1)
    .Add(new TextInput("Enter email…"), 1, 1)
```

**Span example (header spanning both columns):**
```csharp
.Add(new Label { Text = "Contact form", Bold = true }, 0, 0, colSpan: 2)
```

---

### `Wrap` · `lib/Layout/Wrap.cs`

Children arranged left-to-right, wrapping to a new line when they would overflow the
available width. All items on the same line share the tallest item's height.

```csharp
Wrap(params Widget[] children)
float HorizontalSpacing { get; set; }   // gap between items on the same line
float VerticalSpacing   { get; set; }   // gap between lines

Wrap WithSpacing(float horizontal, float vertical)
```

**Tag cloud example:**
```csharp
new Wrap(tags.Select(t => new Label { Text = t }).ToArray()).WithSpacing(6f, 6f)
```

---

### `PaddingBox` · `lib/Layout/PaddingBox.cs`

```csharp
PaddingBox(Widget child, Thickness padding)
PaddingBox(Widget child, float all)   // uniform padding shorthand
// Wraps exactly one child; applies Padding on all sides
```

---

### `Overlay` · `lib/Core/Overlay.cs`

Floating z-layer rendered by `Application` after the root widget tree, so it appears above all other content regardless of nesting depth. Occupies the full logical screen for backdrop hit-testing.

```csharp
Overlay()
Widget?        Content               { get; set; }   // the floating panel
Widget?        Anchor                { get; set; }   // anchor for edge-relative positioning
OverlayAnchor  AnchorEdge            { get; set; }   // BelowAnchor (default) | Above | Right | Left
float          PositionX, PositionY  { get; set; }   // explicit screen coords (Anchor = null, Left/Top)
HAlign         ContentHAlign         { get; set; }   // screen alignment when Anchor is null
VAlign         ContentVAlign         { get; set; }
bool           IsModal               { get; set; }   // semi-transparent backdrop behind Content
bool           DismissOnClickOutside { get; set; }   // default true; Close() on backdrop click
Action?        OnDismiss             { get; set; }

void Open()                                          // register with Application + show
void Open(Widget anchor, OverlayAnchor edge = BelowAnchor)
void Close()                                         // unregister + hide + fire OnDismiss

enum OverlayAnchor { BelowAnchor, AboveAnchor, RightOfAnchor, LeftOfAnchor }
```

**Architecture:** `Application` maintains an internal `_overlays` list. Overlays are measured/arranged
against the full logical screen each frame, then drawn after the root tree — no clip stack inherited
from the regular widget hierarchy. `HitTestAll` checks overlays (last-registered first) before the root,
so all pointer input is blocked from reaching widgets beneath an open overlay.

**Centering a dialog:**
```csharp
var ov = new Overlay { IsModal = true, DismissOnClickOutside = false,
                        ContentHAlign = HAlign.Center, ContentVAlign = VAlign.Center };
ov.Content = new PaddingBox(content, 20f) { BackgroundSource = t => t.Surface, CornerRadius = 12f };
button.OnClick = _ => ov.Open();
```

**Anchored dropdown/popup:**
```csharp
var ov = new Overlay { DismissOnClickOutside = true };
ov.Content = popupPanel;
btn.OnClick = _ => ov.Open(btn, OverlayAnchor.BelowAnchor);
```

---

### `Scroll` · `lib/Widgets/Scroll.cs`

Scrollable single-child container. Clips content to viewport. Scrollbar thumb drawn on the right (vertical) or bottom (horizontal) edge. Mouse-wheel events bubble up from any child to the nearest ancestor `Scroll`.

```csharp
Scroll(Widget content)
bool            ShowScrollbar { get; set; }   // default true
ScrollDirection Direction     { get; set; }   // Vertical | Horizontal | Both

// Fluent
Scroll WithDirection(ScrollDirection dir)
Scroll HideScrollbar()

enum ScrollDirection { Vertical, Horizontal, Both }
```

**Scrollbar geometry:** for `Vertical`, the right `8px` strip is reserved for the scrollbar (content is `Width − 8`). For `Horizontal`, the bottom `8px` strip is reserved. The scrollbar is drawn in `DrawCore`; only the Scroll widget occupies the strip (no child can be clicked there).

**Mouse wheel:** the `OnScroll` event bubbles up the widget tree from the hovered widget. The first ancestor with `OnScroll` set handles the event — `Scroll` sets it in its constructor. For `Horizontal`-only direction, both wheel axes scroll horizontally (so a vertical-wheel mouse still scrolls).

**Content sizing:** content is measured with `(Width, ∞)` for vertical, `(∞, Height)` for horizontal. The base `Widget.MeasureCore` now reports the max of its children's sizes (Stack semantics), so app-level widgets that don't override `MeasureCore` still report their natural size to the Scroll.

---

### `Tabs` · `lib/Widgets/Tabs.cs`

Tabbed container with a scrollable tab bar and a swappable content panel.

```csharp
Tabs(TabPosition position = TabPosition.Top)
Tabs AddTab(string title, Widget content)   // fluent; content shown/hidden by visibility toggle
int  SelectedIndex { get; set; }
Action<int>? OnTabChanged

enum TabPosition { Top, Bottom, Left, Right }
```

**Tab bar:** internally a `Scroll` widget (Direction=Horizontal for Top/Bottom, Vertical for Left/Right, `ShowScrollbar=false`). Mouse-wheel scrolls the bar when hovering over it. Tab bar height/width is fixed at `40px`.

**Content:** all tab content widgets are children of `Tabs`. Only the active tab's `IsVisible = true`. Switching tabs calls `InvalidateLayout()` so the newly-visible content is arranged before the next draw.

**Active indicator:** a `2px` Primary-colored line on the content-facing edge of the active tab button.

---

### `RadioGroup<T>` · `lib/Widgets/RadioGroup.cs`

Mutually-exclusive list of radio buttons backed by a `Bindable<T>`.

```csharp
RadioGroup<T>(RadioOrientation orientation = Vertical, float spacing = 8f)
RadioGroup<T> AddOption(T value, string label)   // fluent; call before Bind
RadioGroup<T> WithSelected(T value)
RadioGroup<T> OnChange(Action<T?> action)
RadioGroup<T> Bind(Bindable<T> source)           // two-way binding

T?            SelectedValue { get; set; }        // set fires OnChanged + updates visuals
Action<T?>?   OnChanged

enum RadioOrientation { Vertical, Horizontal }
```

**Radio item visuals:** 18 px outer ring (Primary when selected, Border otherwise) + 10 px filled dot when selected. Label drawn to the right. Hover ripple shown on mouse-over.

**Example:**
```csharp
var theme = new Bindable<string>("System");
new RadioGroup<string>()
    .AddOption("Light",  "Light")
    .AddOption("Dark",   "Dark")
    .AddOption("System", "System")
    .Bind(theme)
```

---

### `NumberInput` · `lib/Widgets/NumberInput.cs`

Numeric text field flanked by decrement (`−`) and increment (`+`) buttons.
Layout: `[−] [TextInput] [+]` — TextInput fills remaining width.

```csharp
NumberInput(float value = 0f)
float  Min    { get; set; }   // default -∞
float  Max    { get; set; }   // default +∞
float  Step   { get; set; }   // default 1
string Format { get; set; }   // default "G"; set to "F2" or "0" as needed
float  Value  { get; set; }   // clamped to Min..Max; fires OnChanged

Action<float>? OnChanged

// Fluent
NumberInput WithMin(float min)
NumberInput WithMax(float max)
NumberInput WithStep(float step)
NumberInput WithFormat(string format)
NumberInput WithValue(float v)
NumberInput OnChange(Action<float> a)

// Binding
NumberInput Bind(Bindable<float> source)
NumberInput BindInt(Bindable<int> source)   // also sets Step=1 and Format="0"
```

**Typing behaviour:** while the user edits the field the display may show out-of-range text; pressing `+`/`−` or `Enter` syncs the display back to the clamped value.

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
