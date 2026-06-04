using System.Globalization;

namespace ApexUI.Widgets;

/// Numeric text field with decrement/increment buttons.
/// Layout: [−] [TextInput] [+] — TextInput fills remaining width.
public class NumberInput : Widget
{
    private const float BtnSpacing = 4f;

    // ── Properties ────────────────────────────────────────────────────────────

    public float Min
    {
        get;
        set { field = value; Value = _value; }   // re-clamp on range change
    } = float.NegativeInfinity;

    public float Max
    {
        get;
        set { field = value; Value = _value; }
    } = float.PositiveInfinity;

    public float Step { get; set; } = 1f;

    public string Format
    {
        get;
        set { field = value; SyncDisplay(); }
    } = "G";

    private float _value;
    private bool  _syncing;

    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, Min, Max);
            if (_value == clamped) return;
            _value = clamped;
            SyncDisplay();
            OnChanged?.Invoke(_value);
        }
    }

    public Action<float>? OnChanged;

    // ── Children ──────────────────────────────────────────────────────────────

    private readonly Button    _minus;
    private readonly TextInput _input;
    private readonly Button    _plus;

    // ── Constructor ───────────────────────────────────────────────────────────

    public NumberInput(float value = 0f)
    {
        _minus         = new Button("−").WithVariant(ButtonVariant.Secondary);
        _minus.Padding = new Thickness(10f, 6f);
        _minus.CornerRadius = 6f;

        _input = new TextInput().AsFloat();
        _input.MinWidth = 40f;
        _input.OnChanged = s =>
        {
            if (_syncing) return;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                var clamped = Math.Clamp(v, Min, Max);
                if (clamped == _value) return;
                _value = clamped;
                OnChanged?.Invoke(_value);
            }
        };
        _input.OnSubmit = _ => SyncDisplay();   // re-sync display to clamped value on Enter

        _plus         = new Button("+").WithVariant(ButtonVariant.Secondary);
        _plus.Padding = new Thickness(10f, 6f);
        _plus.CornerRadius = 6f;

        _minus.OnPressed = () => Value = _value - Step;
        _plus.OnPressed  = () => Value = _value + Step;

        AddChild(_minus);
        AddChild(_input);
        AddChild(_plus);

        _value = Math.Clamp(value, Min, Max);
        SyncDisplay();
    }

    // ── Fluent ────────────────────────────────────────────────────────────────

    public NumberInput WithMin(float min)        { Min    = min;  return this; }
    public NumberInput WithMax(float max)        { Max    = max;  return this; }
    public NumberInput WithStep(float step)      { Step   = step; return this; }
    public NumberInput WithFormat(string format) { Format = format; return this; }
    public NumberInput WithValue(float v)        { Value  = v;    return this; }
    public NumberInput OnChange(Action<float> a) { OnChanged = a; return this; }

    // ── Binding ───────────────────────────────────────────────────────────────

    public NumberInput Bind(Bindable<float> source)
    {
        Value           = source.Value;
        OnChanged      += v => source.Value = v;
        source.Changed += v => Value = v;
        return this;
    }

    public NumberInput BindInt(Bindable<int> source)
    {
        Step            = 1f;
        Format          = "0";
        Value           = source.Value;
        OnChanged      += v => source.Value = (int)MathF.Round(v);
        source.Changed += v => Value = v;
        return this;
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        _minus.Measure(new Size(float.PositiveInfinity, available.Height));
        _plus.Measure(new Size(float.PositiveInfinity, available.Height));
        float btnW   = MathF.Max(_minus.DesiredSize.Width, _plus.DesiredSize.Width);
        float inputW = MathF.Max(_input.MinWidth, available.Width - 2f * btnW - 2f * BtnSpacing);
        _input.Measure(new Size(inputW, available.Height));
        float maxH = MathF.Max(_input.DesiredSize.Height,
                     MathF.Max(_minus.DesiredSize.Height, _plus.DesiredSize.Height));
        return new Size(2f * btnW + 2f * BtnSpacing + _input.DesiredSize.Width, maxH);
    }

    protected override void ArrangeCore(Rect rect)
    {
        _minus.Measure(new Size(float.PositiveInfinity, rect.Height));
        _plus.Measure(new Size(float.PositiveInfinity, rect.Height));
        float btnW   = MathF.Max(_minus.DesiredSize.Width, _plus.DesiredSize.Width);
        float inputW = MathF.Max(_input.MinWidth, rect.Width - 2f * btnW - 2f * BtnSpacing);
        _input.Measure(new Size(inputW, rect.Height));

        _minus.Arrange(new Rect(rect.X,                        rect.Y, btnW,   rect.Height));
        _input.Arrange(new Rect(rect.X + btnW + BtnSpacing,    rect.Y, inputW, rect.Height));
        _plus.Arrange(new Rect(rect.Right - btnW,              rect.Y, btnW,   rect.Height));
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void SyncDisplay()
    {
        if (_input is null) return;
        _syncing      = true;
        _input.Value  = _value.ToString(Format, CultureInfo.InvariantCulture);
        _syncing      = false;
    }
}
