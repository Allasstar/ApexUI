using System.Globalization;

namespace ApexUI.Widgets;

public enum InputMode { Any, Integer, Float }

/// Single-line text field.
/// Keys: arrows, Home/End (with Shift to select, Ctrl to jump by word), Backspace/Delete (Ctrl: a word),
/// Ctrl+A/C/X/V, Enter submits, Escape restores the text it had when focused and leaves the field.
/// Mouse: click places the caret, Shift+click and drag select, double click selects a word, triple click all.
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
            ScrollToCaret();
            Invalidate();
            ValueChanged?.Invoke(value);
        }
    } = "";

    public string Placeholder
    {
        get;
        set => SetRenderProperty(ref field, value);
    } = "";

    /// Restricts which characters can be typed. Does not re-validate existing Value.
    public InputMode InputMode  { get; set => SetRenderProperty(ref field, value); } = InputMode.Any;

    /// Replaces visible characters with '•'. Value still holds the real text. Copy and cut are disabled.
    public bool IsPassword      { get; set => SetRenderProperty(ref field, value); } = false;

    /// When set, only characters present in this string are accepted. Applied after InputMode.
    public string? AllowedChars { get; set; }

    /// When set, characters present in this string are always rejected. Applied after InputMode.
    public string? BlockedChars { get; set; }

    /// Full-string predicate; false → red border. Does not block typing — use for feedback only.
    public Func<string, bool>? Validate { get; set => SetRenderProperty(ref field, value); }

    public bool IsValid => Validate is null || Validate(Value);

    public event Action<string>? ValueChanged;
    public event Action<string>? Submitted;   // Enter

    private int   _cursorPos;
    private int   _selAnchor;          // selection anchor; differs from _cursorPos when text is selected
    private float _scrollX;            // how far the text is shifted left to keep the caret in view
    private string _valueOnFocus = "";
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
        MinWidth     = 120f;
        Focusable    = true;
        Cursor       = CursorShape.Text;
        _toggleCaret = ToggleCaret;
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public TextInput WithPlaceholder(string ph)            { Placeholder = ph; return this; }
    public TextInput WithValue(string v)                   { Value = v; _cursorPos = _selAnchor = v.Length; return this; }
    public TextInput OnChange(Action<string> a)            { ValueChanged += a; return this; }
    public TextInput WithSubmit(Action<string> a)          { Submitted += a; return this; }
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
        ValueChanged += s => { if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) source.Value = v; };
        Observe(source, v => { if (!IsFocused) { Value = v.ToString(format, CultureInfo.InvariantCulture); _cursorPos = _selAnchor = Value.Length; } });
        return this;
    }

    public TextInput BindInt(Bindable<int> source)
    {
        Value = source.Value.ToString();
        _cursorPos = _selAnchor = Value.Length;
        ValueChanged += s => { if (int.TryParse(s, out var v)) source.Value = v; };
        Observe(source, v => { if (!IsFocused) { Value = v.ToString(); _cursorPos = _selAnchor = Value.Length; } });
        return this;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Thickness DefaultPadding(Theme theme) => theme.InputPadding;
    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;

    protected override Size MeasureCore(Size available)
        => new(available.Width, Theme.FontSizeBody * Theme.LineHeight);

    protected override void ArrangeCore(Rect finalRect) => ScrollToCaret();

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        bool valid = IsValid;
        var borderColor = IsFocused
            ? (valid ? ctx.Theme.Primary : ctx.Theme.Danger)
            : (valid ? ctx.Theme.Border  : ctx.Theme.Danger);

        float radius = CornerRadius;
        ctx.FillRoundRect(LayoutBounds, radius, ctx.Theme.Background);
        ctx.StrokeRoundRect(LayoutBounds, radius, borderColor, IsFocused ? 2f : 1f);

        var    inner   = ContentBounds;
        float  textX   = inner.X - _scrollX;
        string display = DisplayText;
        float  sz      = ctx.Theme.FontSizeBody;

        using (ctx.PushClip(inner))
        {
            if (IsFocused && HasSelection)
            {
                float selX0 = textX + CaretX(SelectionStart);
                float selX1 = textX + CaretX(SelectionEnd);
                ctx.FillRect(new Rect(selX0, inner.Y, selX1 - selX0, inner.Height),
                             ctx.Theme.Selection);
            }

            if (!string.IsNullOrEmpty(display))
                ctx.DrawText(display, new Rect(textX, inner.Y, inner.Width + _scrollX, inner.Height), ctx.Theme.OnSurface, sz);
            else if (!string.IsNullOrEmpty(Placeholder) && !IsFocused)
                ctx.DrawText(Placeholder, inner, ctx.Theme.OnSurfaceMuted, sz);

            if (IsFocused && _cursorVisible)
            {
                float cx = textX + CaretX(_cursorPos);
                ctx.DrawLine(cx, inner.Y + 2, cx, inner.Bottom - 2, ctx.Theme.OnSurface, cap: SKStrokeCap.Butt);
            }
        }
    }

    // The focused border already shows focus.
    protected override void DrawFocusRing(DrawContext ctx) { }

    // ── Focus and caret blink ─────────────────────────────────────────────────
    // A timer instead of per-frame polling, so a focused field costs two frames a second.

    protected override void OnGotFocus()
    {
        _valueOnFocus = Value;
        if (Host is { IsFocusVisible: true }) SelectAll();
        ResetBlink();
    }

    protected override void OnLostFocus()
    {
        _isDragging = false;
        StopBlink();
    }

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

    // ── Pointer handling ──────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Left) return;
        e.Handled = true;
        int idx = HitTestCharIndex(e.X);
        switch (e.ClickCount)
        {
            case 1:
                _cursorPos = idx;
                if (!e.Shift) _selAnchor = idx;
                break;
            case 2:
                _selAnchor = WordStart(idx);
                _cursorPos = WordEnd(idx);
                break;
            default:
                SelectAll();
                break;
        }
        _isDragging = true;
        CaretMoved();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isDragging) return;
        e.Handled = true;
        int idx = HitTestCharIndex(e.X);
        if (idx == _cursorPos) return;
        _cursorPos = idx;
        CaretMoved();
    }

    protected override void OnPointerCaptureLost() => _isDragging = false;

    // Maps an X coordinate in this widget's layout space to the nearest character index in Value.
    private int HitTestCharIndex(float x)
    {
        var inner = ContentBounds;
        float relX = x - inner.X + _scrollX;
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
        var    theme    = Theme;
        float  fontSize = theme.FontSizeBody;
        string family   = theme.FontFamily;

        if (!ReferenceEquals(_caretXsText, display) || _caretXsFamily != family || _caretXsFontSize != fontSize)
        {
            if (_caretXs.Length < display.Length + 1)
                _caretXs = new float[display.Length + 1];

            using var font = LayoutFont.Get(Host, fontSize);
            _caretXs[0] = 0f;
            for (int i = 1; i <= display.Length; i++)
                _caretXs[i] = font.Font.MeasureText(display.AsSpan(0, i));

            _caretXsText     = display;
            _caretXsFamily   = family;
            _caretXsFontSize = fontSize;
        }
        return _caretXs[Math.Clamp(index, 0, display.Length)];
    }

    // Shifts the text so the caret stays inside the field, and pulls it back when text is removed.
    private void ScrollToCaret()
    {
        float width = ContentBounds.Width;
        if (width <= 0f) { _scrollX = 0f; return; }

        float caret = CaretX(_cursorPos);
        if (caret - _scrollX > width) _scrollX = caret - width;
        if (caret < _scrollX)         _scrollX = caret;
        _scrollX = Math.Clamp(_scrollX, 0f, Math.Max(0f, CaretX(DisplayText.Length) - width));
    }

    private void CaretMoved()
    {
        ScrollToCaret();
        ResetBlink();
    }

    // ── Key handling ──────────────────────────────────────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        bool word = e.Ctrl;
        switch (e.Key)
        {
            case Key.Backspace:
                if (HasSelection) DeleteSelection();
                else if (_cursorPos > 0) DeleteRange(word ? PreviousWordStop(_cursorPos) : _cursorPos - 1, _cursorPos);
                break;
            case Key.Delete:
                if (HasSelection) DeleteSelection();
                else if (_cursorPos < Value.Length) DeleteRange(_cursorPos, word ? NextWordStop(_cursorPos) : _cursorPos + 1);
                break;
            case Key.Left:
                if (!e.Shift && HasSelection && !word) MoveCaret(SelectionStart, false);
                else MoveCaret(word ? PreviousWordStop(_cursorPos) : Math.Max(0, _cursorPos - 1), e.Shift);
                break;
            case Key.Right:
                if (!e.Shift && HasSelection && !word) MoveCaret(SelectionEnd, false);
                else MoveCaret(word ? NextWordStop(_cursorPos) : Math.Min(Value.Length, _cursorPos + 1), e.Shift);
                break;
            case Key.Home:
                MoveCaret(0, e.Shift);
                break;
            case Key.End:
                MoveCaret(Value.Length, e.Shift);
                break;
            case Key.Enter or Key.KeypadEnter:
                Submitted?.Invoke(Value);
                break;
            case Key.Escape:
                Value = _valueOnFocus;
                _cursorPos = _selAnchor = Value.Length;
                Host?.ClearFocus();
                e.Handled = true;
                return;
            case Key.A when e.Ctrl:
                SelectAll();
                break;
            case Key.C when e.Ctrl:
                Copy();
                break;
            case Key.X when e.Ctrl:
                if (Copy()) DeleteSelection();
                break;
            case Key.V when e.Ctrl:
                Paste();
                break;
            default:
                return;
        }
        e.Handled = true;
        CaretMoved();
    }

    protected override void OnTextEntered(TextInputEventArgs e)
    {
        e.Handled = true;
        if (InsertText(e.Text)) CaretMoved();
    }

    // ── Editing ───────────────────────────────────────────────────────────────

    private bool InsertText(string text)
    {
        foreach (char c in text)
            if (!IsCharAllowed(c)) return false;
        if (HasSelection) DeleteSelection();

        int at = _cursorPos;
        Value      = Value.Insert(at, text);
        _cursorPos = _selAnchor = at + text.Length;
        return true;
    }

    private void MoveCaret(int index, bool extendSelection)
    {
        _cursorPos = index;
        if (!extendSelection) _selAnchor = index;
        Invalidate();
    }

    public void SelectAll()
    {
        _selAnchor = 0;
        _cursorPos = Value.Length;
        Invalidate();
    }

    private void DeleteSelection() => DeleteRange(SelectionStart, SelectionEnd);

    // The Value setter clamps the caret, so the new position is set after it.
    private void DeleteRange(int start, int end)
    {
        Value      = Value.Remove(start, end - start);
        _cursorPos = _selAnchor = start;
    }

    private bool Copy()
    {
        if (!HasSelection || IsPassword || Host is null) return false;
        Host.Platform.SetClipboardText(Value[SelectionStart..SelectionEnd]);
        return true;
    }

    private void Paste()
    {
        var text = Host?.Platform.GetClipboardText();
        if (string.IsNullOrEmpty(text)) return;
        // Single line: line breaks become spaces, other control characters are dropped.
        text = text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        if (text.Any(char.IsControl)) text = new string(text.Where(c => !char.IsControl(c)).ToArray());
        InsertText(text);
    }

    // ── Word boundaries ───────────────────────────────────────────────────────
    // A word is a run of letters and digits, or a run of other symbols; spaces separate them.
    // A password field is one word, so word jumps do not reveal its structure.

    private int PreviousWordStop(int from)
    {
        if (IsPassword) return 0;
        string s = Value;
        int i = from;
        while (i > 0 && char.IsWhiteSpace(s[i - 1])) i--;
        if (i == 0) return 0;
        int kind = CharKind(s[i - 1]);
        while (i > 0 && CharKind(s[i - 1]) == kind) i--;
        return i;
    }

    private int NextWordStop(int from)
    {
        if (IsPassword) return Value.Length;
        string s = Value;
        int i = from;
        if (i < s.Length && !char.IsWhiteSpace(s[i]))
        {
            int kind = CharKind(s[i]);
            while (i < s.Length && CharKind(s[i]) == kind) i++;
        }
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        return i;
    }

    private int WordStart(int index)
    {
        if (IsPassword || Value.Length == 0) return 0;
        string s = Value;
        int kind = CharKind(s[Math.Min(index, s.Length - 1)]);
        int i = Math.Min(index, s.Length - 1);
        while (i > 0 && CharKind(s[i - 1]) == kind) i--;
        return i;
    }

    private int WordEnd(int index)
    {
        if (IsPassword || Value.Length == 0) return Value.Length;
        string s = Value;
        int i = Math.Min(index, s.Length - 1);
        int kind = CharKind(s[i]);
        while (i < s.Length && CharKind(s[i]) == kind) i++;
        return i;
    }

    private static int CharKind(char c)
        => char.IsWhiteSpace(c) ? 0 : char.IsLetterOrDigit(c) || c == '_' ? 1 : 2;

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
