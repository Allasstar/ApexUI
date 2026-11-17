# ApexUI updates

Newest first. **Breaking** marks changes that need edits in apps using ApexUI.

## Unreleased

### Commands, dialog keys, file dialogs, window title
- New `CommandManager`: named `Command`s with keyboard shortcuts. `Add(name, label, execute, "Mod+Shift+Z", ...)`, `Find`, indexer, `Execute(name)`, `Executed` event. `Attach(host)` runs the first enabled command whose gesture matches a key press no focused widget handled; shortcuts wait while a modal overlay is open and, with `SuspendWhileEditingText` (default on), while the focused widget edits text. `HandleKey(e)` routes keys from elsewhere.
- New `Command`: `Name`, `Label`, `Gestures`, `Enabled` (`Bindable<bool>`) / `IsEnabled`, `Execute()` (does nothing while disabled), `Tooltip` ("Undo (Ctrl+Z)").
- New `KeyGesture(Key, KeyModifiers)`: `Parse`/`TryParse` ("Ctrl+Shift+Z", "Alt+F4", "Equal", "1"; `Mod` is Cmd on macOS and Ctrl elsewhere, `KeyGesture.PrimaryModifier`), `Matches(KeyEventArgs)`, readable `ToString()`.
- New `Button.WithCommand(command)` and `IconButton.WithCommand(command)`: pressing runs the command, and the button follows its enabled state while on screen.
- New `UiHost.HasModalOverlay` and `Widget.EditsText` (true for `TextInput`).
- `Dialog`: focuses `InitialFocus` or the primary button when shown; Enter runs the primary button and Escape runs the cancel button (or closes), also when nothing in the dialog has focus. New `AddCancelButton`, `InitialFocus`, `IsOpen`. `Confirm` marks its cancel button; `Alert` and `Confirm` wrap long messages. `Close` on a closed dialog no longer raises `Closed` again. `BuildPanel` no longer uses LINQ.
- Desktop file dialogs: `OpenFileAsync` and `SaveFileAsync` now work. Windows uses the system common dialogs on their own STA thread, owned by the window so it stays responsive and blocked for input; macOS uses AppleScript (`osascript`); Linux uses `zenity`, then `kdialog`. Save asks before replacing a file and adds the first filter's extension to a bare name. No new packages.
- `DesktopPlatform` installs a synchronization context on the UI thread, so code after an `await` (a file dialog, file I/O) continues on the UI thread between loop iterations.
- **Breaking:** `IPlatform` gains `void SetTitle(string title)`. Custom backends add it.
- Gallery: new Commands tab (shortcuts with bound buttons and an enable switch, a text field that keeps typed letters, Open and Save dialogs, window title).
- 25 new tests (300 in total).

### Dashed strokes
- New `DrawContext.StrokeDashedPath(path, color, thickness, dash, gap)`: strokes a path as dashes. Lengths are in the current canvas units, so inside a `Viewport` divide them by `Zoom` to keep them a fixed size on screen; thickness 0 draws hairline dashes. The context keeps one dash pattern and rebuilds it only when the lengths change.
- Gallery: the Viewport tab draws a dashed frame around the area Fit shows.
- 2 new tests (275 in total).

### Live window resize
- The desktop window now redraws while you drag its edge, instead of freezing until the drag ends. Windows and macOS run their own resize loop during the drag, which blocked ApexUI's loop; `DesktopPlatform` now draws from inside the size callbacks.
- New `UiHost.ResizeFrameRate` (default 60) and `BindResizeFrameRate(Bindable<float>)`: redraws per second while resizing, 0 for the old behaviour (one redraw when the resize ends). `DesktopPlatform.WithResizeFrameRate(fps)` sets it before the host exists.
- **Breaking:** `IPlatform` gains `float ResizeFrameRate { get; set; }`. Custom backends add it; a browser backend uses it to throttle resize-driven layout.
- Gallery: Settings has a Window section with a rate field and slider.
- 2 new tests (273 in total).

### M3 retro
- `Tooltip`, `Toast` and `ColorSwatch` share one internal `PopupPanel` (Surface fill, themed padding and radius) instead of three private copies.
- `Toast` lifts itself off the bottom edge in arrange, so the gap follows theme changes; before, it was set once when the toast first opened.
- 1 new test (271 in total).

