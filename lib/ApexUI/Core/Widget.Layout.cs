namespace ApexUI.Core;

/// Layout: Measure(available) reports DesiredSize, then Arrange(rect) sets LayoutBounds.
/// Both are cached: a widget whose layout was not invalidated and that gets the same
/// constraint (or rect) again returns at once, so only changed branches are laid out.
public abstract partial class Widget
{
    // ── Layout properties ─────────────────────────────────────────────────

    /// Width including Padding; NaN sizes to content.
    public float Width  { get; set => SetLayoutProperty(ref field, value); } = float.NaN;
    public float Height { get; set => SetLayoutProperty(ref field, value); } = float.NaN;

    public float MinWidth  { get; set => SetLayoutProperty(ref field, value); } = 0f;
    public float MinHeight { get; set => SetLayoutProperty(ref field, value); } = 0f;
    public float MaxWidth  { get; set => SetLayoutProperty(ref field, value); } = float.PositiveInfinity;
    public float MaxHeight { get; set => SetLayoutProperty(ref field, value); } = float.PositiveInfinity;

    // NaN = no constraint. Mode controls which axis drives the other.
    public float           AspectRatio     { get; set => SetLayoutProperty(ref field, value); } = float.NaN;
    public AspectRatioMode AspectRatioMode { get; set => SetLayoutProperty(ref field, value); } = AspectRatioMode.WidthControlsHeight;

    /// Space outside the widget's bounds.
    public Thickness Margin { get; set => SetLayoutProperty(ref field, value); } = Thickness.Zero;

    /// Space inside the widget's bounds around its content. MeasureCore and ArrangeCore
    /// work on the content box; background, border and LayoutBounds include the padding.
    /// Until set, the widget's themed default (DefaultPadding).
    public Thickness Padding
    {
        get => _padding ?? DefaultPadding(Theme);
        set => SetLayoutProperty(ref _padding, value);
    }
    private Thickness? _padding;

    /// Padding used until Padding is set. Override to take it from the theme.
    protected virtual Thickness DefaultPadding(Theme theme) => Thickness.Zero;

    public HAlign HAlign { get; set => SetLayoutProperty(ref field, value); } = HAlign.Stretch;
    public VAlign VAlign { get; set => SetLayoutProperty(ref field, value); } = VAlign.Stretch;

    /// Share of a Row's or Column's leftover space this child takes, in proportion to the other
    /// children's Grow. 0 (default) keeps the child at its desired size.
    public float Grow { get; set => SetLayoutProperty(ref field, value); }

    // ── Layout results ────────────────────────────────────────────────────

    /// Bounds set by the last Arrange, in the widget's layout space (see ChildTransform).
    public Rect LayoutBounds { get; internal set; }

    /// LayoutBounds without Padding: where the content goes.
    public Rect ContentBounds => LayoutBounds.Deflate(Padding);

    /// Size including Margin, set by the last Measure.
    public Size DesiredSize { get; private set; }

    // ── Invalidation and cache ────────────────────────────────────────────

    private bool _measureDirty = true;
    private bool _arrangeDirty = true;
    private bool _arranging;
    private Size _measuredFor;
    private int  _measuredGeneration;
    private Rect _arrangedFor;
    private Size _arrangedDesired;
    private int  _arrangedGeneration;

    // Changes the host makes to every widget's measurement (theme, font) bump its generation
    // instead of walking the tree; closed overlays catch up when they open.
    private int LayoutGeneration => Host?.LayoutGeneration ?? 0;

    /// Marks this widget and its ancestors for measure and arrange, and asks the host for a frame.
    public void InvalidateLayout()
    {
        _measureDirty = _arrangeDirty = true;
        // An ancestor already marked for both has its own ancestors marked too:
        // layout only clears a widget after arranging every visible child.
        // An ancestor in the middle of arranging lays its dirty children out again before it
        // finishes (ArrangeDirtyChildren), so the walk stops there.
        for (var w = Parent; w is not null && !w._arranging && !(w._measureDirty && w._arrangeDirty); w = w.Parent)
            w._measureDirty = w._arrangeDirty = true;
        Host?.RequestFrame();
    }

    // A widget adding or removing children in its own ArrangeCore arranges them there.
    private void InvalidateLayoutUnlessArranging()
    {
        if (!_arranging) InvalidateLayout();
    }

    /// Asks the host to redraw. The whole window is redrawn, so nothing is tracked per widget.
    public void Invalidate() => Host?.RequestFrame();

    internal bool IsLayoutDirty => _measureDirty || _arrangeDirty;

    // For widgets whose arrangement depends on something outside their subtree (anchored overlays).
    internal void InvalidateArrange() => _arrangeDirty = true;

