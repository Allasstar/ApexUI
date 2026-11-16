namespace ApexUI.Core;

/// Which edge of the anchor widget the overlay content attaches to.
public enum OverlayAnchor { BelowAnchor, AboveAnchor, RightOfAnchor, LeftOfAnchor }

/// Floating z-layer that renders above the entire widget tree.
/// UiHost draws all open overlays after the root tree,
/// so they bypass any clip accumulated by parent panels.
///
/// Usage:
///   var ov = new Overlay { IsModal = true, ContentHAlign = HAlign.Center, ContentVAlign = VAlign.Center };
///   ov.Content = myDialogWidget;
///   button.Click += _ => ov.Open(button.Host!);
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
    public Widget? Anchor { get; set => SetLayoutProperty(ref field, value); }

    /// Which edge of Anchor to place Content against. Ignored when Anchor is null.
    public OverlayAnchor AnchorEdge { get; set => SetLayoutProperty(ref field, value); } = OverlayAnchor.BelowAnchor;

    /// Explicit screen position used when Anchor is null and alignment is Left/Top.
    public float PositionX { get; set => SetLayoutProperty(ref field, value); }
    public float PositionY { get; set => SetLayoutProperty(ref field, value); }

    /// Content alignment on screen when Anchor is null.
    public HAlign ContentHAlign { get; set => SetLayoutProperty(ref field, value); } = HAlign.Left;
    public VAlign ContentVAlign { get; set => SetLayoutProperty(ref field, value); } = VAlign.Top;

    /// Draws a semi-transparent backdrop behind Content.
    public bool IsModal { get; set => SetRenderProperty(ref field, value); }

    /// Pressing any pointer button outside Content closes the overlay (default true).
    public bool DismissOnClickOutside { get; set; } = true;

    /// When Anchor is set, stretches Content to match the anchor's width (e.g. for dropdowns).
    public bool MatchAnchorWidth { get; set => SetLayoutProperty(ref field, value); }

    public Action? OnDismiss { get; set; }

    public Overlay()
    {
        IsVisible = false;
    }

    // Events from Content bubble here too; only a press on the backdrop itself dismisses.
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Source != this || !DismissOnClickOutside) return;
        e.Handled = true;
        Close();
    }

    /// Show this overlay above everything else in host's window.
    /// The overlay stays attached to host after Close, like a hidden widget, so its content
    /// keeps its state and resources for the next Open. Detach releases it.
    public void Open(UiHost host)
    {
        if (Host is not null && Host != host) Detach();
        AttachToHost(host);
        IsVisible = true;
        host.ShowOverlay(this);
    }

    /// Open and anchor Content below/above/beside the given widget, in the anchor's window.
    public void Open(Widget anchor, OverlayAnchor edge = OverlayAnchor.BelowAnchor)
    {
        var host = anchor.Host
            ?? throw new InvalidOperationException("The anchor widget is not shown by a UiHost.");
        Anchor     = anchor;
        AnchorEdge = edge;
        Open(host);
    }

    public void Close()
    {
        IsVisible = false;
        Host?.HideOverlay(this);
        OnDismiss?.Invoke();
    }

    /// Closes the overlay if open and detaches it from its host, releasing bindings and owned
    /// resources of its content. Opening it again re-attaches it.
    public void Detach()
    {
        if (Host is null) return;
        if (IsVisible) Close();
        DetachFromHost();
    }

    // ── Layout ────────────────────────────────────────────────────────────────
    // UiHost calls Measure/Arrange with the full logical screen rect,
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
            ctx.FillRect(LayoutBounds, new SKColor(0, 0, 0, 110));

        if (_content is not null)
            ctx.DrawShadow(_content.LayoutBounds.Translate(0, 4), 12f, 10f, new SKColor(0, 0, 0, 55));
    }
}
