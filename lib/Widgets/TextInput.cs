using System.Globalization;

namespace ApexUI.Widgets;

public enum InputMode { Any, Integer, Float }

public class TextInput : Widget
{
    public string Value
    {
        get;
        set { field = value; Invalidate(); OnChanged?.Invoke(value); }
    } = "";

    public string Placeholder
    {
        get;
        set { field = value; Invalidate(); }
    } = "";

    /// Restricts which characters can be typed. Does not re-validate existing Value.
    public InputMode InputMode  { get; set { field = value; Invalidate(); } } = InputMode.Any;

    /// Replaces visible characters with '•'. Value still holds the real text.
    public bool IsPassword      { get; set { field = value; Invalidate(); } } = false;

    /// When set, only characters present in this string are accepted. Applied after InputMode.
    public string? AllowedChars { get; set; }

    /// When set, characters present in this string are always rejected. Applied after InputMode.
    public string? BlockedChars { get; set; }

    /// Full-string predicate; false → red border. Does not block typing — use for feedback only.
    public Func<string, bool>? Validate { get; set; }

    public bool IsValid => Validate is null || Validate(Value);

    public bool IsFocused { get; internal set; }
    public Action<string>? OnChanged;
    public Action<string>? OnSubmit;   // fired on Enter

    private int   _cursorPos;
    private float _blinkTimer;
    private bool  _cursorVisible = true;

    public TextInput(string placeholder = "")
    {
        Placeholder  = placeholder;
        Padding      = new Thickness(10f, 6f);
        CornerRadius = 6f;
        MinWidth     = 120f;
        OnKeyDown    = HandleKey;
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public TextInput WithPlaceholder(string ph)            { Placeholder = ph; return this; }
    public TextInput WithValue(string v)                   { Value = v; _cursorPos = v.Length; return this; }
    public TextInput OnChange(Action<string> a)            { OnChanged = a; return this; }
    public TextInput AsPassword()                          { IsPassword = true; return this; }
    public TextInput AsInteger()                           { InputMode = InputMode.Integer; return this; }
    public TextInput AsFloat()                             { InputMode = InputMode.Float; return this; }
    public TextInput WithAllowedChars(string chars)        { AllowedChars = chars; return this; }
    public TextInput WithBlockedChars(string chars)        { BlockedChars = chars; return this; }
    public TextInput WithValidation(Func<string, bool> fn) { Validate = fn; return this; }

    // ── Binding ───────────────────────────────────────────────────────────────

    public TextInput BindFloat(Bindable<float> source, string format = "F2")
    {
        Value = source.Value.ToString(format, CultureInfo.InvariantCulture);
        _cursorPos = Value.Length;
        OnChanged      += s => { if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) source.Value = v; };
        // Skip update while focused so dragging the slider doesn't interrupt typing.
        source.Changed += v => { if (!IsFocused) { Value = v.ToString(format, CultureInfo.InvariantCulture); _cursorPos = Value.Length; } };
        return this;
    }