    /// Sets a property that changes the widget's size or position; does nothing when the value is unchanged.
    /// Returns whether it changed.
    protected bool SetLayoutProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        InvalidateLayout();
        return true;
    }

    /// Sets a property that changes only how the widget looks; does nothing when the value is unchanged.
    /// Returns whether it changed.
    protected bool SetRenderProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Invalidate();
        return true;
    }

    // ── Per-child data for panels ─────────────────────────────────────────

    private object? _parentData;

    /// Data a panel keeps on its child (a grid cell, a canvas position). Cleared when the child
    /// leaves its parent, so it can never refer to a removed child.
    protected static object? GetParentData(Widget child) => child._parentData;

    protected static void SetParentData(Widget child, object? data) => child._parentData = data;

    // ── Measure ───────────────────────────────────────────────────────────

    public void Measure(Size available)
    {
        if (!IsVisible)
        {
            DesiredSize = Size.Zero;
            return;
        }

        int generation = LayoutGeneration;
        if (!_measureDirty && available == _measuredFor && generation == _measuredGeneration) return;

        var box = new Size(
            Math.Max(0, available.Width  - Margin.Horizontal),
            Math.Max(0, available.Height - Margin.Vertical));
        var padding = Padding;
        var content = new Size(
            Math.Max(0, box.Width  - padding.Horizontal),
            Math.Max(0, box.Height - padding.Vertical));

        var desired = MeasureCore(content);

        float w = float.IsNaN(Width)  ? desired.Width  + padding.Horizontal : Width;
        float h = float.IsNaN(Height) ? desired.Height + padding.Vertical   : Height;

        // Aspect ratio: compute the dependent dimension.
        // When Stretch + finite available, use the available size so auto rows/cols in Grid size correctly.
        if (!float.IsNaN(AspectRatio) && AspectRatio > 0f)
        {
            if (AspectRatioMode == AspectRatioMode.WidthControlsHeight)
            {
                float aw = HAlign == HAlign.Stretch && !float.IsInfinity(box.Width) ? box.Width : w;
                h = aw / AspectRatio;
            }
            else
            {
                float ah = VAlign == VAlign.Stretch && !float.IsInfinity(box.Height) ? box.Height : h;
                w = ah * AspectRatio;
            }
        }

        w = Math.Clamp(w, MinWidth,  MaxWidth);
        h = Math.Clamp(h, MinHeight, MaxHeight);

        DesiredSize         = new Size(w + Margin.Horizontal, h + Margin.Vertical);
        _measureDirty       = false;
        _measuredFor        = available;
        _measuredGeneration = generation;
    }

    /// Measures content inside Padding. Default: children overlap (Stack), the largest wins.
    protected virtual Size MeasureCore(Size available)
    {
        float w = 0f, h = 0f;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            child.Measure(available);
            w = Math.Max(w, child.DesiredSize.Width);
            h = Math.Max(h, child.DesiredSize.Height);
        }
        return new Size(w, h);
    }

    // ── Arrange ───────────────────────────────────────────────────────────

    public void Arrange(Rect finalRect)
    {
        if (!IsVisible) return;

        int generation = LayoutGeneration;
        if (!_arrangeDirty && finalRect == _arrangedFor && DesiredSize == _arrangedDesired
            && generation == _arrangedGeneration) return;

        LayoutBounds = ApplyAlignment(finalRect.Deflate(Margin), DesiredSize);
        _arranging = true;
        ArrangeCore(ContentBounds);
        ArrangeDirtyChildren();
        _arranging = false;

        _arrangeDirty       = false;
        _arrangedFor        = finalRect;
        _arrangedDesired    = DesiredSize;
        _arrangedGeneration = generation;
    }

    /// Positions children inside the content box (LayoutBounds without Padding).
    /// Must arrange every visible child. Default: each child fills the content box.
    protected virtual void ArrangeCore(Rect finalRect)
    {
        foreach (var child in _children)
        {
            child.Measure(new Size(finalRect.Width, finalRect.Height));
            child.Arrange(finalRect);
        }
    }

    // A visible child left dirty would stop later invalidations below it from reaching this
    // widget (see InvalidateLayout). Such a child was either skipped by ArrangeCore or changed
    // after it was arranged; it is laid out again with the constraint and slot it last had.
    private void ArrangeDirtyChildren()
    {
        foreach (var child in _children)
        {
            if (!child.IsVisible || !child.IsLayoutDirty) continue;
            child.Measure(child._measuredFor);
            child.Arrange(child._arrangedFor);
        }
    }

    private Rect ApplyAlignment(Rect available, Size desired)
    {
        // DesiredSize includes the margin, which available already excludes.
        float dw = desired.Width  - Margin.Horizontal;
        float dh = desired.Height - Margin.Vertical;
        float w = HAlign == HAlign.Stretch ? available.Width  : Math.Min(dw, available.Width);
        float h = VAlign == VAlign.Stretch ? available.Height : Math.Min(dh, available.Height);

        if (!float.IsNaN(AspectRatio) && AspectRatio > 0f)
        {
            if (AspectRatioMode == AspectRatioMode.WidthControlsHeight)
                h = w / AspectRatio;
            else
                w = h * AspectRatio;
        }

        float x = available.X + HAlign switch
        {
            HAlign.Center => (available.Width - w) * 0.5f,
            HAlign.Right  => available.Width - w,
            _ => 0
        };

        float y = available.Y + VAlign switch
        {
            VAlign.Center => (available.Height - h) * 0.5f,
            VAlign.Bottom => available.Height - h,
            _ => 0
        };

        return new Rect(x, y, w, h);
    }
}

public enum HAlign { Left, Center, Right, Stretch }
public enum VAlign { Top,  Center, Bottom, Stretch }
public enum AspectRatioMode { WidthControlsHeight, HeightControlsWidth }
