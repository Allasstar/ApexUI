namespace ApexUI.Widgets;

/// What a hint's chip stands for: a keyboard key (a rounded square) or a mouse action (a capsule).
public enum KeyHintKind { Key, Mouse }

/// A status bar hint: a key or mouse chip followed by what it does, as in "[Shift] Add" or "(Drag) Pan".
/// Key, Text and Kind can be changed on a live hint, so a bar can keep a fixed set of hints and refill them;
/// IsHighlighted only redraws, for a hint whose modifier is being held.
/// Theme tokens: KeyHintFontSize (text and chip), KeyHintText, KeyHintBorder; Primary and OnPrimary when highlighted.
///
/// Usage:
///   var bar = new Row(new KeyHint("LMB", "Select", KeyHintKind.Mouse), new KeyHint("Shift", "Add"));
///   shiftHint.IsHighlighted = true;
public sealed class KeyHint : LinearPanel
{
    private static readonly Func<Theme, float> HintFontSize = t => t.KeyHintFontSize;

    private readonly Chip _chip;
    private readonly Label _label;

    public KeyHint(string key = "", string text = "", KeyHintKind kind = KeyHintKind.Key) : base(Orientation.Horizontal)
    {
        _chip  = new Chip(this) { VAlign = VAlign.Center };
        _label = new Label
        {
            FontSizeSource = HintFontSize,
            VAlign         = VAlign.Center,
            ColorSource    = t => IsHighlighted ? t.OnSurface : t.KeyHintText,
        };
        AddChild(_chip);
        AddChild(_label);
        Key  = key;
        Text = text;
        Kind = kind;
        IsHitTestVisible = false;
    }

    public string Key
    {
        get => _chip.Text.Text;
        set => _chip.Text.Text = value;
    }

    public string Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public KeyHintKind Kind { get; set => SetRenderProperty(ref field, value); }

    /// Draws the chip in Primary and the text in OnSurface instead of KeyHintText.
    public bool IsHighlighted
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _chip.Invalidate();
            _label.Invalidate();
        }
    }

    /// Sets everything a bar refills at once.
    public void Set(string key, string text, KeyHintKind kind, bool highlighted)
    {
        Key           = key;
        Text          = text;
        Kind          = kind;
        IsHighlighted = highlighted;
    }

    protected override float DefaultSpacing(Theme theme) => theme.SpacingXs;

    private sealed class Chip : Widget
    {
        private readonly KeyHint _owner;

        public Chip(KeyHint owner)
        {
            _owner = owner;
            Text = new Label
            {
                FontSizeSource = HintFontSize,
                HAlign         = HAlign.Center,
                VAlign         = VAlign.Center,
                ColorSource    = t => owner.IsHighlighted ? t.OnPrimary : t.OnSurface,
            };
            AddChild(Text);
        }

        public Label Text { get; }

        // Half the font size at the sides: 6 at the default small font, as before the token, and in step with it.
        protected override Thickness DefaultPadding(Theme theme) => new(theme.KeyHintFontSize * 0.5f, 0f);

        protected override void DrawCore(DrawContext ctx)
        {
            var   t      = ctx.Theme;
            var   b      = LayoutBounds;
            float radius = _owner.Kind == KeyHintKind.Mouse ? b.Height * 0.5f : t.CornerRadiusSmall;
            if (_owner.IsHighlighted)
            {
                ctx.FillRoundRect(b, radius, t.Primary);
                return;
            }

            ctx.FillRoundRect(b, radius, t.Surface);
            ctx.StrokeRoundRect(b, radius, t.KeyHintBorder);
        }
    }
}
