using System.Globalization;

namespace ApexUI.Widgets;

/// A compact number field in the style of design tools: a caption and a text field.
/// Drag the caption sideways to scrub the value by Step per pixel (Shift: ten times faster),
/// or type a value and press Enter or move focus away. Escape restores the value from before typing.
/// ValueChanged fires on every change (each scrub step); EditCompleted fires once when a scrub or
/// typed edit that changed the value ends, which suits recording undo steps. Removing the field from the
/// tree ends its scrub or typed edit the same way, so a handler that rebuilds its panel on ValueChanged
/// must keep the field it is editing, or EditCompleted arrives in the middle of the rebuild.
///
/// Units (optional): WithUnits("px", "%") shows the current unit inside the field at its right end, on a small chip
/// as wide as the widest unit so the text never shifts. Typing a value with a
/// unit suffix ("50%", "120 px") switches to that unit; clicking the unit button cycles to the next one,
/// converting the value through ConvertUnit when set. A unit change raises UnitChanged before the
/// ValueChanged of the same edit, and the edit ends with EditCompleted.
///
/// IsMixed shows MixedText instead of a number, for a field that stands for several differing values.
/// A typed or scrubbed edit clears it before raising ValueChanged, so a handler can ignore changes while
/// IsMixed is set (a unit switch converts the hidden value but leaves the field mixed).
public class NumberField : Widget
{
    private const float FastFactor = 10f;

    private readonly Label _caption;
    private readonly TextInput _input;
    private readonly UnitButton _unitButton;
    private float _value;
    private bool _scrubbing;
    private float _scrubStartX;
    private float _scrubStartValue;
    private float _valueBeforeEdit;
    private string _unitBeforeEdit = "";
    private bool _mixedBeforeEdit;

    public NumberField(string caption = "", float value = 0f)
    {
        _caption = new Label { Text = caption, ColorSource = t => t.OnSurfaceMuted, VAlign = VAlign.Center, Cursor = CursorShape.ResizeHorizontal };
        _input = new TextInput { Grow = 1f }.AsFloat();
        _input.MinWidth = 40f;
        _input.Submitted += _ => CommitText();
        _input.GotFocus += () =>
        {
            _valueBeforeEdit = _value;
            _unitBeforeEdit = Unit;
            _mixedBeforeEdit = IsMixed;
        };
        _input.LostFocus += CommitText;
        _input.Placeholder = "Mixed";
        _unitButton = new UnitButton { IsVisible = false };
        AddChild(new Row(_caption, new InputBox(_input, _unitButton)) { SpacingSource = t => t.SpacingSm });

        _value = value;
        SyncText();
    }

    public float Value
    {
        get => _value;
        set
        {
            float clamped = Math.Clamp(value, Min, Max);
            if (_value == clamped) return;
            _value = clamped;
            SyncText();
            ValueChanged?.Invoke(clamped);
        }
    }

    public float Min { get; set { field = value; Value = _value; } } = float.NegativeInfinity;

    public float Max { get; set { field = value; Value = _value; } } = float.PositiveInfinity;

    /// Change per pixel of scrubbing.
    public float Step { get; set; } = 1f;

    /// .NET numeric format for the text, e.g. "0" or "0.##".
    public string Format { get; set { field = value; SyncText(); } } = "0.##";

    public string Caption
    {
        get => _caption.Text;
        set => _caption.Text = value;
    }

    public bool IsScrubbing => _scrubbing;

    /// The units the value can be in; empty (the default) shows no unit.
    public IReadOnlyList<string> Units
    {
        get;
        set
        {
            field = value;
            _input.InputMode = value.Count > 0 ? InputMode.Any : InputMode.Float;
            _unitButton.IsVisible = value.Count > 0;
            _unitButton.Units = value;
            if (IndexOfUnit(Unit) < 0) Unit = value.Count > 0 ? value[0] : "";
        }
    } = [];

