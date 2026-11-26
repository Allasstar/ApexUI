namespace ApexUI.Core;

/// Every visual value the built-in widgets use: colors, typography, radii, spacing, paddings
/// and control sizes. Widgets read the host's theme at layout and draw time, so replacing it
/// restyles the whole window. Derive a variant with `with`:
///
///   host.Theme = Theme.Dark with { Primary = new(255, 128, 0), FontSizeBody = 15f };
///
/// Colors marked "derived" follow another token unless set.
public sealed record Theme
{
    public static Theme Light { get; } = new();

    // Neutral greys with a slight cool tint, so user colours read true; one blue accent.
    public static Theme Dark { get; } = new()
    {
        Background     = new(23,  24,  27),
        Surface        = new(35,  36,  40),
        SurfaceHover   = new(45,  47,  52),
        SurfacePressed = new(56,  58,  64),
        OnSurface      = new(228, 230, 234),
        OnSurfaceMuted = new(142, 146, 155),
        Primary        = new(76,  141, 246),
        PrimaryHover   = new(106, 161, 248),
        OnPrimary      = new(255, 255, 255),
        Success        = new(61,  179, 126),
        Warning        = new(227, 164, 59),
        Danger         = new(229, 83,  75),
        Border         = new(58,  61,  68),
        // Dark backgrounds need a heavier scrim and shadow to read as depth.
        Scrim          = new(0,   0,   0,   150),
        Shadow         = new(0,   0,   0,   110),
    };

    // ── Surfaces ──────────────────────────────────────────────────────────

    /// Window background; also the fill of text fields and the active tab.
    public SKColor Background     { get; init; } = new(255, 255, 255);

    /// Fill of buttons, cards, menus and other raised areas.
    public SKColor Surface        { get; init; } = new(245, 245, 245);
    public SKColor SurfaceHover   { get; init; } = new(232, 232, 232);
    public SKColor SurfacePressed { get; init; } = new(208, 208, 208);

    // ── Text ──────────────────────────────────────────────────────────────

    public SKColor OnSurface      { get; init; } = new(26,  26,  26);
    /// Secondary text, placeholders and disabled labels.
    public SKColor OnSurfaceMuted { get; init; } = new(136, 136, 136);

    // ── Accent ────────────────────────────────────────────────────────────

    public SKColor Primary        { get; init; } = new(55,  138, 221);
    /// Primary while hovered or pressed.
    public SKColor PrimaryHover   { get; init; } = new(24,  95,  165);
    /// Text and icons on a Primary fill.
    public SKColor OnPrimary      { get; init; } = new(255, 255, 255);

    /// Tint over a hovered checkbox or radio button. Derived: Primary at 12%.
    public SKColor PrimaryHoverGhost
    {
        get => field == SKColor.Empty ? Primary.WithAlpha(0.12f) : field;
        init;
    }

    // ── Semantic ──────────────────────────────────────────────────────────

    public SKColor Success        { get; init; } = new(29,  158, 117);
    public SKColor Warning        { get; init; } = new(239, 159, 39);
    /// Errors and invalid input.
    public SKColor Danger         { get; init; } = new(226, 75,  74);

    // ── Lines ─────────────────────────────────────────────────────────────

    /// Outlines of controls, separators and unchecked control tracks.
    public SKColor Border         { get; init; } = new(204, 204, 204);

    /// Quiet outlines and dividers inside controls (popup edges, the dropdown divider). Derived: Border at 40%.
    public SKColor BorderSubtle
    {
        get => field == SKColor.Empty ? Border.WithAlpha(0.4f) : field;
        init;
    }

    // ── Overlays ──────────────────────────────────────────────────────────

    /// Dims the window behind a modal overlay.
    public SKColor Scrim          { get; init; } = new(0, 0, 0, 110);
    /// Shadow under overlay content (dialogs, menus, tooltips).
    public SKColor Shadow         { get; init; } = new(0, 0, 0, 55);
    public float   ShadowBlur     { get; init; } = 10f;
    public float   ShadowOffsetY  { get; init; } = 4f;

    // ── Interaction ───────────────────────────────────────────────────────

    /// Keyboard focus outline. Derived: Primary.
    public SKColor FocusRing
    {
        get => field == SKColor.Empty ? Primary : field;
        init;
    }
    public float FocusRingWidth   { get; init; } = 2f;

    /// Background of selected text. Derived: Primary at 30%.
    public SKColor Selection
    {
        get => field == SKColor.Empty ? Primary.WithAlpha(0.3f) : field;
        init;
    }

    /// Movable part of a toggle and the ring around a slider thumb.
    public SKColor Knob           { get; init; } = new(255, 255, 255);

    /// The lit side of a DirectionPicker's sphere. Derived: Knob.
    public SKColor SphereLit
    {
        get => field == SKColor.Empty ? Knob : field;
        init;
    }

    /// The far side of a DirectionPicker's sphere, away from the light. Derived: OnSurfaceMuted at 45%
    /// brightness, dark enough to read against light and dark surfaces alike.
    public SKColor SphereShade
    {
        get => field == SKColor.Empty ? Darker(OnSurfaceMuted, 0.45f) : field;
        init;
    }

    // ── Scrollbars ────────────────────────────────────────────────────────

    /// Derived: Border at 25%.
    public SKColor ScrollbarTrack
    {
        get => field == SKColor.Empty ? Border.WithAlpha(0.25f) : field;
        init;
    }

    /// Derived: OnSurfaceMuted at 60%.
    public SKColor ScrollbarThumb
    {
        get => field == SKColor.Empty ? OnSurfaceMuted.WithAlpha(0.6f) : field;
        init;
    }

