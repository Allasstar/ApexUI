namespace ApexUI.Widgets;

/// A short message that floats at the bottom center of the window and hides itself after Duration.
/// It never takes pointer input. Showing a new message while one is up replaces it and restarts the timer.
///
/// Usage:
///   var toast = new Toast();
///   toast.Show(host, "Saved");
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

    // Lifts the message off the window's bottom edge, where Overlay would place it.
    protected override void ArrangeCore(Rect screen)
        => base.ArrangeCore(screen with { Height = Math.Max(0f, screen.Height - Theme.SpacingLg) });

    protected override void OnDetached()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
