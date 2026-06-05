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
7. **Update CLAUDE.md** whenever you add/rename/delete a `.cs` file: update API Reference.

---

## Build profiles

| Profile | Command | Output |
|---|---|---|
| `Full` (default) | `dotnet publish` | `bin/Publish/Full/` |
| `SingleFile` | `dotnet publish -p:BuildProfile=SingleFile` | `bin/Publish/SingleFile/` |
| `MinSize` | `dotnet publish -p:BuildProfile=MinSize` | `bin/Publish/MinSize/` (trimmed) |

Development: `dotnet run` / `dotnet build`. Self-contained publish always true.

---

## API Reference

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

```csharp
var example = new TabsExample();
new Application("My App", 900, 700)
    .BindUiScale(example.Scale)
    .BindDarkMode(example.DarkMode)
    .Run(example);
```

---

### `DrawContext` · `lib/Core/DrawContext.cs`

```csharp
SKCanvas Canvas  { get; }
Theme    Theme   { get; }
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

### `Label` · `lib/Widgets/Label.cs`

```csharp
Label()
string   Text     { get; set; }
float    FontSize { get; set; }   // NaN = Theme.FontSizeBase
bool     Bold     { get; set; }
SKColor? Color    { get; set; }   // null = Theme.OnSurface

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
ButtonVariant Variant { get; set; }
Action?       OnPressed

Button WithText(string text)
Button WithVariant(ButtonVariant v)
Button OnPress(Action action)

enum ButtonVariant { Primary, Secondary, Ghost }
```

---

### `Slider` · `lib/Widgets/Slider.cs`

```csharp
Slider()
float Min, Max, Step, Value
Action<float>? OnChanged

Slider WithMin(float min)
Slider WithMax(float max)
Slider WithStep(float step)
Slider WithValue(float value)
Slider OnChange(Action<float> a)
Slider Bind(Bindable<float> source)
Slider BindInt(Bindable<int> source)
```

---

### `TextInput` · `lib/Widgets/TextInput.cs`

```csharp
TextInput(string placeholder = "")
string Value, Placeholder
bool   IsFocused, IsPassword, IsValid
InputMode InputMode        // Any | Integer | Float
string?   AllowedChars, BlockedChars
Func<string, bool>? Validate   // false → red border (never blocks typing)

Action<string>? OnChanged, OnSubmit

TextInput WithPlaceholder(string ph)
TextInput WithValue(string v)
TextInput OnChange(Action<string> a)
TextInput AsPassword()
TextInput AsInteger()
TextInput AsFloat()
TextInput WithAllowedChars(string chars)
TextInput WithBlockedChars(string chars)
TextInput WithValidation(Func<string, bool> fn)
TextInput BindFloat(Bindable<float> source, string format = "F2")
TextInput BindInt(Bindable<int> source)

enum InputMode { Any, Integer, Float }
```

Binding: `source.Changed` is suppressed while `IsFocused` to avoid interrupting typing.

---

### `ProgressBar` · `lib/Widgets/ProgressBar.cs`

```csharp
ProgressBar()
float              Value   { get; set; }   // 0..1
ProgressBarVariant Variant { get; set; }

ProgressBar WithValue(float value)
ProgressBar WithVariant(ProgressBarVariant v)
ProgressBar Bind(Bindable<float> source)   // one-way

enum ProgressBarVariant { Primary, Success, Warning, Danger }
```

---

### `Separator` · `lib/Widgets/Separator.cs`

```csharp
Separator()
SeparatorOrientation Orientation { get; set; }   // Horizontal (default) | Vertical
SKColor?             Color       { get; set; }

Separator AsHorizontal()
Separator AsVertical()
Separator WithColor(SKColor color)
```

For a vertical separator in a Row: `new Separator { Height = 24f }.AsVertical()`

---

### `Toggle` · `lib/Widgets/Toggle.cs`

```csharp
Toggle(bool isChecked = false, string label = "", Action<bool>? onChanged = null)
bool   IsChecked { get; set; }
string Label     { get; set; }
Action<bool>? OnChanged

Toggle WithLabel(string label)
Toggle WithChecked(bool value)
Toggle OnChange(Action<bool> action)
Toggle Bind(Bindable<bool> source)
```

---

### `Image` · `lib/Widgets/Image.cs`

```csharp
static Image FromFile(string path)
static Image FromBitmap(SKBitmap bitmap)
static Image FromSvgString(string svgXml)

