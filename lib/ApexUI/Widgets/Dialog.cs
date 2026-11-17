namespace ApexUI.Widgets;

/// Modal dialog with a title bar, content area, and a configurable row of buttons.
/// Use the static factories for common patterns, or build a custom dialog with the fluent API.
///
/// Keyboard: when shown, focus goes to InitialFocus or else the primary button. Enter runs the primary
/// button (unless a focused widget uses Enter itself) and Escape runs the cancel button, or just closes
/// the dialog when there is none.
///
/// Example — alert:
///   Dialog.Alert("Error", "File not found.").Show(host);
///
/// Example — custom:
///   var dlg = new Dialog("Rename", nameInput) { InitialFocus = nameInput };
///   dlg.AddCancelButton("Cancel", () => dlg.Close())
///      .AddPrimaryButton("OK", () => { Rename(); dlg.Close(); });
///   dlg.Show(host);
public sealed class Dialog
{
    private readonly Overlay _overlay;
    private readonly Label _titleLabel;
    private readonly List<ButtonSpec> _buttonSpecs = [];
    private readonly Widget _content;
    private Button? _primary;
    private Action? _primaryAction;
    private Action? _cancel;
    private Action<KeyEventArgs>? _onUnhandledKey;
    private UiHost? _host;
    private bool _built;

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    /// The widget focused when the dialog opens, for example a text field. Default: the primary button.
    public Widget? InitialFocus { get; set; }

    public bool IsOpen => _overlay.IsVisible;

    public event Action? Closed;

    public Dialog(string title, Widget content)
    {
        _content = content;
        _titleLabel = new Label { Text = title, Bold = true, Role = TextRole.Subheading };

        _overlay = new Overlay
        {
            IsModal               = true,
            DismissOnClickOutside = false,
            ContentHAlign         = HAlign.Center,
            ContentVAlign         = VAlign.Center,
        };
    }

    // ── Fluent button builder ─────────────────────────────────────────────────

    public Dialog AddButton(string text, Action action, ButtonVariant variant = ButtonVariant.Secondary)
    {
        _buttonSpecs.Add(new ButtonSpec(text, action, variant, IsCancel: false));
        return this;
    }

    public Dialog AddPrimaryButton(string text, Action action)
        => AddButton(text, action, ButtonVariant.Primary);

    /// A secondary button that Escape also presses.
    public Dialog AddCancelButton(string text, Action action)
    {
        _buttonSpecs.Add(new ButtonSpec(text, action, ButtonVariant.Secondary, IsCancel: true));
        return this;
    }

    // ── Show / Close ──────────────────────────────────────────────────────────

    /// Shows the dialog over host's window. Inside a widget, pass its Host.
    public void Show(UiHost host)
    {
        if (!_built) BuildPanel();
        _overlay.Open(host);
        // Keys also arrive here when nothing in the dialog has focus (after a click on its background).
        _onUnhandledKey ??= OnPanelKey;
        if (_host != host)
        {
            if (_host is not null) _host.UnhandledKeyDown -= _onUnhandledKey;
            _host = host;
            host.UnhandledKeyDown += _onUnhandledKey;
        }
        (InitialFocus ?? _primary)?.Focus();
    }

    public void Close()
    {
        if (!_overlay.IsVisible) return;
        _overlay.Close();
        if (_host is not null)
        {
            _host.UnhandledKeyDown -= _onUnhandledKey;
            _host = null;
        }
        Closed?.Invoke();
    }

    // ── Static factories ──────────────────────────────────────────────────────

    public static Dialog Alert(string title, string message, string btnText = "OK", Action? onClose = null)
    {
        Dialog? dlg = null;
        dlg = new Dialog(title, Message(message));
        dlg.AddPrimaryButton(btnText, () => { dlg!.Close(); onClose?.Invoke(); });
        return dlg;
    }

    public static Dialog Confirm(
        string title,
        string message,
        Action? onConfirm  = null,
        Action? onCancel   = null,
        string confirmText = "OK",
        string cancelText  = "Cancel")
    {
        Dialog? dlg = null;
        dlg = new Dialog(title, Message(message));
        dlg.AddCancelButton(cancelText, () => { dlg!.Close(); onCancel?.Invoke(); });
        dlg.AddPrimaryButton(confirmText, () => { dlg!.Close(); onConfirm?.Invoke(); });
        return dlg;
    }

    private static Label Message(string text) => new() { Text = text, WordWrap = true };

    // ── Panel construction ────────────────────────────────────────────────────

    private void BuildPanel()
    {
        _built = true;

        var buttons = new Widget[_buttonSpecs.Count];
        for (int i = 0; i < buttons.Length; i++)
        {
            var spec   = _buttonSpecs[i];
            var button = new Button(spec.Text, spec.Action).WithVariant(spec.Variant);
            buttons[i] = button;
            if (spec.Variant == ButtonVariant.Primary && _primary is null)
            {
                _primary       = button;
                _primaryAction = spec.Action;
            }
            if (spec.IsCancel) _cancel ??= spec.Action;
        }

        var column = new Column(_titleLabel, new DialogSeparator(), _content);
        if (buttons.Length > 0)
        {
            column.Add(new DialogSeparator());
            column.Add(new ButtonRow(buttons) { HAlign = HAlign.Right });
        }

        _overlay.Content = new Panel(column, OnPanelKey);
    }

    private void OnPanelKey(KeyEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None || e.IsRepeat) return;

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (_cancel is not null) _cancel();
            else Close();
        }
        else if (e.Key is Key.Enter or Key.KeypadEnter && _primary is { IsEnabled: true })
        {
            e.Handled = true;
            _primaryAction!();
        }
    }

    private readonly record struct ButtonSpec(string Text, Action Action, ButtonVariant Variant, bool IsCancel);

    // Keys from a focused button or field bubble here unless that widget handled them.
    private sealed class Panel : Widget
    {
        private readonly Action<KeyEventArgs> _onKey;

        public Panel(Widget content, Action<KeyEventArgs> onKey)
        {
            _onKey           = onKey;
            BackgroundSource = t => t.Surface;
            MinWidth         = 320f;
            MaxWidth         = 520f;
            AddChild(content);
        }

        protected override Thickness DefaultPadding(Theme theme) => theme.DialogPadding;
        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusDialog;

        protected override void OnKeyDown(KeyEventArgs e) => _onKey(e);
    }

    private sealed class DialogSeparator : Separator
    {
        protected override Thickness DefaultPadding(Theme theme) => new(0f, theme.SpacingXs);
    }

    private sealed class ButtonRow : Row
    {
        public ButtonRow(Widget[] buttons) : base(buttons) { }

        protected override float DefaultSpacing(Theme theme) => theme.SpacingSm;
    }
}