### Lists, number fields, colors, toasts
- New `ListView<T>`: a `VirtualList` with one selected row (`SelectedIndex`, `SelectionChanged`), a hover highlight, Up/Down/Home/End while focused, and with `AllowReorder` drag reordering: a drop line follows the pointer and release raises `Reordered(from, to)` with the row's final index. The list never moves items itself. A press that a widget inside the row handles does not select. Its fluent setters return `ListView<T>`.
- New `EditableLabel`: text that turns into a text field on double-click or `BeginEdit()`. Enter or losing focus commits (`TextCommitted`), Escape cancels; blank or unchanged text is not committed.
- New `NumberField`: a caption and a number in the style of design tools. Dragging the caption scrubs by `Step` per pixel (Shift: ten times); typing commits on Enter or focus loss, and invalid text reverts. `ValueChanged` on every change, `EditCompleted` once per finished scrub or typed edit that changed the value (for undo steps). `Min`, `Max`, `Format`, `Bind`.
- New `ColorPicker`: saturation/brightness square, hue strip, alpha strip (`ShowAlpha`) and a hex field. Hue survives gray and black. `ColorChanged`, `EditCompleted`, and the helpers `ToHex` (`#RRGGBB`, or `#RRGGBBAA` when not opaque) and `TryParseHex`.
- New `ColorSwatch`: a color square (checkerboard under transparent colors) that opens a `ColorPicker` popup below itself on click or Space/Enter. `Color`, `ColorChanged`, `EditCompleted`, `ShowAlpha`, `IsReadOnly`.
- New `Toast`: an `Overlay` that shows a message at the bottom center and hides after `Duration`; a new message replaces the current one. It never takes pointer input.
- New `LinearPanel.SpacingSource` (e.g. `t => t.SpacingSm`): spacing from the theme that follows theme changes, until `Spacing` is set.
- New `Widget.DrawOverChildren` hook, drawn after the children and before the focus ring.
- New `DrawContext.FillCheckerboard`.
- `VirtualList<T>`: `Count`, `Refresh()` (rebuild rows after changing a plain list), `ScrollIntoView(index)`, and for subclasses `ScrollOffset`, `RowWidth`, `RowRect(index)`, `RowIndexAt(y)`. A change notification from its `ObservableList` now keeps the scroll position (clamped to the new length); before, every change scrolled back to the top. `WithItems` still starts at the top.
- `TextInput.SelectAll()` is public.
- Fix: a `TextInput` offered unlimited width (a `Row` measuring with no width limit, an overlay) asked for infinite width. It now asks for its `MinWidth` and `Grow` decides the rest.
- New theme tokens: `SwatchSize`, `ColorAreaSize`, `ColorStripHeight`.
- Gallery: new Lists tab (reorderable, renamable list and a 2,000-row list) and Colors tab (swatches, inline picker, toasts); the Inputs tab shows `NumberField`.
- 40 new tests (270 in total).

### M2 retro
- `Separator` asks for no length along its line, so a vertical separator no longer makes a toolbar row as tall as the window (before, it asked for all the height it was offered). Stretch alignment, the default, still fills the slot.
- `RadioGroup<T>` raises `SelectionChanged` only when the selection changes; setting the current value again raised it before.
- `ToggleGroup<T>` builds both option kinds through one path, so text and icon options share their colors.
- 3 new tests (230 in total).

### Icons, toggle groups, split panels, sections
- New `Icon`: a vector icon from SVG path data laid out in a `ViewBox` square (24 by default), filled (nonzero) or stroked with round caps (`IconStyle`, `StrokeWidth` in view-box units). Sized by the new theme token `IconSize` until `Size` is set; color from `Color` / `ColorSource`, default `OnSurface` (`OnSurfaceMuted` when disabled). The path is parsed on first draw and released when the icon leaves the screen; invalid path data draws nothing.
- New `IconButton`: a borderless, focusable button showing an `Icon`, with hover and pressed fills and the `Pressed` event. Wrap it in a `Tooltip` to name it.
- New `ToggleGroup<T>`: a segmented control with text or icon options (icon options take an optional tooltip), horizontal or vertical. `SelectedValue`, `SelectionChanged` (once per change, never for setting the current value), `WithSelected`, `OnChange`, `Bind`.
- New `SplitPanel`: two panes with a draggable divider, horizontal or vertical. The `FixedSide` pane is `SplitSize` long and keeps it when the panel resizes; the other takes the rest, at least `MinOtherSize`. `MinSplitSize` / `MaxSplitSize` clamp the size. When `Collapsible`, double-clicking the divider or dragging it below half of `MinSplitSize` hides the fixed pane (`IsCollapsed`). Events `SplitSizeChanged`, `CollapsedChanged`. The divider shows a resize cursor.
- New `Section`: a titled group whose body collapses when the header is clicked (or Space/Enter on the focused header), with `IsExpanded` and `ExpandedChanged`. Stacked sections are divided by a `BorderSubtle` line.
- New `DrawContext.FillPath` and `StrokePath` (thickness 0 draws a hairline).
- New `UiHost.UnhandledKeyUp`: key releases no focused widget handled, raised also when nothing has focus (before, a release with no focus went nowhere), so an app can end a held-key mode such as Space to pan.
- New theme tokens: `IconSize`, `SplitterSize`, `IconButtonPadding`, `SegmentPadding`, `SectionHeaderPadding`.
- Gallery: new Icons tab (icons, icon buttons, toggle groups) and Panels tab (a three-pane split layout with an inspector of sections).
- Fix: a disabled `Button` was drawn exactly like an enabled one. It now uses the `Surface` fill (none for `Ghost`), a `Border` outline and `OnSurfaceMuted` text.
- 28 new tests (227 in total).