ImageStretch Stretch { get; set; }
Image WithStretch(ImageStretch s)
Image WithSize(float w, float h)
// Implements IDisposable

enum ImageStretch { None, Fill, Uniform, UniformToFill }
```

---

### `Stack` · `lib/Layout/Stack.cs`

```csharp
Stack(params Widget[] children)
// All children occupy same bounds; last child drawn on top
```

---

### `Column` · `lib/Layout/Column.cs`

```csharp
Column(params Widget[] children)
float Spacing { get; set; }
Column WithSpacing(float spacing)
Column Add(Widget child)   // dynamic; returns this
```

---

### `Row` · `lib/Layout/Row.cs`

```csharp
Row(params Widget[] children)
float Spacing { get; set; }
Row WithSpacing(float spacing)
Row Add(Widget child)   // dynamic; returns this
```

`Spacer` children get equal share of leftover space in both Row and Column.

---

### `Spacer` · `lib/Layout/Spacer.cs`

```csharp
Spacer()
// No properties. Example:
new Row(new Label { Text = "File: foo.txt" }, new Spacer(), new Button("Close"))
```

---

### `Grid` · `lib/Layout/Grid.cs`

```csharp
Grid()
Grid DefineColumns(params GridLength[] columns)
Grid DefineRows(params GridLength[] rows)      // optional; auto-detected from cell coords
Grid WithColumns(int count)                    // N equal Star columns
Grid WithRows(int count)
Grid WithSpacing(float columnSpacing, float rowSpacing)
Grid Add(Widget child, int col = 0, int row = 0, int colSpan = 1, int rowSpan = 1)

GridLength.Auto
GridLength.Fixed(float px)
GridLength.Star(float factor = 1f)
```

```csharp
new Grid()
    .DefineColumns(GridLength.Auto, GridLength.Star())
    .WithSpacing(12f, 8f)
    .Add(new Label { Text = "Name", VAlign = VAlign.Center }, 0, 0)
    .Add(new TextInput("Enter name…"), 1, 0)
```

---

### `Wrap` · `lib/Layout/Wrap.cs`

```csharp
Wrap(params Widget[] children)
float HorizontalSpacing { get; set; }
float VerticalSpacing   { get; set; }
Wrap WithSpacing(float horizontal, float vertical)
```

---

### `PaddingBox` · `lib/Layout/PaddingBox.cs`

```csharp
PaddingBox(Widget child, Thickness padding)
PaddingBox(Widget child, float all)
```

---

### `Overlay` · `lib/Core/Overlay.cs`

```csharp
Overlay()
Widget?       Content               { get; set; }
Widget?       Anchor                { get; set; }
OverlayAnchor AnchorEdge            { get; set; }
float         PositionX, PositionY  { get; set; }
HAlign        ContentHAlign         { get; set; }
VAlign        ContentVAlign         { get; set; }
bool          IsModal               { get; set; }
bool          DismissOnClickOutside { get; set; }   // default true
Action?       OnDismiss             { get; set; }

void Open()
void Open(Widget anchor, OverlayAnchor edge = BelowAnchor)
void Close()

enum OverlayAnchor { BelowAnchor, AboveAnchor, RightOfAnchor, LeftOfAnchor }
```

```csharp
// Centered modal
var ov = new Overlay { IsModal = true, DismissOnClickOutside = false,
                        ContentHAlign = HAlign.Center, ContentVAlign = VAlign.Center };
ov.Content = new PaddingBox(content, 20f) { BackgroundSource = t => t.Surface, CornerRadius = 12f };
button.OnClick = _ => ov.Open();

// Anchored popup
var ov = new Overlay { DismissOnClickOutside = true };
ov.Content = popupPanel;
btn.OnClick = _ => ov.Open(btn, OverlayAnchor.BelowAnchor);
```

---

### `Scroll` · `lib/Widgets/Scroll.cs`

```csharp
Scroll(Widget content)
bool            ShowScrollbar { get; set; }   // default true
ScrollDirection Direction     { get; set; }   // Vertical | Horizontal | Both

Scroll WithDirection(ScrollDirection dir)
Scroll HideScrollbar()