    /// One of Units. Setting it does not convert the value.
    public string Unit
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _unitButton.Text = value;
            UnitChanged?.Invoke(value);
        }
    } = "";

    /// (value, from, to) → the same quantity in the unit `to`; used when the unit label is clicked.
    public Func<float, string, string, float>? ConvertUnit { get; set; }

    public bool IsMixed
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            SyncText();
        }
    }

    public string MixedText
    {
        get => _input.Placeholder;
        set => _input.Placeholder = value;
    }

    public event Action<float>? ValueChanged;

    public event Action<float>? EditCompleted;

    public event Action<string>? UnitChanged;

    public NumberField WithRange(float min, float max) { Min = min; Max = max; return this; }
    public NumberField WithStep(float step)            { Step = step; return this; }
    public NumberField WithFormat(string format)       { Format = format; return this; }
    public NumberField WithValue(float value)          { Value = value; return this; }
    public NumberField OnChange(Action<float> action)  { ValueChanged += action; return this; }
    public NumberField OnEditCompleted(Action<float> action) { EditCompleted += action; return this; }
    public NumberField WithUnits(params string[] units)      { Units = units; return this; }
    public NumberField WithUnitConversion(Func<float, string, string, float> convert) { ConvertUnit = convert; return this; }
    public NumberField OnUnitChange(Action<string> action)   { UnitChanged += action; return this; }

    public NumberField Bind(Bindable<float> source)
    {
        Value = source.Value;
        ValueChanged += v => source.Value = v;
        Observe(source, v => Value = v);
        return this;
    }

    // Asks for the caption and the field's MinWidth only, so two fields share a row; Grow or Width widens it.
    protected override Size MeasureCore(Size available)
        => base.MeasureCore(new Size(float.PositiveInfinity, available.Height));

    // ── Scrubbing ─────────────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Source == _unitButton && e.Button == PointerButton.Left && Units.Count > 1)
        {
            e.Handled = true;
            CycleUnit();
            return;
        }

        if (e.Source != _caption || e.Button != PointerButton.Left || !_caption.CapturePointer()) return;
        e.Handled = true;
        _scrubbing = true;
        _scrubStartX = e.X;
        _scrubStartValue = _value;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_scrubbing) return;
        e.Handled = true;
        float factor = e.Shift ? FastFactor : 1f;
        float next = _scrubStartValue + MathF.Round(e.X - _scrubStartX) * Step * factor;
        if (next == _value) return;
        IsMixed = false;
        Value = next;
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (!_scrubbing) return;
        e.Handled = true;
        EndScrub();
    }

    protected override void OnDetached()
    {
        if (_scrubbing) EndScrub();
    }

    private void EndScrub()
    {
        _scrubbing = false;
        if (_value == _scrubStartValue) return;
        EditCompleted?.Invoke(_value);
    }

    // ── Units ─────────────────────────────────────────────────────────────────

    private void CycleUnit()
    {
        string from = Unit;
        string to = Units[(IndexOfUnit(from) + 1) % Units.Count];
        float converted = ConvertUnit?.Invoke(_value, from, to) ?? _value;
        Unit = to;
        Value = converted;
        SyncText();
        EditCompleted?.Invoke(_value);
    }

    private int IndexOfUnit(string unit)
    {
        IReadOnlyList<string> units = Units;
        for (int i = 0; i < units.Count; i++)
        {
            if (string.Equals(units[i], unit, StringComparison.OrdinalIgnoreCase)) return i;
        }

        return -1;
    }

    // "50%", "120 px", "-3.5": the number, and the unit after it (null when there is none).
    private bool TryParse(string text, out float value, out string? unit)
    {
        unit = null;
        string trimmed = text.Trim();
        int end = trimmed.Length;
        while (end > 0 && !char.IsDigit(trimmed[end - 1]) && trimmed[end - 1] != '.') end--;

        if (end < trimmed.Length)
        {
            int index = IndexOfUnit(trimmed[end..].Trim());
            if (index < 0)
            {
                value = 0f;
                return false;
            }

            unit = Units[index];
        }

        return float.TryParse(trimmed[..end], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    // ── Typing ────────────────────────────────────────────────────────────────

    private void CommitText()
    {
        if (TryParse(_input.Value, out float typed, out string? unit))
        {
            IsMixed = false;
            if (unit is not null) Unit = unit;
            Value = typed;
        }

        SyncText();
        if (_value != _valueBeforeEdit || Unit != _unitBeforeEdit || IsMixed != _mixedBeforeEdit)
        {
            _valueBeforeEdit = _value;
            _unitBeforeEdit = Unit;
            _mixedBeforeEdit = IsMixed;
            EditCompleted?.Invoke(_value);
        }
    }

    private void SyncText() => _input.Value = IsMixed ? "" : _value.ToString(Format, CultureInfo.InvariantCulture);

    // The text field with the unit chip laid over its right end; the field keeps the chip's room free of text.
    private sealed class InputBox : Widget
    {
        private const float MinTextWidth = 24f;

        private readonly TextInput _input;
        private readonly UnitButton _unit;

        public InputBox(TextInput input, UnitButton unit)
        {
            _input = input;
            _unit = unit;
            Grow = 1f;
            AddChild(input);
            AddChild(unit);
        }

        private float ChipInset => Theme.SpacingXs;

        protected override Size MeasureCore(Size available)
        {
            _input.Measure(available);
            Size size = _input.DesiredSize;
            if (!_unit.IsVisible) return size;
            _unit.Measure(available);
            return new Size(Math.Max(size.Width, _unit.DesiredSize.Width + ChipInset * 2f + MinTextWidth), size.Height);
        }

        protected override void ArrangeCore(Rect r)
        {
            if (!_unit.IsVisible)
            {
                _input.TrailingInset = 0f;
                _input.Arrange(r);
                return;
            }

            float margin = ChipInset;
            float width = _unit.DesiredSize.Width;
            float chipLeft = r.Right - margin - width;
            _input.TrailingInset = Math.Max(0f, r.Right - _input.Padding.Right - (chipLeft - margin));
            _input.Arrange(r);
            _unit.Arrange(new Rect(chipLeft, r.Y + margin, width, Math.Max(0f, r.Height - margin * 2f)));
        }
    }

    // The unit inside the field, in the small font. With more than one unit it is a chip with a swap mark,
    // since a click cycles them.
    private sealed class UnitButton : Widget
    {
        private const float MarkWidth = 7f;

        private readonly Label _label = new() { Role = TextRole.Small, HAlign = HAlign.Center, VAlign = VAlign.Center, IsHitTestVisible = false };

        public UnitButton()
        {
            _label.ColorSource = t => !Cycles || !IsEnabledInTree ? t.OnSurfaceMuted : IsHovered || IsPressed ? t.OnSurface : t.OnSurfaceMuted;
            AddChild(_label);
        }

        public string Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        public IReadOnlyList<string> Units
        {
            get;
            set
            {
                SetLayoutProperty(ref field, value);
                Cursor = Cycles ? CursorShape.Hand : null;
            }
        } = [];

        private bool Cycles => Units.Count > 1;

        protected override Thickness DefaultPadding(Theme theme)
        {
            float side = theme.SpacingXs * 1.5f;
            return new Thickness(side, 0f, Cycles ? side + MarkWidth : side, 0f);
        }

        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

        // As wide as the widest unit, so switching units never moves the text.
        protected override Size MeasureCore(Size available)
        {
            Size label = base.MeasureCore(available);
            float fontSize = Theme.FontSizeSmall;
            using var font = LayoutFont.Get(Host, fontSize);
            float width = label.Width;
            foreach (string unit in Units) width = Math.Max(width, font.Font.MeasureText(unit) + 2f);
            return new Size(width, label.Height);
        }

        protected override void DrawCore(DrawContext ctx)
        {
            if (!Cycles) return;
            var t = ctx.Theme;
            bool enabled = IsEnabledInTree;
            var b = LayoutBounds;
            if (enabled && IsPressed) ctx.FillRoundRect(b, CornerRadius, t.Primary.WithAlpha(0.28f));
            else if (enabled && IsHovered) ctx.FillRoundRect(b, CornerRadius, t.Primary.WithAlpha(0.16f));
            else ctx.FillRoundRect(b, CornerRadius, t.OnSurface.WithAlpha(0.07f));

            // Two small chevrons, up over down: the chip switches between values.
            var mark = !enabled ? t.OnSurfaceMuted.WithAlpha(0.5f) : IsHovered || IsPressed ? t.Primary : t.OnSurfaceMuted;
            float cx = ContentBounds.Right + MarkWidth * 0.5f;
            float cy = b.CenterY;
            ctx.DrawChevron(cx, cy - 2.5f, 2.25f, pointUp: true, mark, 1.25f);
            ctx.DrawChevron(cx, cy + 2.5f, 2.25f, pointUp: false, mark, 1.25f);
        }
    }
}
