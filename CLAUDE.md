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
│   │   └── TextInput.cs       ← TextInput widget
│   └── Extensions/
│       ├── SKColorExtensions.cs   ← C# 14 extension members on SKColor
│       └── SKCanvasExtensions.cs  ← C# 14 extension members on SKCanvas
│
└── src/                       ← app-specific code (untouched by framework updates)
    ├── Examples/              ← self-contained example widgets (one class per example)
    │   └── CounterExample.cs  ← counter + text-input demo
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
