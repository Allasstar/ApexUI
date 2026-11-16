using System.Globalization;

namespace ApexUI.Widgets;

public enum InputMode { Any, Integer, Float }

public class TextInput : Widget
{
    public string Value
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            // Text can shrink under the caret when set from code (e.g. re-formatting on submit).
            _cursorPos = Math.Min(_cursorPos, value.Length);
            _selAnchor = Math.Min(_selAnchor, value.Length);
            Invalidate();
            OnChanged?.Invoke(value);
        }
    } = "";

    public string Placeholder
    {
        get;
        set => SetRenderProperty(ref field, value);
    } = "";

    /// Restricts which characters can be typed. Does not re-validate existing Value.
    public InputMode InputMode  { get; set => SetRenderProperty(ref field, value); } = InputMode.Any;

    /// Replaces visible characters with '•'. Value still holds the real text.
    public bool IsPassword      { get; set => SetRenderProperty(ref field, value); } = false;

    /// When set, only characters present in this string are accepted. Applied after InputMode.
    public string? AllowedChars { get; set; }

    /// When set, characters present in this string are always rejected. Applied after InputMode.
    public string? BlockedChars { get; set; }

    /// Full-string predicate; false → red border. Does not block typing — use for feedback only.
    public Func<string, bool>? Validate { get; set; }

    public bool IsValid => Validate is null || Validate(Value);

    public bool IsFocused
    {
        get;
        internal set
        {
            if (field == value) return;
            field = value;
            if (value) ResetBlink();
            else StopBlink();
        }
    }
    public Action<string>? OnChanged;
    public Action<string>? OnSubmit;   // fired on Enter

    private int   _cursorPos;
    private int   _selAnchor;          // selection anchor; differs from _cursorPos when text is selected
    private static readonly TimeSpan BlinkInterval = TimeSpan.FromSeconds(0.53);
    private readonly Action _toggleCaret;
    private IDisposable? _blinkTimer;
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
        OnTextInput  = HandleText;
        OnPointerDown = HandlePointerDown;
        OnPointerMove = HandlePointerMove;
        OnPointerUp   = _ => _isDragging = false;
        _toggleCaret  = ToggleCaret;
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
        Observe(source, v => { if (!IsFocused) { Value = v.ToString(format, CultureInfo.InvariantCulture); _cursorPos = _selAnchor = Value.Length; } });
        return this;
    }

    public TextInput BindInt(Bindable<int> source)
    {
        Value = source.Value.ToString();
        _cursorPos = _selAnchor = Value.Length;
        OnChanged      += s => { if (int.TryParse(s, out var v)) source.Value = v; };
        Observe(source, v => { if (!IsFocused) { Value = v.ToString(); _cursorPos = _selAnchor = Value.Length; } });
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
        string display = DisplayText;
        float  sz      = ctx.Theme.FontSizeBase;

        using (ctx.PushClip(inner))
        {
            if (IsFocused && HasSelection)
            {
                float selX0 = inner.X + CaretX(SelectionStart);
                float selX1 = inner.X + CaretX(SelectionEnd);
                ctx.FillRect(new Rect(selX0, inner.Y, selX1 - selX0, inner.Height),
                             ctx.Theme.Primary.WithAlpha(0.3f));
            }

            if (!string.IsNullOrEmpty(display))
                ctx.DrawText(display, inner, ctx.Theme.OnSurface, sz);
            else if (!string.IsNullOrEmpty(Placeholder) && !IsFocused)
                ctx.DrawText(Placeholder, inner, ctx.Theme.OnSurfaceMuted, sz);

            if (IsFocused && _cursorVisible)
            {
                float cx = inner.X + CaretX(_cursorPos);
                ctx.DrawLine(cx, inner.Y + 2, cx, inner.Bottom - 2, ctx.Theme.OnSurface, cap: SKStrokeCap.Butt);
            }
        }
    }

    // ── Caret blink ───────────────────────────────────────────────────────────
    // A timer instead of per-frame polling, so a focused field costs two frames a second.

    private void ToggleCaret()
    {
        _cursorVisible = !_cursorVisible;
        Invalidate();
    }

    private void ResetBlink()
    {
        _cursorVisible = true;
        _blinkTimer?.Dispose();
        _blinkTimer = IsFocused ? Host?.Platform.StartTimer(BlinkInterval, _toggleCaret) : null;
        Invalidate();
    }

    private void StopBlink()
    {
        _blinkTimer?.Dispose();
        _blinkTimer = null;
        Invalidate();
    }

    protected override void OnDetached() => IsFocused = false;

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

        int length = DisplayText.Length;
        for (int i = 1; i <= length; i++)
        {
            float prev = CaretX(i - 1);
            float w    = CaretX(i);
            if (w >= relX)
                return (relX - prev < w - relX) ? i - 1 : i;
        }
        return length;
    }

    // ── Text measurement cache ────────────────────────────────────────────────
    // The shown text and the x offset of every caret slot are rebuilt only when
    // the text, password mode or font change, not on every frame or click.

    private string  _display = "";
    private string? _displaySource;
    private bool    _displayIsPassword;

    private float[] _caretXs = [0f];
    private string? _caretXsText;
    private string? _caretXsFamily;
    private float   _caretXsFontSize;

    private string DisplayText
    {
        get
        {
            if (!ReferenceEquals(_displaySource, Value) || _displayIsPassword != IsPassword)
            {
                _display           = IsPassword ? new string('•', Value.Length) : Value;
                _displaySource     = Value;
                _displayIsPassword = IsPassword;
            }
            return _display;
        }
    }

    // Distance from the start of the text to caret slot index (0..length).
    private float CaretX(int index)
    {
        string display  = DisplayText;
        var    host     = Host;
        float  fontSize = (host?.Theme ?? Theme.Light).FontSizeBase;
        string family   = host?.FontFamily ?? FontCache.DefaultFamily;

        if (!ReferenceEquals(_caretXsText, display) || _caretXsFamily != family || _caretXsFontSize != fontSize)
        {
            if (_caretXs.Length < display.Length + 1)
                _caretXs = new float[display.Length + 1];

            using var font = LayoutFont.Get(host, fontSize);
            _caretXs[0] = 0f;
            for (int i = 1; i <= display.Length; i++)
                _caretXs[i] = font.Font.MeasureText(display.AsSpan(0, i));

            _caretXsText     = display;
            _caretXsFamily   = family;
            _caretXsFontSize = fontSize;
        }
        return _caretXs[Math.Clamp(index, 0, display.Length)];
    }

    // ── Key handling ──────────────────────────────────────────────────────────

    private void HandleKey(KeyEvent e)
    {
        if (!e.IsDown) return;
        switch (e.Key)
        {
            case Key.Backspace:
                if (HasSelection) DeleteSelection();
                else if (_cursorPos > 0)
                {
                    // The Value setter clamps the caret, so compute the new position first.
                    int at = _cursorPos - 1;
                    Value = Value.Remove(at, 1);
                    _cursorPos = _selAnchor = at;
                }
                break;
            case Key.Delete:
                if (HasSelection) DeleteSelection();
                else if (_cursorPos < Value.Length) Value = Value.Remove(_cursorPos, 1);
                break;
            case Key.Left:
                if (e.Shift)
                    _cursorPos = Math.Max(0, _cursorPos - 1);
                else if (HasSelection)
                    _cursorPos = _selAnchor = SelectionStart;
                else
                    _cursorPos = _selAnchor = Math.Max(0, _cursorPos - 1);
                Invalidate(); break;
            case Key.Right:
                if (e.Shift)
                    _cursorPos = Math.Min(Value.Length, _cursorPos + 1);
                else if (HasSelection)
                    _cursorPos = _selAnchor = SelectionEnd;
                else
                    _cursorPos = _selAnchor = Math.Min(Value.Length, _cursorPos + 1);
                Invalidate(); break;
            case Key.Home:
                if (e.Shift) _cursorPos = 0;
                else _cursorPos = _selAnchor = 0;
                Invalidate(); break;
            case Key.End:
                if (e.Shift) _cursorPos = Value.Length;
                else _cursorPos = _selAnchor = Value.Length;
                Invalidate(); break;
            case Key.Enter or Key.KeypadEnter: OnSubmit?.Invoke(Value); break;
            case Key.A when e.Ctrl:
                _selAnchor = 0; _cursorPos = Value.Length; Invalidate(); break;
        }
        ResetBlink();
    }

    private void HandleText(TextInputEvent e)
    {
        foreach (char c in e.Text)
            if (!IsCharAllowed(c)) return;

        if (HasSelection) DeleteSelection();
        Value      = Value.Insert(_cursorPos, e.Text);
        _cursorPos += e.Text.Length;
        _selAnchor = _cursorPos;
        ResetBlink();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void DeleteSelection()
    {
        int start = SelectionStart;
        Value      = Value.Remove(start, SelectionEnd - start);
        _cursorPos = _selAnchor = start;
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
