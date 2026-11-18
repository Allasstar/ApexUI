namespace ApexUI.Widgets;

/// Which window edge a Toast sits at.
public enum ToastPlacement { Top, Bottom }

/// A short message that floats at the top or bottom center of the window and hides itself after Duration.
/// It never takes pointer input. Showing a new message while one is up replaces it and restarts the timer.
///
/// Usage:
///   var toast = new Toast();
///   toast.Show(host, "Saved");
///   var notice = new Toast { Placement = ToastPlacement.Top, EdgeOffset = 56 };
public sealed class Toast : Overlay
{
    private readonly Label _message;
    private readonly Action _hide;
    private IDisposable? _timer;

    public Toast()
    {
        _message = new Label { IsHitTestVisible = false };
        Content = new PopupPanel(_message, t => t.PopupPadding, t => t.CornerRadiusPopup);
        ContentHAlign = HAlign.Center;
        ContentVAlign = VAlign.Bottom;
        DismissOnClickOutside = false;
        IsHitTestVisible = false;
        _hide = Hide;
    }

    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(2.5);

    public ToastPlacement Placement
    {
        get;
        set
        {
            field = value;
            ContentVAlign = value == ToastPlacement.Top ? VAlign.Top : VAlign.Bottom;
        }
    } = ToastPlacement.Bottom;

    /// The gap between the message and its window edge, for example to clear a toolbar at the top.
    /// Null follows the theme's SpacingLg.
    public float? EdgeOffset { get; set => SetLayoutProperty(ref field, value); }

    public string Message => _message.Text;

    public void Show(UiHost host, string message)
    {
        _message.Text = message;
        _timer?.Dispose();
        _timer = host.Platform.StartTimer(Duration, _hide);
        Open(host);
    }

    public void Hide()
    {
        _timer?.Dispose();
        _timer = null;
        if (IsVisible) Close();
    }

    // Moves the message off its window edge, where Overlay would place it.
    protected override void ArrangeCore(Rect screen)
    {
        float offset = Math.Min(EdgeOffset ?? Theme.SpacingLg, screen.Height);
        float height = screen.Height - offset;
        base.ArrangeCore(Placement == ToastPlacement.Top
            ? screen with { Y = screen.Y + offset, Height = height }
            : screen with { Height = height });
    }

    protected override void OnDetached()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