### Viewport
- New `Viewport` widget: a pan and zoom camera over an unbounded content space. Subclasses draw in content space by overriding `DrawContent`; child widgets are laid out once at their natural size and moved by the view transform. Wheel zooms around the pointer (`WheelZoomStep`), a drag with one of `PanButtons` pans (default middle and right; add `Left` for a pan tool) and shows `PanCursor`. Code API: `SetView`, `PanBy`, `ZoomAt`, `ZoomAtCenter`, `Fit(rect, padding)` (refits on resize until the view is changed another way), `ContentToView` / `ViewToContent`, `ViewTransform`, `MinZoom` / `MaxZoom`, `IsPanning` and the `ViewChanged` event.
- New `PointerButtons` flags enum.
- Gallery: the Transform tab becomes the Viewport tab and uses `Viewport` instead of its own zoom view.

### Change events
- **Breaking:** value-change callbacks are now `event`s instead of public delegate fields, like the input events. Before, assigning one replaced every other handler: `slider.Bind(volume).OnChange(...)` silently dropped the binding, because `Bind` subscribed and `OnChange` overwrote. Migrate `x.OnChanged = h` to `x.ValueChanged += h` (or keep using the fluent method):
  | Widget | Before | After |
  |---|---|---|
  | `Button` | `OnPressed` | `Pressed` |
  | `Checkbox`, `Toggle` | `OnChanged` | `CheckedChanged` |
  | `Slider`, `NumberInput`, `TextInput` | `OnChanged` | `ValueChanged` |
  | `TextInput` | `OnSubmit` | `Submitted` |
  | `Dropdown<T>`, `RadioGroup<T>` | `OnChanged` | `SelectionChanged` |
  | `Tabs` | `OnTabChanged` | `SelectionChanged` |
  | `Dialog` | `OnClosed` | `Closed` |
  | `Overlay` | `OnDismiss` (property) | `Closed` |
- Fluent `OnChange(...)`, `OnPress(...)`, `WithSubmit(...)` and the constructor callbacks of `Button`, `Checkbox` and `Toggle` now add a handler instead of replacing the previous one.
- **Breaking:** `Checkbox` and `Toggle` raise `CheckedChanged` whenever `IsChecked` changes, including from code and from `Bind`, like every other widget's change event. Before, only a click or key press raised it.

### Retro fixes
- `Rect.Contains` is half-open: the right and bottom edges are outside. Before, two widgets sharing an edge both contained a point on it and the later one won silently.
- `Slider`: steps count from `Min` (before: from 0, so `Min = 1, Step = 2` snapped to even numbers), a snapped value never passes `Max`, and setting `Step` snaps the current value.
- `TextInput.Validate` redraws the field when set, so the invalid border shows at once.
- **Breaking:** removed unused `Size.Constrain`, `Rect.Intersects`, `SKColor.Lighten` and `SKColor.Darken`.

### Tests and docs
- 38 new tests (175 in total): measure and arrange of `Stack`, `Wrap`, `Canvas` with explicit size, `Grid` fixed/star rows, auto tracks, inferred rows, spans and out-of-range cells, hidden children in `Row`/`Column`, cross-axis alignment; right/bottom alignment, margins, min/max, explicit size and aspect ratio; `Handled` stopping a bubbling press, `Source` while bubbling, releasing capture mid-drag, capture without a press, disabled widgets passing clicks to their parent; Tab skipping hidden and disabled branches, focus event order, Shift+Tab from nothing; dirty flags on ancestors and not on siblings, render-only changes skipping layout, layout on detached widgets, showing and hiding a child.
- `README.md` gains Input and focus and Testing sections, a corrected panel table (`Grid` tracks, `Canvas`, `Grow`) and the missing `Checkbox` and `VirtualList`.
- `ARCHITECTURE.md`: the new-widget example uses `DrawContext` and theme tokens (it used removed `SKCanvas` extensions and literal sizes); new Testing section listing the test helpers; the roadmap lists what is still missing instead of widgets that exist.
- `CLAUDE.md`: `Grid`, `Stack` and `PaddingBox` API, and a Testing section.
- Gallery: the Layout tab shows `Stack` (layered children with their own alignment); it was the only block missing from the Gallery.

