namespace ApexUI.Widgets;

public enum TooltipPosition { Below, Above, Right, Left }

/// Transparent decorator that shows a floating tooltip after a hover delay.
/// Wrap any widget: new Tooltip(new Button("Save"), "Save the document")
public sealed class Tooltip : Widget
{
    private readonly Widget _child;
    private readonly Overlay _overlay;
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
            // Never under the pointer, so showing the tooltip does not end the hover that opened it.
            IsHitTestVisible = false,
        };
        _overlay.Content = new Panel(label);

        _show = Show;
    }

    protected override void OnDetached()
    {
        Hide();
        _overlay.Detach();
    }

    // ── Hover ─────────────────────────────────────────────────────────────────
    // The child is this widget's only child, so the pointer is over the child exactly when it is over this.

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        if (_tooltipVisible || _delay is not null) return;
        if (ShowDelay > 0f)
            _delay = Host!.Platform.StartTimer(TimeSpan.FromSeconds(ShowDelay), _show);
        else
            Show();
    }

    protected override void OnPointerExited(PointerEventArgs e) => Hide();

    protected override void OnPointerPressed(PointerEventArgs e) => Hide();

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

    private sealed class Panel : Widget
    {
        public Panel(Widget content)
        {
            BackgroundSource = t => t.Surface;
            AddChild(content);
        }

        protected override Thickness DefaultPadding(Theme theme) => theme.TooltipPadding;
        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;
    }

    // ── Transparent layout ────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        _child.Measure(available);
        return _child.DesiredSize;
    }

    protected override void ArrangeCore(Rect r)
        => _child.Arrange(r);
}
