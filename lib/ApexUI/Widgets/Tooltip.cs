namespace ApexUI.Widgets;

public enum TooltipPosition { Below, Above, Right, Left }

/// Transparent decorator that shows a floating tooltip after a hover delay.
/// Wrap any widget: new Tooltip(new Button("Save"), "Save the document")
public sealed class Tooltip : Widget
{
    private readonly Widget _child;
    private readonly Overlay _overlay;
    private readonly Action _onHoverChanged;
    private readonly Action _show;
    private IDisposable? _delay;
    private bool _tooltipVisible;

    public float ShowDelay { get; set; } = 0.6f;
    public TooltipPosition Position { get; set; } = TooltipPosition.Below;

    public Tooltip(Widget child, string text)
    {
        _child = child;
        AddChild(child);

        var label = new Label { Text = text, IsHitTestVisible = false };
        _overlay = new Overlay
        {
            DismissOnClickOutside = false,
            // Non-hit-testable so the tooltip never intercepts pointer events.
            // Without this, the overlay captures hover → target widget loses IsHovered → tooltip
            // immediately closes itself in the next Tick.
            IsHitTestVisible = false,
        };
        _overlay.Content = new PaddingBox(label, new Thickness(8f, 4f))
        {
            BackgroundSource = t => t.Surface,
            CornerRadius = 4f,
        };

        _onHoverChanged = OnHoverChanged;
        _show           = Show;
    }

    protected override void OnAttached(UiHost host) => host.HoveredWidgetChanged += _onHoverChanged;

    protected override void OnDetached()
    {
        Host!.HoveredWidgetChanged -= _onHoverChanged;
        Hide();
        _overlay.Detach();
    }

    // ── Hover ─────────────────────────────────────────────────────────────────

    private void OnHoverChanged()
    {
        if (!IsWithinChild(Host!.HoveredWidget))
        {
            Hide();
            return;
        }
        if (_tooltipVisible || _delay is not null) return;

        if (ShowDelay > 0f)
            _delay = Host.Platform.StartTimer(TimeSpan.FromSeconds(ShowDelay), _show);
        else
            Show();
    }

    private void Show()
    {
        _delay?.Dispose();
        _delay = null;
        _tooltipVisible = true;
        var edge = Position switch
        {
            TooltipPosition.Above => OverlayAnchor.AboveAnchor,
            TooltipPosition.Right => OverlayAnchor.RightOfAnchor,
            TooltipPosition.Left  => OverlayAnchor.LeftOfAnchor,
            _                     => OverlayAnchor.BelowAnchor,
        };
        _overlay.Open(_child, edge);
    }

    private void Hide()
    {
        _delay?.Dispose();
        _delay = null;
        if (!_tooltipVisible) return;
        _tooltipVisible = false;
        _overlay.Close();
    }

    // ── Transparent layout ────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        _child.Measure(available);
        return _child.DesiredSize;
    }

    protected override void ArrangeCore(Rect r)
        => _child.Arrange(r);

    // Pass hit-testing directly to the child so this wrapper is invisible to input.
    public override Widget? HitTest(float x, float y)
    {
        if (!IsVisible || !IsEnabled || !IsHitTestVisible) return null;
        if (!LayoutBounds.Contains(x, y)) return null;
        return _child.HitTest(x, y);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsWithinChild(Widget? hovered)
    {
        for (var w = hovered; w is not null; w = w.Parent)
            if (w == _child) return true;
        return false;
    }
}