### Composite cleanup
- `ContextMenu` and `Dropdown` share one popup panel (the context menu's own copy is gone). Context menu separators are inset by the theme's `SpacingXs` instead of a fixed 4 × 2.
- `Separator` draws inside its `Padding`, so padding insets the line. It takes no length when measured unconstrained, so a separator in a panel that sizes to its content no longer makes that panel infinitely wide; with stretch alignment (the default) it still fills its slot.
- `Scroll` computes viewport, thumb and track geometry in one helper used by layout, drawing, thumb dragging, track clicks and the wheel (before: five copies).

### Theme
- Built-in widgets take every visual value from the theme: no hardcoded colors, font sizes, radii, paddings or control sizes remain in the library. New tokens:
  - Colors: `BorderSubtle`, `Scrim`, `Shadow`, `Selection`, `Knob`, `ScrollbarTrack`, `ScrollbarThumb`, `ScrollbarThumbActive`. `PrimaryHoverGhost` can now be set. Colors marked derived (`PrimaryHoverGhost`, `BorderSubtle`, `FocusRing`, `Selection`, scrollbar colors) follow their base token (e.g. `Selection` = `Primary` at 30%) unless set.
  - Shadow and focus: `ShadowBlur`, `ShadowOffsetY`, `FocusRingWidth`.
  - Typography: `FontSizeSubheading`, `FontSizeHeading`, `LineHeight` (multiplier, 1.4), `FontSize(TextRole)`.
  - Paddings (`Thickness`): `ButtonPadding`, `InputPadding`, `MenuItemPadding`, `PopupPadding`, `TooltipPadding`, `DialogPadding`, `TabPadding`.
  - Control sizes: `CheckboxSize`, `RadioSize`, `ToggleWidth`, `ToggleHeight`, `SliderThumbSize`, `SliderTrackHeight`, `ProgressBarHeight`, `ScrollbarSize`, `ScrollbarMinThumb`, `TabBarSize`.
- **Breaking:** `Theme` is a `sealed record`: make variants with `with` (`Theme.Dark with { Primary = ... }`). It can no longer be subclassed.
- **Breaking:** renamed tokens. `FontSizeBase` → `FontSizeBody`; `FontSizeLarge` → `FontSizeHeading` (still 18); `CornerRadiusSm` / `Md` / `Lg` → `CornerRadiusSmall` (4) / `CornerRadiusControl` (6) / `CornerRadiusPopup` (8), plus new `CornerRadiusDialog` (12). `FontSizeTitle` is now 22 (was 24).
- **Breaking:** the static `ThemeLibrary` is replaced by a `ThemeRegistry` instance per host, `UiHost.Themes`, so two hosts no longer share registrations. `ThemeLibrary.Register/Remove/Names/DefaultName/Get` → `host.Themes.Register/...`. The built-in names and palettes moved to `BuiltInThemes` (`BuiltInThemes.Default`, `BuiltInThemes.ForestDark`, ...); the palettes are now public for reuse. Register custom themes on the host before building UI that lists `host.Themes.Names`.
- **Breaking:** `UiHost.FontFamily` is removed; the font family is `Theme.FontFamily` (before, that token was ignored). `BindFontFamily` still works and overrides the family of whichever theme `BindTheme`/`BindDarkMode` select. `FontCache.DefaultFamily` is removed (use `Theme.Light.FontFamily`).
- **Breaking:** `DrawContext(canvas, theme, pixelScale, fontFamily, fonts)` loses the `fontFamily` parameter; `DrawContext.FontFamily` returns `Theme.FontFamily`.
- New `Label.Role` (`TextRole`: `Body`, `Small`, `Subheading`, `Heading`, `Title`) and `WithRole(...)`: the label's size comes from the theme. An explicit `FontSize` still wins.
- `Widget.Padding` and `Widget.CornerRadius` use the widget's themed default until set; a set value always wins. Custom widgets override the new `protected virtual DefaultPadding(Theme)` / `DefaultCornerRadius(Theme)`, and read tokens through the new protected `Widget.Theme` (the host's theme, `Theme.Light` while detached). `LinearPanel.Spacing` likewise has `DefaultSpacing(Theme)` (0 for `Row`/`Column`).
- `RadioGroup<T>`'s `spacing` parameter defaults to the theme's `SpacingSm` (was 8).
- `Theme.Dark` sets a heavier `Scrim` and `Shadow` for dark backgrounds.
- Small visual changes from aligning to shared tokens: buttons use `CornerRadiusControl` (6, was 8) like text fields and dropdowns; the dropdown button is 2 units shorter, matching a text field; radio labels sit 8 units from the circle (was 10); `VirtualList`'s scrollbar matches `Scroll`'s; menu headers are 12 (was 11); dialog titles use `FontSizeSubheading`; overlay shadows follow the content's corner radius.

