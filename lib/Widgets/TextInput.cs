using System.Globalization;

namespace ApexUI.Widgets;

public enum InputMode { Any, Integer, Float }

public class TextInput : Widget, ITickable
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
    private int   _selAnchor;          // selection anchor; differs from _cursorPos when text is selected
    private float _blinkTimer;
    private bool  _cursorVisible = true;
    private bool  _isDragging;

    private int  SelectionStart => Math.Min(_cursorPos, _selAnchor);
    private int  SelectionEnd   => Math.Max(_cursorPos, _selAnchor);
    private bool HasSelection   => _cursorPos != _selAnchor;

    public TextInput(string placeholder = "")
    {
        Placeholder  = placeholder;
        Padding      = new Thickness(10f, 6f);
        CornerRadius = 6f;
        MinWidth     = 120f;
        OnKeyDown    = HandleKey;
        OnPointerDown = HandlePointerDown;
        OnPointerMove = HandlePointerMove;
        OnPointerUp   = _ => _isDragging = false;
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public TextInput WithPlaceholder(string ph)            { Placeholder = ph; return this; }
    public TextInput WithValue(string v)                   { Value = v; _cursorPos = _selAnchor = v.Length; return this; }
    public TextInput OnChange(Action<string> a)            { OnChanged = a; return this; }
    public TextInput WithSubmit(Action<string> a)          { OnSubmit = a; return this; }
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
        _cursorPos = _selAnchor = Value.Length;
        OnChanged      += s => { if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) source.Value = v; };
        source.Changed += v => { if (!IsFocused) { Value = v.ToString(format, CultureInfo.InvariantCulture); _cursorPos = _selAnchor = Value.Length; } };
        return this;
    }

    public TextInput BindInt(Bindable<int> source)
    {
        Value = source.Value.ToString();
        _cursorPos = _selAnchor = Value.Length;
        OnChanged      += s => { if (int.TryParse(s, out var v)) source.Value = v; };
        source.Changed += v => { if (!IsFocused) { Value = v.ToString(); _cursorPos = _selAnchor = Value.Length; } };
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

        ctx.FillRoundRect(LayoutBounds, CornerRadius, ctx.Theme.Background);
        ctx.StrokeRoundRect(LayoutBounds, CornerRadius, borderColor, IsFocused ? 2f : 1f);

        var    inner   = LayoutBounds.Deflate(Padding);
        string display = IsPassword ? new string('•', Value.Length) : Value;
        float  sz      = ctx.Theme.FontSizeBase;

        using (ctx.PushClip(inner))
        {
            if (IsFocused && HasSelection)
            {
                float selX0 = inner.X + (SelectionStart > 0 ? ctx.MeasureText(display[..SelectionStart], sz) : 0);
                float selX1 = inner.X + ctx.MeasureText(display[..SelectionEnd], sz);
                ctx.FillRect(new Rect(selX0, inner.Y, selX1 - selX0, inner.Height),
                             ctx.Theme.Primary.WithAlpha(0.3f));
            }

            if (!string.IsNullOrEmpty(display))
                ctx.DrawText(display, inner, ctx.Theme.OnSurface, sz);
            else if (!string.IsNullOrEmpty(Placeholder) && !IsFocused)
                ctx.DrawText(Placeholder, inner, ctx.Theme.OnSurfaceMuted, sz);

            if (IsFocused && _cursorVisible)
            {
                float cx = string.IsNullOrEmpty(display)
                    ? inner.X
                    : inner.X + ctx.MeasureText(display[.._cursorPos], sz);
                ctx.DrawLine(cx, inner.Y + 2, cx, inner.Bottom - 2, ctx.Theme.OnSurface, cap: SKStrokeCap.Butt);
            }
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

    // ── Pointer handling ──────────────────────────────────────────────────────

    private void HandlePointerDown(PointerEvent e)
    {
        if (e.Button != PointerButton.Left) return;
        int idx = HitTestCharIndex(e.X);
        _cursorPos = _selAnchor = idx;
        _isDragging = true;
        ResetBlink();
    }

    private void HandlePointerMove(PointerEvent e)
    {
        if (!_isDragging) return;
        int idx = HitTestCharIndex(e.X);
        if (idx == _cursorPos) return;
        _cursorPos = idx;
        ResetBlink();
    }

    // Maps a screen X coordinate to the nearest character index in Value.
    private int HitTestCharIndex(float screenX)
    {
        var inner = LayoutBounds.Deflate(Padding);
        float relX = screenX - inner.X;
        if (relX <= 0) return 0;

        string display = IsPassword ? new string('•', Value.Length) : Value;
        if (string.IsNullOrEmpty(display)) return 0;

        float sz     = Application.Current?.Theme.FontSizeBase ?? 14f;
        string family = Application.Current?.FontFamily ?? "Segoe UI";
        using var typeface = SKTypeface.FromFamilyName(family, SKFontStyle.Normal);
        using var font     = new SKFont(typeface, sz);

        float prev = 0f;
        for (int i = 1; i <= display.Length; i++)
        {
            float w = font.MeasureText(display[..i]);
            if (w >= relX)
                return (relX - prev < w - relX) ? i - 1 : i;
            prev = w;
        }
        return display.Length;
    }

    // ── Key handling ──────────────────────────────────────────────────────────

    private void HandleKey(KeyEvent e)
    {
        if (!e.IsDown) return;
        switch (e.Key)
        {
            case "Backspace":
                if (HasSelection) DeleteSelection();
                else if (_cursorPos > 0) { Value = Value.Remove(_cursorPos - 1, 1); _cursorPos--; _selAnchor = _cursorPos; }
                break;
            case "Delete":
                if (HasSelection) DeleteSelection();
                else if (_cursorPos < Value.Length) Value = Value.Remove(_cursorPos, 1);
                break;
            case "ArrowLeft":
                if (e.Shift)
                    _cursorPos = Math.Max(0, _cursorPos - 1);
                else if (HasSelection)
                    _cursorPos = _selAnchor = SelectionStart;
                else
                    _cursorPos = _selAnchor = Math.Max(0, _cursorPos - 1);
                Invalidate(); break;
            case "ArrowRight":
                if (e.Shift)
                    _cursorPos = Math.Min(Value.Length, _cursorPos + 1);
                else if (HasSelection)
                    _cursorPos = _selAnchor = SelectionEnd;
                else
                    _cursorPos = _selAnchor = Math.Min(Value.Length, _cursorPos + 1);
                Invalidate(); break;
            case "Home":
                if (e.Shift) _cursorPos = 0;
                else _cursorPos = _selAnchor = 0;
                Invalidate(); break;
            case "End":
                if (e.Shift) _cursorPos = Value.Length;
                else _cursorPos = _selAnchor = Value.Length;
                Invalidate(); break;
            case "Enter": OnSubmit?.Invoke(Value); break;
            default:
                if (e.Ctrl)
                {
                    if (string.Equals(e.Key, "a", StringComparison.OrdinalIgnoreCase))
                    { _selAnchor = 0; _cursorPos = Value.Length; Invalidate(); }
                    break;
                }
                if (e.Key.Length == 1 && IsCharAllowed(e.Key[0]))
                {
                    if (HasSelection) DeleteSelection();
                    Value = Value.Insert(_cursorPos, e.Key);
                    _cursorPos++;
                    _selAnchor = _cursorPos;
                }
                break;
        }
        ResetBlink();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void DeleteSelection()
    {
        int start = SelectionStart;
        Value      = Value.Remove(start, SelectionEnd - start);
        _cursorPos = _selAnchor = start;
    }

    private void ResetBlink()
    {
        _cursorVisible = true;
        _blinkTimer    = 0;
        Invalidate();
    }

    // ── Character filtering ───────────────────────────────────────────────────

    private bool IsCharAllowed(char c)
    {
        switch (InputMode)
        {
            case InputMode.Integer:
                if (!char.IsDigit(c))
                {
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
