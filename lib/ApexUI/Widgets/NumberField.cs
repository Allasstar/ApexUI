using System.Globalization;

namespace ApexUI.Widgets;

/// A compact number field in the style of design tools: a caption and a text field.
/// Drag the caption sideways to scrub the value by Step per pixel (Shift: ten times faster),
/// or type a value and press Enter or move focus away. Escape restores the value from before typing.
/// ValueChanged fires on every change (each scrub step); EditCompleted fires once when a scrub or
/// typed edit that changed the value ends, which suits recording undo steps.
public class NumberField : Widget
{
    private const float FastFactor = 10f;

    private readonly Label _caption;
    private readonly TextInput _input;
    private float _value;
    private bool _scrubbing;
    private float _scrubStartX;
    private float _scrubStartValue;
    private float _valueBeforeEdit;

    public NumberField(string caption = "", float value = 0f)
    {
        _caption = new Label { Text = caption, ColorSource = t => t.OnSurfaceMuted, VAlign = VAlign.Center, Cursor = CursorShape.ResizeHorizontal };
        _input = new TextInput { Grow = 1f }.AsFloat();
        _input.MinWidth = 40f;
        _input.Submitted += _ => CommitText();
        _input.GotFocus += () => _valueBeforeEdit = _value;
        _input.LostFocus += CommitText;
        AddChild(new Row(_caption, _input) { SpacingSource = t => t.SpacingSm });

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

    public event Action<float>? ValueChanged;

    public event Action<float>? EditCompleted;

    public NumberField WithRange(float min, float max) { Min = min; Max = max; return this; }
    public NumberField WithStep(float step)            { Step = step; return this; }
    public NumberField WithFormat(string format)       { Format = format; return this; }
    public NumberField WithValue(float value)          { Value = value; return this; }
    public NumberField OnChange(Action<float> action)  { ValueChanged += action; return this; }
    public NumberField OnEditCompleted(Action<float> action) { EditCompleted += action; return this; }

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
        Value = _scrubStartValue + MathF.Round(e.X - _scrubStartX) * Step * factor;
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
        if (_value != _scrubStartValue) EditCompleted?.Invoke(_value);
    }

    // ── Typing ────────────────────────────────────────────────────────────────

    private void CommitText()
    {
        if (float.TryParse(_input.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float typed)) Value = typed;
        SyncText();
        if (_value != _valueBeforeEdit)
        {
            _valueBeforeEdit = _value;
            EditCompleted?.Invoke(_value);
        }
    }

    private void SyncText() => _input.Value = _value.ToString(Format, CultureInfo.InvariantCulture);
}
