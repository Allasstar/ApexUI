namespace ApexUI.Core;

/// Which edge of the anchor widget the overlay content attaches to.
public enum OverlayAnchor { BelowAnchor, AboveAnchor, RightOfAnchor, LeftOfAnchor }

/// Floating z-layer that renders above the entire widget tree.
/// Application draws all registered overlays after the root tree,
/// so they bypass any clip accumulated by parent panels.
///
/// Usage:
///   var ov = new Overlay { IsModal = true, ContentHAlign = HAlign.Center, ContentVAlign = VAlign.Center };
///   ov.Content = myDialogWidget;
///   button.OnClick = _ => ov.Open();
public class Overlay : Widget
{
    private Widget? _content;

    /// The floating panel shown when this overlay is open.
    public Widget? Content
    {
        get => _content;
        set
        {
            if (_content is not null) RemoveChild(_content);
            _content = value;
            if (_content is not null) AddChild(_content);
            InvalidateLayout();
        }
    }

    /// Widget used to anchor the content position (optional).
    public Widget? Anchor { get; set; }

    /// Which edge of Anchor to place Content against. Ignored when Anchor is null.
    public OverlayAnchor AnchorEdge { get; set; } = OverlayAnchor.BelowAnchor;

    /// Explicit screen position used when Anchor is null and alignment is Left/Top.
    public float PositionX { get; set; }
    public float PositionY { get; set; }

    /// Content alignment on screen when Anchor is null.
    public HAlign ContentHAlign { get; set; } = HAlign.Left;
    public VAlign ContentVAlign { get; set; } = VAlign.Top;

    /// Draws a semi-transparent backdrop behind Content.
    public bool IsModal { get; set; }

    /// Clicking outside Content closes the overlay (default true).
    public bool DismissOnClickOutside { get; set; } = true;

    /// When Anchor is set, stretches Content to match the anchor's width (e.g. for dropdowns).
    public bool MatchAnchorWidth { get; set; }

    public Action? OnDismiss { get; set; }

    public Overlay()
    {
        IsVisible = false;
        // Click on the backdrop (i.e. the overlay itself, not a child) → dismiss.
        OnClick = _ => { if (DismissOnClickOutside) Close(); };
    }

    /// Show this overlay and register it with the Application for top-level rendering.
    public void Open()
    {
        IsVisible = true;
        Application.Current?.RegisterOverlay(this);
    }

    /// Open and anchor Content below/above/beside the given widget.
    public void Open(Widget anchor, OverlayAnchor edge = OverlayAnchor.BelowAnchor)
    {
        Anchor    = anchor;
        AnchorEdge = edge;
        Open();
    }

    /// Hide this overlay and unregister it from the Application.
    public void Close()
    {
        IsVisible = false;
        Application.Current?.UnregisterOverlay(this);
        OnDismiss?.Invoke();
    }

    // ── Layout ────────────────────────────────────────────────────────────────
    // Application calls Measure/Arrange with the full logical screen rect,
    // so LayoutBounds = entire screen (needed for backdrop hit-testing).

    protected override Size MeasureCore(Size available)
        => available;

    protected override void ArrangeCore(Rect screen)
    {
        if (_content is null) return;

        float x, y;

        if (Anchor is not null)
        {
            var ab = Anchor.LayoutBounds;

            // MatchAnchorWidth: measure at anchor width so popup is never narrower than trigger.
            var measureSize = MatchAnchorWidth
                ? new Size(ab.Width, float.PositiveInfinity)
                : Size.Infinite;
            _content.Measure(measureSize);
            var cs = _content.DesiredSize;

            (x, y) = AnchorEdge switch
            {
                OverlayAnchor.AboveAnchor   => (ab.X,            ab.Y - cs.Height),
                OverlayAnchor.RightOfAnchor => (ab.Right,        ab.Y),
                OverlayAnchor.LeftOfAnchor  => (ab.X - cs.Width, ab.Y),
                _                           => (ab.X,            ab.Bottom),  // BelowAnchor
            };

            x = Math.Clamp(x, screen.X, Math.Max(screen.X, screen.Right  - cs.Width));
            y = Math.Clamp(y, screen.Y, Math.Max(screen.Y, screen.Bottom - cs.Height));

            _content.Arrange(new Rect(x, y, cs.Width, cs.Height));
        }
        else
        {
            _content.Measure(Size.Infinite);
            var cs = _content.DesiredSize;

            x = ContentHAlign switch
            {
                HAlign.Center  => screen.CenterX - cs.Width  * 0.5f,
                HAlign.Right   => screen.Right   - cs.Width,
                HAlign.Stretch => screen.X,
                _              => PositionX,
            };
            y = ContentVAlign switch
            {
                VAlign.Center  => screen.CenterY - cs.Height * 0.5f,
                VAlign.Bottom  => screen.Bottom  - cs.Height,
                VAlign.Stretch => screen.Y,
                _              => PositionY,
            };

            x = Math.Clamp(x, screen.X, Math.Max(screen.X, screen.Right  - cs.Width));
            y = Math.Clamp(y, screen.Y, Math.Max(screen.Y, screen.Bottom - cs.Height));

            _content.Arrange(new Rect(x, y, cs.Width, cs.Height));
        }
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        if (IsModal)
        {
            using var p = ctx.MakePaint(new SKColor(0, 0, 0, 110));
            ctx.Canvas.DrawRect(LayoutBounds.ToSKRect(), p);
        }

        // Soft drop-shadow behind Content.
        if (_content is not null)
        {
            var sr = _content.LayoutBounds.ToSKRect();
            sr.Offset(0, 4);
            using var sp = new SKPaint
            {
                Color       = new SKColor(0, 0, 0, 55),
                MaskFilter  = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 10f),
                IsAntialias = true,
            };
            ctx.Canvas.DrawRoundRect(sr, 12f, 12f, sp);
        }
    }
}