    public TextInput BindInt(Bindable<int> source)
    {
        Value = source.Value.ToString();
        _cursorPos = Value.Length;
        OnChanged      += s => { if (int.TryParse(s, out var v)) source.Value = v; };
        source.Changed += v => { if (!IsFocused) { Value = v.ToString(); _cursorPos = Value.Length; } };
        return this;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        float h = 14f * 1.4f + Padding.Vertical;
        float w = float.IsNaN(Width) ? Math.Max(MinWidth, available.Width) : Width;
        return new Size(w, h);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        bool valid = IsValid;
        var borderColor = IsFocused
            ? (valid ? ctx.Theme.Primary : ctx.Theme.Danger)
            : (valid ? ctx.Theme.Border  : ctx.Theme.Danger);

        using (var bg = ctx.MakePaint(ctx.Theme.Background))
            ctx.Canvas.DrawRoundRect(LayoutBounds.ToSKRect(), CornerRadius, CornerRadius, bg);

        using (var border = ctx.MakePaint(borderColor))
        {
            border.IsStroke    = true;
            border.StrokeWidth = IsFocused ? 2f : 1f;
            ctx.Canvas.DrawRoundRect(LayoutBounds.ToSKRect(), CornerRadius, CornerRadius, border);
        }

        var    inner   = LayoutBounds.Deflate(Padding);
        string display = IsPassword ? new string('•', Value.Length) : Value;

        using var font = ctx.MakeTextFont(ctx.Theme.FontSizeBase);
        var   m     = font.Metrics;
        float textY = inner.Y + (inner.Height - (m.Descent - m.Ascent)) * 0.5f - m.Ascent;

        if (!string.IsNullOrEmpty(display))
        {
            using var paint = ctx.MakeTextPaint(ctx.Theme.OnSurface);
            ctx.Canvas.DrawText(display, inner.X, textY, SKTextAlign.Left, font, paint);
        }
        else if (!string.IsNullOrEmpty(Placeholder) && !IsFocused)
        {
            using var paint = ctx.MakeTextPaint(ctx.Theme.OnSurfaceMuted);
            ctx.Canvas.DrawText(Placeholder, inner.X, textY, SKTextAlign.Left, font, paint);
        }

        // Cursor — drawn even on empty field so focus is always visible.
        if (IsFocused && _cursorVisible)
        {
            float cx = string.IsNullOrEmpty(display)
                ? inner.X
                : inner.X + font.MeasureText(display[.._cursorPos]);
            using var cursor = ctx.MakePaint(ctx.Theme.OnSurface);
            ctx.Canvas.DrawLine(cx, inner.Y + 2, cx, inner.Bottom - 2, cursor);
        }
    }

    // ── Tick (cursor blink) ───────────────────────────────────────────────────

    public void Tick(float deltaSeconds)
    {
        if (!IsFocused) return;
        _blinkTimer += deltaSeconds;
        if (_blinkTimer >= 0.53f)
        {
            _blinkTimer    = 0;
            _cursorVisible = !_cursorVisible;
            Invalidate();
        }
    }

    // ── Key handling ──────────────────────────────────────────────────────────

    private void HandleKey(KeyEvent e)
    {
        if (!e.IsDown) return;
        switch (e.Key)
        {
            case "Backspace":
                if (_cursorPos > 0) { Value = Value.Remove(_cursorPos - 1, 1); _cursorPos--; }
                break;
            case "Delete":
                if (_cursorPos < Value.Length) Value = Value.Remove(_cursorPos, 1);
                break;
            case "ArrowLeft":  _cursorPos = Math.Max(0, _cursorPos - 1);           Invalidate(); break;
            case "ArrowRight": _cursorPos = Math.Min(Value.Length, _cursorPos + 1); Invalidate(); break;
            case "Home":       _cursorPos = 0;            Invalidate(); break;
            case "End":        _cursorPos = Value.Length; Invalidate(); break;
            case "Enter":      OnSubmit?.Invoke(Value);   break;
            default:
                if (e.Key.Length == 1 && IsCharAllowed(e.Key[0]))
                {
                    Value = Value.Insert(_cursorPos, e.Key);
                    _cursorPos++;
                }
                break;
        }
    }

    // ── Character filtering ───────────────────────────────────────────────────

    private bool IsCharAllowed(char c)
    {
        switch (InputMode)
        {
            case InputMode.Integer:
                if (!char.IsDigit(c))
                {
                    // Allow '-' only at position 0 and only if none exists yet
                    if (c == '-' && _cursorPos == 0 && !Value.Contains('-')) break;
                    return false;
                }
                break;

            case InputMode.Float:
                if (!char.IsDigit(c))
                {
                    if (c == '-' && _cursorPos == 0 && !Value.Contains('-')) break;
                    if (c == '.' && !Value.Contains('.'))                     break;
                    return false;
                }
                break;
        }

        if (AllowedChars is not null && !AllowedChars.Contains(c)) return false;
        if (BlockedChars is not null &&  BlockedChars.Contains(c)) return false;
        return true;
    }
}
