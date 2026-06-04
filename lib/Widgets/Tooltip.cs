namespace ApexUI.Widgets;

public enum TooltipPosition { Below, Above, Right, Left }

/// Transparent decorator that shows a floating tooltip after a hover delay.
/// Wrap any widget: new Tooltip(new Button("Save"), "Save the document")
public sealed class Tooltip : Widget, ITickable
{
    private readonly Widget _child;
    private readonly Overlay _overlay;
    private float _hoverTimer;
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
    }

    // ── ITickable ─────────────────────────────────────────────────────────────

    public void Tick(float deltaSeconds)
    {
        bool hovered = IsDescendantHovered(_child);

        if (hovered && !_tooltipVisible)
        {
            _hoverTimer += deltaSeconds;
            if (_hoverTimer >= ShowDelay)
            {
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
        }
        else if (!hovered)
        {
            _hoverTimer = 0f;
            if (_tooltipVisible)
            {
                _tooltipVisible = false;
                _overlay.Close();
            }
        }
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

    private static bool IsDescendantHovered(Widget w)
    {
        if (w.IsHovered) return true;
        foreach (var child in w.Children)
            if (IsDescendantHovered(child)) return true;
        return false;
    }
}