enum ScrollDirection { Vertical, Horizontal, Both }
```

Scrollbar strip: 8px on right (Vertical) or bottom (Horizontal). Wheel events bubble to nearest Scroll ancestor.

---

### `Tabs` · `lib/Widgets/Tabs.cs`

```csharp
Tabs(TabPosition position = TabPosition.Top)
Tabs AddTab(string title, Widget content)
int  SelectedIndex { get; set; }
Action<int>? OnTabChanged

enum TabPosition { Top, Bottom, Left, Right }
```

Tab bar: 40px; internally uses `Scroll`. Only active tab has `IsVisible = true`.

---

### `RadioGroup<T>` · `lib/Widgets/RadioGroup.cs`

```csharp
RadioGroup<T>(RadioOrientation orientation = Vertical, float spacing = 8f)
RadioGroup<T> AddOption(T value, string label)
RadioGroup<T> WithSelected(T value)
RadioGroup<T> OnChange(Action<T?> action)
RadioGroup<T> Bind(Bindable<T> source)

T?          SelectedValue { get; set; }
Action<T?>? OnChanged

enum RadioOrientation { Vertical, Horizontal }
```

---

### `NumberInput` · `lib/Widgets/NumberInput.cs`

Layout: `[−] [TextInput] [+]`

```csharp
NumberInput(float value = 0f)
float  Min, Max, Step, Value
string Format { get; set; }   // default "G"
Action<float>? OnChanged

NumberInput WithMin(float min)
NumberInput WithMax(float max)
NumberInput WithStep(float step)
NumberInput WithFormat(string format)
NumberInput WithValue(float v)
NumberInput OnChange(Action<float> a)
NumberInput Bind(Bindable<float> source)
NumberInput BindInt(Bindable<int> source)   // also sets Step=1, Format="0"
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

---

### `Dropdown<T>` · `lib/Widgets/Dropdown.cs`

```csharp
Dropdown<T>()
T?      SelectedValue { get; set; }
string  Placeholder   { get; set; }
Action<T?>? OnChanged

Dropdown<T> AddItem(T value, string label)
Dropdown<T> WithPlaceholder(string text)
Dropdown<T> WithSelected(T value)
Dropdown<T> OnChange(Action<T?> action)
Dropdown<T> Bind(Bindable<T> source)
```

---

### `Tooltip` · `lib/Widgets/Tooltip.cs`

```csharp
Tooltip(Widget child, string text)
float           ShowDelay { get; set; }   // default 0.6s
TooltipPosition Position  { get; set; }  // Below (default) | Above | Right | Left

enum TooltipPosition { Below, Above, Right, Left }
```

`HitTest` delegates to child — pointer events pass through unchanged.

---

### `Dialog` · `lib/Widgets/Dialog.cs`

Not a Widget — orchestrates an `Overlay`.

```csharp
Dialog(string title, Widget content)
string  Title    { get; set; }
Action? OnClosed

Dialog AddButton(string text, Action action, ButtonVariant variant = Secondary)
Dialog AddPrimaryButton(string text, Action action)
void Show()
void Close()

static Dialog Alert(string title, string message, string btnText = "OK", Action? onClose = null)
static Dialog Confirm(string title, string message,
    Action? onConfirm = null, Action? onCancel = null,
    string confirmText = "OK", string cancelText = "Cancel")
```

```csharp
Dialog? dlg = null;
dlg = new Dialog("Rename", new TextInput("New name…"))
    .AddButton("Cancel", () => dlg!.Close())
    .AddPrimaryButton("Rename", () => { Rename(input.Value); dlg!.Close(); });
renameButton.OnClick = _ => dlg.Show();
```

---

### `ContextMenu` · `lib/Widgets/ContextMenu.cs`

Not a Widget — orchestrates an `Overlay`.

```csharp
ContextMenu()
ContextMenu AddItem(string label, Action action, bool enabled = true)
ContextMenu AddCheckItem(string label, Bindable<bool> binding, bool closeOnClick = false)
ContextMenu AddSeparator()
ContextMenu AddHeader(string text)

static void Attach(Widget target, ContextMenu menu)   // hooks right-click on target + all descendants; call after tree is built
void Show(float x, float y)
void Close()
```

```csharp
var bold = new Bindable<bool>(false);
var menu = new ContextMenu()
    .AddHeader("Format")
    .AddCheckItem("Bold", bold)
    .AddSeparator()
    .AddItem("Copy",  () => Copy())
    .AddItem("Paste", () => Paste());
ContextMenu.Attach(myWidget, menu);
```