    /// Thumb while dragged. Derived: OnSurface at 60%.
    public SKColor ScrollbarThumbActive
    {
        get => field == SKColor.Empty ? OnSurface.WithAlpha(0.6f) : field;
        init;
    }

    public float ScrollbarSize     { get; init; } = 8f;
    public float ScrollbarMinThumb { get; init; } = 20f;

    // ── Typography ────────────────────────────────────────────────────────

    public string FontFamily         { get; init; } = "Segoe UI";
    public float  FontSizeSmall      { get; init; } = 12f;
    public float  FontSizeBody       { get; init; } = 14f;
    public float  FontSizeSubheading { get; init; } = 16f;
    public float  FontSizeHeading    { get; init; } = 18f;
    public float  FontSizeTitle      { get; init; } = 22f;

    /// Line height as a multiple of the font size.
    public float  LineHeight         { get; init; } = 1.4f;

    public float FontSize(TextRole role) => role switch
    {
        TextRole.Small      => FontSizeSmall,
        TextRole.Subheading => FontSizeSubheading,
        TextRole.Heading    => FontSizeHeading,
        TextRole.Title      => FontSizeTitle,
        _                   => FontSizeBody,
    };

    // ── Key hints ─────────────────────────────────────────────────────────

    /// Size of a KeyHint's text and chip text; the chip grows with it. Derived: FontSizeSmall.
    public float KeyHintFontSize
    {
        get => field > 0f ? field : FontSizeSmall;
        init;
    }

    /// A KeyHint's text while not highlighted. Derived: OnSurfaceMuted.
    public SKColor KeyHintText
    {
        get => field == SKColor.Empty ? OnSurfaceMuted : field;
        init;
    }

    /// Outline of a KeyHint's chip while not highlighted. Derived: Border.
    public SKColor KeyHintBorder
    {
        get => field == SKColor.Empty ? Border : field;
        init;
    }

    // ── Corner radii ──────────────────────────────────────────────────────

    /// Checkboxes, tooltips, small badges.
    public float CornerRadiusSmall   { get; init; } = 4f;
    /// Buttons, text fields, dropdowns, menu items.
    public float CornerRadiusControl { get; init; } = 6f;
    /// Menus and popups.
    public float CornerRadiusPopup   { get; init; } = 8f;
    public float CornerRadiusDialog  { get; init; } = 12f;

    // ── Spacing ───────────────────────────────────────────────────────────

    /// Gaps between related items, e.g. a checkbox and its label (Sm) or a dialog's buttons (Sm).
    public float SpacingXs { get; init; } = 4f;
    public float SpacingSm { get; init; } = 8f;
    public float SpacingMd { get; init; } = 16f;
    public float SpacingLg { get; init; } = 24f;

    // ── Paddings ──────────────────────────────────────────────────────────

    public Thickness ButtonPadding   { get; init; } = new(16f, 8f);
    /// Text fields, dropdown buttons and NumberInput's step buttons.
    public Thickness InputPadding    { get; init; } = new(10f, 6f);
    public Thickness MenuItemPadding { get; init; } = new(12f, 6f);
    /// Between a menu's edge and its items.
    public Thickness PopupPadding    { get; init; } = new(4f);
    public Thickness TooltipPadding  { get; init; } = new(8f, 4f);
    public Thickness DialogPadding   { get; init; } = new(20f);
    public Thickness TabPadding      { get; init; } = new(16f, 10f);
    /// Around the icon of an icon button or an icon-only toggle group option.
    public Thickness IconButtonPadding { get; init; } = new(6f);
    /// Around the text of a toggle group option.
    public Thickness SegmentPadding  { get; init; } = new(10f, 4f);
    public Thickness SectionHeaderPadding { get; init; } = new(12f, 8f);

    // ── Control sizes ─────────────────────────────────────────────────────

    public float CheckboxSize      { get; init; } = 18f;
    /// Diameter of a radio button's ring.
    public float RadioSize         { get; init; } = 20f;
    public float ToggleWidth       { get; init; } = 44f;
    public float ToggleHeight      { get; init; } = 24f;
    /// Diameter of a slider's thumb; also the slider's height.
    public float SliderThumbSize   { get; init; } = 20f;
    public float SliderTrackHeight { get; init; } = 4f;
    public float ProgressBarHeight { get; init; } = 8f;
    /// Height of a top or bottom tab bar, width of a side one.
    public float TabBarSize        { get; init; } = 40f;
    /// Width and height of an Icon unless its Size is set.
    public float IconSize          { get; init; } = 16f;
    /// Thickness of a split panel's draggable divider (its grab area; the drawn line is one unit).
    public float SplitterSize      { get; init; } = 6f;
    /// Indent per tree level; the expand arrow sits in one indent's width.
    public float TreeIndent        { get; init; } = 16f;
    /// Width and height of a color swatch button.
    public float SwatchSize        { get; init; } = 18f;
    /// Side of a color picker's saturation/brightness square; its strips are as wide.
    public float ColorAreaSize     { get; init; } = 180f;
    /// Height of a color picker's hue and alpha strips.
    public float ColorStripHeight  { get; init; } = 12f;
    /// Diameter of a DirectionPicker's sphere when no size is set.
    public float DirectionPickerSize { get; init; } = 72f;

    private static SKColor Darker(SKColor c, float brightness) =>
        new((byte)(c.Red * brightness), (byte)(c.Green * brightness), (byte)(c.Blue * brightness), c.Alpha);
}

/// The font size a Label takes from the theme.
public enum TextRole { Body, Small, Subheading, Heading, Title }
