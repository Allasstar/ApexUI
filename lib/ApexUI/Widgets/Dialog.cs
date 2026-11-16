namespace ApexUI.Widgets;

/// Modal dialog with a title bar, content area, and a configurable row of buttons.
/// Use the static factories for common patterns, or build a custom dialog with the fluent API.
///
/// Example — alert:
///   Dialog.Alert("Error", "File not found.").Show(host);
///
/// Example — custom:
///   var dlg = new Dialog("Rename", new TextInput("Enter name…"))
///       .AddButton("Cancel", () => dlg.Close())
///       .AddPrimaryButton("OK", () => { Rename(); dlg.Close(); });
///   dlg.Show(host);
public sealed class Dialog
{
    private readonly Overlay _overlay;
    private readonly Label _titleLabel;
    private readonly List<(string Text, Action Action, ButtonVariant Variant)> _buttonSpecs = [];
    private readonly Widget _content;
    private bool _built;

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    public Action? OnClosed;

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
        _buttonSpecs.Add((text, action, variant));
        return this;
    }

    public Dialog AddPrimaryButton(string text, Action action)
        => AddButton(text, action, ButtonVariant.Primary);

    // ── Show / Close ──────────────────────────────────────────────────────────

    /// Shows the dialog over host's window. Inside a widget, pass its Host.
    public void Show(UiHost host)
    {
        if (!_built) BuildPanel();
        _overlay.Open(host);
    }

    public void Close()
    {
        _overlay.Close();
        OnClosed?.Invoke();
    }

    // ── Static factories ──────────────────────────────────────────────────────

    public static Dialog Alert(string title, string message, string btnText = "OK", Action? onClose = null)
    {
        Dialog? dlg = null;
        dlg = new Dialog(title, new Label { Text = message });
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
        dlg = new Dialog(title, new Label { Text = message });
        dlg.AddButton(cancelText,  () => { dlg!.Close(); onCancel?.Invoke(); });
        dlg.AddPrimaryButton(confirmText, () => { dlg!.Close(); onConfirm?.Invoke(); });
        return dlg;
    }

    // ── Panel construction ────────────────────────────────────────────────────

    private void BuildPanel()
    {
        _built = true;

        var buttons = _buttonSpecs
            .Select(s => (Widget)new Button(s.Text, s.Action).WithVariant(s.Variant))
            .ToArray();

        var rows = new List<Widget>
        {
            _titleLabel,
            new DialogSeparator(),
            _content,
        };

        if (buttons.Length > 0)
        {
            rows.Add(new DialogSeparator());
            rows.Add(new ButtonRow(buttons) { HAlign = HAlign.Right });
        }

        _overlay.Content = new Panel(new Column(rows.ToArray()));
    }

    private sealed class Panel : Widget
    {
        public Panel(Widget content)
        {
            BackgroundSource = t => t.Surface;
            MinWidth         = 320f;
            MaxWidth         = 520f;
            AddChild(content);
        }

        protected override Thickness DefaultPadding(Theme theme) => theme.DialogPadding;
        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusDialog;
    }

    private sealed class DialogSeparator : Separator
    {
        protected override Size MeasureCore(Size available)
            => base.MeasureCore(available) with { Height = 1f + Theme.SpacingSm };
    }

    private sealed class ButtonRow : Row
    {
        public ButtonRow(Widget[] buttons) : base(buttons) { }

        protected override float DefaultSpacing(Theme theme) => theme.SpacingSm;
    }
}