### Layout
- **Breaking:** `Widget` applies `Padding` itself. `MeasureCore` gets the space inside the padding and returns the content size; `ArrangeCore` gets the content box. `LayoutBounds` (background, border, hit area) and explicit `Width`/`Height` include the padding. Setting `Padding` on a `Column`, `Row`, `Grid` or any widget now works; before, only `PaddingBox`, `Button`, `TextInput` and menu items honored it. Custom widgets that subtracted `Padding` themselves must stop doing so. New `ContentBounds` (`LayoutBounds` minus `Padding`) for drawing content. `PaddingBox` still works and is now just a widget with `Padding` set.
- **Breaking:** `Row` and `Column` are the two forms of the new `LinearPanel` (`Orientation`: `Horizontal`/`Vertical`, `Spacing`). Any child can take leftover length with the new `Widget.Grow` (default 0): leftover space is shared by `Grow` factor. `Spacer` is now an empty widget with `Grow = 1` (`new Spacer(2f)` for a factor), no longer a type the panels look for; it behaves as before.
- **Breaking:** `RadioGroup<T>` is a `LinearPanel`; its constructor takes `Orientation` instead of `RadioOrientation`, which is removed (`RadioOrientation.Horizontal` → `Orientation.Horizontal`).
- Layout is cached. `Measure` with the same constraint and `Arrange` with the same rect return at once unless the widget's layout was invalidated, so a change re-lays out only its own branch instead of the whole window. `InvalidateLayout()` stops walking up at an ancestor that is already marked. Theme and font changes still re-measure everything.
- `ArrangeCore` must arrange every visible child. A visible child it skips is arranged in its previous slot (before: left wherever it was).
- New `Widget.ChildTransform` (`Transform2D`: uniform scale then offset; protected setter): children are laid out as usual, then drawn, hit-tested and given pointer positions through it. Changing it only redraws. New `WindowBounds` (`LayoutBounds` in window coordinates through every ancestor's transform) and `PointFromWindow(x, y)`.
- **Breaking:** `Scroll` and `VirtualList` scroll with `ChildTransform`: scrolling no longer runs layout (a `VirtualList` lays out again only when it reaches rows it has not built; it now builds a few rows beyond the viewport). Content `LayoutBounds` no longer move when scrolling; use `WindowBounds` for on-screen position. Anchored overlays follow scrolled anchors.
- **Breaking:** `PointerEventArgs.X/Y` and `WheelEventArgs.X/Y` are in the receiving widget's layout space (the space of its `LayoutBounds`), re-mapped as the event bubbles past a transform; they equal window coordinates outside scrolled or zoomed content. New `WindowX/WindowY` hold window coordinates for every receiver (use them to open overlays, as `ContextMenu` does). Both now derive from the new `PositionedEventArgs`.
- After a wheel event, hover is updated for the content that moved under the pointer.
- Children outside the visible clip are not drawn (culling), so long scrolled content draws only what is on screen.
- `Grid` and `Canvas` keep each child's cell or position on the child (new `protected static GetParentData(child)` / `SetParentData(child, data)` for custom panels); it is dropped automatically when the child leaves.
- `Grid` and `Wrap` reuse their buffers: laying them out allocates nothing.
- `Stack` uses `Widget`'s default layout instead of a copy of it.
- Fix: a widget with `Margin` and non-stretch alignment was arranged with its margin added to its width or height.

### Routed input
- **Breaking:** widget input is now events plus overridable methods instead of public delegate fields, so your handlers add to a widget's behavior instead of replacing it (before, setting `OnClick` on a `Toggle` stopped it toggling). Migrate:
  | Before | After (user code) | After (inside a widget subclass) |
  |---|---|---|
  | `w.OnClick = e => …` | `w.Click += e => …` | `protected override void OnClick(PointerEventArgs e)` |
  | `OnPointerDown` / `OnPointerUp` / `OnPointerMove` | `PointerPressed` / `PointerReleased` / `PointerMoved` | `OnPointerPressed` / `OnPointerReleased` / `OnPointerMoved` |
  | `OnPointerEnter` / `OnPointerExit` | `PointerEntered` / `PointerExited` | `OnPointerEntered` / `OnPointerExited` |
  | `OnScroll = (dx, dy) => …` | `Wheel += e => …` (`e.DeltaX`, `e.DeltaY`) | `OnWheel(WheelEventArgs e)` |
  | `OnKeyDown` / `OnKeyUp` | `KeyDown` / `KeyUp` | `OnKeyDown` / `OnKeyUp` |
  | `OnTextInput` | `TextEntered` | `OnTextEntered(TextInputEventArgs e)` |
- **Breaking:** the `PointerEvent`, `KeyEvent` and `TextInputEvent` structs are replaced by `PointerEventArgs`, `WheelEventArgs`, `KeyEventArgs` and `TextInputEventArgs` (base `InputEventArgs`: `Source`, `Handled`, `Modifiers`, `Ctrl`/`Shift`/`Alt`/`Super`). The host reuses one object per kind so input allocates nothing: read it during the call, do not store it. `PointerEvent.IsDown` is gone (the event says it); `KeyEvent.IsDown` likewise.
- Pointer, wheel, key and text events **bubble** from their source to the root until a handler sets `e.Handled`. On each widget its own handling runs first, then subscribers; `Handled` only stops the next ancestor. Built-in widgets handle only what they act on (a `Scroll` handles the wheel only when it actually scrolled, so nested scrolls pass it on at their ends).
- **Breaking:** `Click` is sent for the left button only, when the press and release happen over the same widget (or its descendants). Use `PointerReleased` and check `e.Button` for other buttons.
- **Pointer capture:** a press captures the pressed widget until every button is released, as before. New `CapturePointer()`, `ReleasePointerCapture()`, `HasPointerCapture`, `UiHost.PointerCapture` and `OnPointerCaptureLost()` / `PointerCaptureLost`, so an ancestor can take over a drag and widgets reset drag state in one place.
- **Hover chain:** `IsHovered` is true on the widget under the pointer and all its ancestors (before: only the deepest widget), with `PointerEntered` / `PointerExited` sent to each.
- **Double click:** `PointerEventArgs.ClickCount` counts presses of the same button within 500 ms and 4 units.
- **Modifiers** on every pointer and wheel event. `KeyEventArgs.IsRepeat` marks auto-repeated key presses.
- **Focus:** new `Widget.Focusable`, `IsFocused` (now on every widget; before only `TextInput`), `Focus()`, `OnGotFocus` / `OnLostFocus` and `GotFocus` / `LostFocus`. A press focuses the nearest focusable widget under the pointer and clears focus otherwise. Tab / Shift+Tab move focus in tree order, wrapping, and stay inside an open modal overlay. A themed focus ring (new `Theme.FocusRing`, default `Primary`) is drawn only when focus came from the keyboard (`UiHost.IsFocusVisible`); override `DrawFocusRing` to change it. `UiHost.FocusedWidget`, `MoveFocus(bool forward)`, `ClearFocus()`.
- Focusable built-ins: `Button`, `Checkbox`, `Toggle`, `RadioGroup` options and the `Dropdown` button react to Space and Enter (the dropdown also opens with Down); `Slider` moves with the arrows, Page Up/Down, Home and End; `TextInput`. `NumberInput`'s −/+ buttons are skipped by Tab, and Up/Down in its field step the value.
- New `UiHost.UnhandledKeyDown`: key presses no focused widget handled, for app-wide shortcuts.
- New `Widget.Cursor` (`CursorShape?`, null inherits): the pointer shape over the widget. `TextInput` shows a text cursor.
- `TextInput`: copy, cut and paste (Ctrl+C/X/V; line breaks paste as spaces; password fields do not copy), Ctrl+Left/Right and Ctrl+Backspace/Delete by word, Shift+click extends the selection, double click selects a word and triple click everything, Escape restores the text from when it was focused and leaves the field, Tab focus selects all, and long text scrolls to keep the caret visible.
- `ContextMenu.Attach` subscribes once on the target and catches right-clicks from descendants added later (before: only those present at the call).
- **Breaking:** an `Overlay` with `DismissOnClickOutside` closes when any button is pressed on the backdrop (before: on a left click), and clicks inside its content no longer reach the backdrop handler. A context menu moves on a right press outside it.
- `Tooltip` reacts to its own hover enter and exit, hides on press, and is hit-testable itself, so it also works around widgets that are not.
- **Breaking:** `IPlatform` gains `TimeSpan Now` (monotonic clock). `IPlatformListener.PointerMove/Down/Up` and `Wheel` take a `KeyModifiers` argument.

### Event-driven rendering
- A window draws only when something changed. An untouched app renders no frames and uses no CPU; before, it redrew the whole tree every vsync. `Invalidate()` and `InvalidateLayout()` ask the host for a frame, and every change made before that frame is drawn in it.
- `DesktopPlatform` sleeps until input, a due timer or a frame request instead of polling every frame, and redraws when the window is restored, moved, resized or focused.
- Layout runs at the start of the next frame (or before routing pointer input, if a change is still pending) instead of immediately on resize.
- Property setters do nothing when given the current value: no layout, no frame, no `OnChanged`. Affects every built-in widget, `TextInput.Value`, `UiHost.Theme`, `UiScale` and `FontFamily`.
- New `UiHost.RequestAnimationFrame(Action<TimeSpan>)`: runs the callback once before the next frame with that frame's time. Request again from the callback to keep animating; frames stop when nothing asks.
- New `protected bool SetLayoutProperty<T>(ref T field, T value)` and `SetRenderProperty<T>(...)` on `Widget` for custom widget properties: they compare, store and invalidate. Use them as `set => SetLayoutProperty(ref field, value);`.
- New `Label.ColorSource` (`Func<Theme, SKColor>?`), like `BackgroundSource`: picks the text color from the theme at draw time and overrides `Color`. Built-in buttons, tabs, menus and dropdowns use it instead of setting `Color` while drawing.
- `TextInput` blinks its caret with a platform timer (two frames a second while focused, none otherwise); `Tooltip` waits for its delay with a timer and reacts to hover changes, instead of both polling every frame.
- Overlays are laid out only when they open, change, or the window or root tree is laid out again; before, every open overlay was measured and arranged every frame.
- **Breaking:** `Widget.IsVisible` now invalidates layout (it changes the size a widget reports), not just drawing.
- **Breaking:** `IPlatformListener.Render(SKCanvas, float deltaSeconds)` → `Render(SKCanvas, TimeSpan frameTime)`, a monotonic clock reading. `IPlatform.RequestFrame()` must now render exactly when asked (and after `Resize`); a platform that keeps rendering continuously still works but wastes the savings.
- **Breaking:** the internal `ITickable` is removed; use `RequestAnimationFrame` for per-frame work and `UiHost.Platform.StartTimer` for delays.
- `Overlay.Anchor`, `AnchorEdge`, `PositionX`/`PositionY`, `ContentHAlign`/`ContentVAlign`, `MatchAnchorWidth` and `IsModal` now invalidate when changed, so moving an open overlay redraws it.

### Widget lifecycle
- Widgets are attached while they are in a tree a `UiHost` shows. New `protected virtual void OnAttached(UiHost host)` and `OnDetached()`; `Widget.Host` is now set on each widget as it is attached instead of being looked up through the parents.
- New `protected IDisposable Observe(Bindable<T>, Action<T>)` and `Observe(ObservableList<T>, Action)`: the subscription is dropped when the widget is detached and restored (with the current value) when it is attached again. Every built-in `Bind`, `BindInt`, `BindFloat` and `WithItems(ObservableList)` uses it, so removed widgets are no longer kept alive by the bindables and lists they were bound to.
- Moving a widget to another parent in the same window does not detach and re-attach it.
- Hover, press and focus are cleared when the widget holding them is detached, so a removed widget stops receiving input.
- `Image` disposes its decoded image when detached. New `DisposeOnDetach` (default true) and `KeepOnDetach()` for images you remove and add again.
- **Breaking:** a closed `Overlay` stays attached to the host it was opened on, so dialogs and menus keep their state and images between openings. New `Overlay.Detach()` closes it and releases it. `Dropdown` and `Tooltip` detach their overlays when they are removed.
- **Breaking:** `VirtualList` rows are real children: created and removed during layout (not while drawing), attached and detached like any widget, hit-tested normally. Scrolling a `VirtualList` now re-runs layout.
- **Breaking:** `Widget.Children` is a `ChildList` (read-only, `IReadOnlyList<Widget>`) instead of `IReadOnlyList<Widget>`. `foreach` over it no longer allocates.

### Performance
- `Label` caches one `SKTextBlob` per line and draws it with the new `DrawContext.DrawText(SKTextBlob?, ...)`. Drawing unchanged labels allocates nothing; the blobs are freed on detach.

### Display scaling
- Apps are sharp on scaled displays. The platform reports the surface size in pixels and the display scale; `UiHost` applies `DpiScale × UiScale` once as the canvas transform and converts pointer positions with the same factor. Layout, drawing, fonts and input are in logical units everywhere else.
- `DesktopPlatform` reads the display scale from GLFW, draws to a surface the size of the framebuffer (before: the window size, wrong on Retina), and treats its width and height as logical units: on a 150% display `new DesktopPlatform("App", 900, 700)` opens a 1350×1050 pixel window. It also resizes the window when it moves to a display with a different scale.
- **Breaking:** `IPlatformListener.Resize(width, height)` → `Resize(pixelWidth, pixelHeight, dpiScale)`. Sizes and pointer positions are surface pixels.
- **Breaking:** `DrawContext`'s `dpiScale` parameter and `DpiScale` property are now `pixelScale` / `PixelScale` (canvas pixels per logical unit, display scale × UI scale). Fonts from `GetFont` and `MakeTextFont` are no longer multiplied by it: the canvas is already scaled, so text was going to be scaled twice.
- New `UiHost.PixelScale` (`DpiScale × UiScale`). `UiHost.DpiScale` now holds the real display scale instead of always 1.

### Platform layer
- **Breaking:** `Application` is gone. An app is now a platform-neutral `UiHost` (tree, layout, drawing, input routing, overlays, focus, theme) running on an `IPlatform` backend. The desktop backend, `DesktopPlatform`, lives in the new `ApexUI.Desktop` project, the only code that uses Silk.NET. Migrate:
  ```csharp
  // before
  new Application("App", 900, 700).SetIcon("icon.svg").BindTheme(preset).Run(root);
  // after
  new UiHost(new DesktopPlatform("App", 900, 700).WithIcon("icon.svg")).BindTheme(preset).Run(root);
  ```
- **Breaking:** desktop apps reference `lib/ApexUI.Desktop/ApexUI.Desktop.csproj` (it brings in `ApexUI`) and add `using ApexUI.Desktop;`. `lib/ApexUI` now depends only on SkiaSharp and Svg.Skia.
- `IPlatform` covers the window and GPU surface (`Run`), frame requests, clipboard, cursor shape (`CursorShape`), UI-thread timers (`StartTimer`) and file dialogs (`OpenFileAsync`/`SaveFileAsync`, declared but not implemented on desktop yet). The backend reports events through `IPlatformListener`, which `UiHost` implements; tests can drive a `UiHost` with a fake platform and no window.
- `Widget.Host` is the `UiHost` showing the widget's tree, or null while it is not on screen. `UiHost.Platform` reaches clipboard, cursor and timers.
- **Breaking:** the static `Application.Current` is removed, so overlays need to know which window to open in:
  - `Overlay.Open()` → `Overlay.Open(UiHost host)`. `Overlay.Open(Widget anchor, edge)` uses the anchor's host and throws if the anchor is not on screen.
  - `Dialog.Show()` → `Dialog.Show(UiHost host)`; inside a widget pass `Host!`.
  - `ContextMenu.Show(x, y)` → `ContextMenu.Show(UiHost host, x, y)`. `ContextMenu.Attach` is unchanged.
- SkiaSharp native assets for Windows, macOS and Linux ship with `ApexUI.Desktop` (before: Windows only).

### Input
- **Breaking:** `KeyEvent` is now `KeyEvent(Key Key, KeyModifiers Modifiers, bool IsDown)` with a `Key` enum (physical keys: `A`–`Z`, `D0`–`D9`, `F1`–`F12`, `Left`, `Enter`, `KeypadEnter`, ...) and `KeyModifiers` flags (`Shift`, `Control`, `Alt`, `Super`). `Ctrl`, `Shift`, `Alt` and `Super` remain as read-only properties. Before, keys were strings such as `"ArrowLeft"` or Silk.NET enum names.
- **Breaking:** typed characters no longer arrive as one-character `KeyDown` events. They arrive in the new `Widget.OnTextInput` (`TextInputEvent(string Text)`), already translated by the keyboard layout; `KeyDown` is sent for every key, including letters.
- Key-up events now carry the modifiers held at the time; before they always reported none.

### Fixes
- `TextInput`: Backspace at the end of the text moved the caret two places back, so the next character was inserted one place too early.

### Performance
- Drawing no longer creates Skia objects per call. `DrawContext` reuses its fill, stroke, text, shadow and layer paints, one path, one rounded rect and cached blur filters; text uses a `FontCache`. In the Gallery, bytes allocated per frame dropped from 8–52 KB to 2.5–16 KB, and per layout pass from 17–210 KB to 0.7–40 KB. What remains in drawing is SkiaSharp's own `DrawText` (about 160 B per call).
- The application keeps one `DrawContext` and one `FontCache` for its lifetime instead of creating a context every frame.
- `Label` caches its measured width and wrapped lines until text, font, size or width change.
- `TextInput` caches its displayed text and caret positions; clicking to place the caret no longer re-measures every prefix of the text.
- `Toggle`, `Checkbox` and `RadioGroup` measure labels with cached fonts.

### Added
- `FontCache`: typefaces and fonts by family, size and weight. Fonts it returns are owned by the cache.
- `DrawContext.GetFont(sizePx, bold)`: cached font for custom text drawing. Do not dispose it.
- `DrawContext.MeasureText(ReadOnlySpan<char>, ...)`: measure part of a string without a substring.
- `DrawContext` constructor takes an optional shared `FontCache`.

### Changed
- `DrawContext` is now `IDisposable`. Dispose contexts you create yourself (for example when rendering off-screen).
- `MakePaint`, `MakeTextFont` and `MakeTextPaint` still return new objects the caller must dispose. `MakeTextFont` no longer leaks its typeface.

### Fixes
- The built-in dark theme now sets `SurfacePressed`, `PrimaryHover`, `Success`, `Warning` and `Danger`; before, it inherited the light values (pressed buttons turned light grey).
- Setting `TextInput.Value` from code to a shorter string no longer crashes drawing when the caret or selection was past the new end (e.g. `NumberInput` re-formatting on Enter).
- `Grid` star columns/rows size like `Auto` under an infinite constraint (inside a horizontal `Scroll` or an unconstrained `Row`) instead of becoming infinitely large.
- `Label` measures with the same default font size it draws with (`Theme.FontSizeBase`), so its box matches its text.
- `RadioGroup` items use the application font family instead of a hardcoded "Segoe UI".
- `Slider` re-clamps `Value` when `Min` or `Max` changes.
- `Toggle`, `Checkbox` and `RadioGroup` no longer leak a typeface on every layout pass.
- Adding a widget that already has a parent moves it: it is removed from the old parent first.
- `Grid` and `Canvas` forget a child's cell or position when it is removed in any way, including `RemoveAllChildren()` and moving it to another parent.

### Changed
- Exceptions thrown while drawing now propagate instead of being drawn as a pink box and logged; they were hiding real bugs.
- New `protected virtual void OnChildRemoved(Widget child)` on `Widget` for panels that keep per-child data.
- The Gallery builds framework-dependent; its self-contained `Full` profile applies only to `dotnet publish`.
- New Gallery smoke test: every tab is laid out and drawn off-screen in the light and dark themes.

### Removed
- **Breaking:** `SKCanvasExtensions` (`canvas.FillRoundRect`, `canvas.StrokeRoundRect`, `canvas.DrawTextCentered`). Use the `DrawContext` helpers.
- **Breaking:** `FrameworkInfo`.

### Project structure
- **Breaking:** ApexUI is now a class library at `lib/ApexUI/ApexUI.csproj`. Reference it with a `ProjectReference` instead of importing `lib/ApexUI.props` (removed).
- **Breaking:** `global using` directives no longer leak into apps. Add `using ApexUI.Core;`, `using ApexUI.Layout;`, `using ApexUI.Widgets;`, `using ApexUI.Extensions;` and `using SkiaSharp;` (or your own `GlobalUsings.cs`) where needed.
- The demo app moved to `src/Gallery` (run with `dotnet run --project src/Gallery`); examples now use the `ApexUI.Gallery.Examples` namespace. Publish profiles moved with it.
- Added `tests/ApexUI.Tests` (xUnit) with the first layout tests for `Row`, `Column`, `Spacer`, margin and alignment.
- Added `ApexUI.sln` and a root `Directory.Build.props` (net10.0, nullable, implicit usings, warnings as errors, latest C#).
- Package versions are pinned: Silk.NET 2.23.0, SkiaSharp 3.119.4, Svg.Skia 2.0.0.8.

### Themes
- **Breaking:** `ThemeLibrary` is no longer `partial` and the `RegisterCustom()` hook is gone (partial classes cannot span assemblies). Call `ThemeLibrary.Register(name, light, dark)` at startup instead.
