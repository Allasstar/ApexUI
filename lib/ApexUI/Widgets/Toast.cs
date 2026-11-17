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
        Content = new Panel(_message);
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

    protected override void OnDetached()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private sealed class Panel : Widget
    {
        public Panel(Widget content)
        {
            BackgroundSource = t => t.Surface;
            AddChild(content);
        }

        protected override Thickness DefaultPadding(Theme theme) => theme.PopupPadding;

        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusPopup;

        // Overlay places content flush with the window edge; the margin lifts the toast off it.
        protected override void OnAttached(UiHost host) => Margin = new Thickness(0, 0, 0, host.Theme.SpacingLg);
    }
}
