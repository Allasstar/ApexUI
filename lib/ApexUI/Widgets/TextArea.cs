namespace ApexUI.Widgets;

/// Multi-line text field. Value holds the lines joined by '\n' (pasted "\r\n" and '\r' become '\n').
/// It is MinLines to MaxLines lines tall, following the text, and scrolls to keep the caret in view; lines do not wrap.
/// Keys: arrows (Up/Down keep the caret's column on screen), Home/End (line; Ctrl: whole text), Shift selects,
/// Ctrl+Left/Right and Ctrl+Backspace/Delete by word, Ctrl+A/C/X/V, Enter inserts a line break, Ctrl+Enter
/// completes the edit, Escape restores the text it had when focused and leaves the field.
/// Mouse: click places the caret, Shift+click and drag select, double click selects a word, triple click all.
/// EditCompleted fires once per edit (Ctrl+Enter or leaving the field) when the text changed since it got focus.
public class TextArea : Widget
{
    private static readonly TimeSpan BlinkInterval = TimeSpan.FromSeconds(0.53);

    private readonly Action _toggleCaret;
    private IDisposable? _blinkTimer;
    private bool _cursorVisible = true;
    private bool _isDragging;
    private int _cursorPos;
    private int _selAnchor;
    private float _scrollX;
    private float _scrollY;
    // The x the caret keeps while moving up and down through shorter lines.
    private float _preferredX = float.NaN;
    private string _valueOnFocus = "";

    // Lines of Value and the caret x of every slot of each line, rebuilt when Value or the font changes.
    private string[] _lines = [""];
    private int[] _lineStarts = [0];
    private float[][] _caretXs = [[0f]];
    private string? _linesSource;
    private string? _measuredFamily;
    private float _measuredSize;

    public TextArea(string placeholder = "")
    {
        Placeholder  = placeholder;
        MinWidth     = 120f;
        Focusable    = true;
        Cursor       = CursorShape.Text;
        _toggleCaret = ToggleCaret;
    }

    public string Value
    {
        get;
        set
        {
            value = Normalize(value);
            if (field == value) return;
            bool grows = CountLines(field) != CountLines(value);
            field = value;
            _cursorPos = Math.Min(_cursorPos, value.Length);
            _selAnchor = Math.Min(_selAnchor, value.Length);
            if (grows) InvalidateLayout();
            else Invalidate();
            ScrollToCaret();
            ValueChanged?.Invoke(value);
        }
    } = "";

    public string Placeholder
    {
        get;
        set => SetRenderProperty(ref field, value);
    } = "";

    /// The fewest lines the field shows, also when empty.
    public int MinLines
    {
        get;
        set => SetLayoutProperty(ref field, Math.Max(1, value));
    } = 3;

    /// The most lines the field grows to; longer text scrolls.
    public int MaxLines
    {
        get;
        set => SetLayoutProperty(ref field, Math.Max(1, value));
    } = 10;

    public int CaretIndex => _cursorPos;

    public int SelectionStart => Math.Min(_cursorPos, _selAnchor);

    public int SelectionEnd => Math.Max(_cursorPos, _selAnchor);

    public override bool EditsText => true;

    public event Action<string>? ValueChanged;

    /// Ctrl+Enter or focus left, after the text changed.
    public event Action<string>? EditCompleted;

    private bool HasSelection => _cursorPos != _selAnchor;

    // ── Fluent ───────────────────────────────────────────────────────────────

    public TextArea WithPlaceholder(string placeholder) { Placeholder = placeholder; return this; }
    public TextArea WithValue(string value)             { Value = value; _cursorPos = _selAnchor = Value.Length; return this; }
    public TextArea WithLines(int min, int max)         { MinLines = min; MaxLines = Math.Max(min, max); return this; }
    public TextArea OnChange(Action<string> action)     { ValueChanged += action; return this; }
    public TextArea OnEditCompleted(Action<string> action) { EditCompleted += action; return this; }

    /// Selects the whole text, as a field focused to be retyped wants.
    public void SelectAll()
    {
        _selAnchor = 0;
        _cursorPos = Value.Length;
        _preferredX = float.NaN;
        ScrollToCaret();
        Invalidate();
    }

    /// Selects [start, end), the caret at end.
    public void Select(int start, int end)
    {
        _selAnchor = Math.Clamp(start, 0, Value.Length);
        _cursorPos = Math.Clamp(end, 0, Value.Length);
        _preferredX = float.NaN;
        ScrollToCaret();
        Invalidate();
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Thickness DefaultPadding(Theme theme) => theme.InputPadding;
    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;

    private float LineHeight => Theme.FontSizeBody * Theme.LineHeight;

    protected override Size MeasureCore(Size available)
    {
        int shown = Math.Clamp(CountLines(Value), MinLines, Math.Max(MinLines, MaxLines));
        return new Size(float.IsInfinity(available.Width) ? 0f : available.Width, shown * LineHeight);
    }

    protected override void ArrangeCore(Rect finalRect) => ScrollToCaret();

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        bool enabled = IsEnabledInTree;
        float radius = CornerRadius;
        ctx.FillRoundRect(LayoutBounds, radius, enabled ? ctx.Theme.Background : ctx.Theme.Surface);
        ctx.StrokeRoundRect(LayoutBounds, radius, IsFocused ? ctx.Theme.Primary : ctx.Theme.Border, IsFocused ? 2f : 1f);

        Measure();
        var inner = ContentBounds;
        float lineHeight = LineHeight;
        float size = ctx.Theme.FontSizeBody;
        using var clip = ctx.PushClip(inner);
        if (Value.Length == 0 && !IsFocused && Placeholder.Length > 0)
        {
            ctx.DrawText(Placeholder, new Rect(inner.X, inner.Y, inner.Width, lineHeight), ctx.Theme.OnSurfaceMuted, size);
            return;
        }

        int first = Math.Max(0, (int)(_scrollY / lineHeight));
        int last = Math.Min(_lines.Length - 1, (int)((_scrollY + inner.Height) / lineHeight));
        int selStart = SelectionStart, selEnd = SelectionEnd;
        for (int line = first; line <= last; line++)
        {
            float y = inner.Y + line * lineHeight - _scrollY;
            float x = inner.X - _scrollX;
            if (IsFocused && HasSelection) DrawSelection(ctx, line, x, y, lineHeight, selStart, selEnd);
            ctx.DrawText(_lines[line], new Rect(x, y, inner.Width + _scrollX, lineHeight), enabled ? ctx.Theme.OnSurface : ctx.Theme.OnSurfaceMuted, size);
        }

        if (!IsFocused || !_cursorVisible) return;
        (int caretLine, int column) = LineOf(_cursorPos);
        float cx = inner.X - _scrollX + _caretXs[caretLine][column];
        float cy = inner.Y + caretLine * lineHeight - _scrollY;
        ctx.DrawLine(cx, cy + 2, cx, cy + lineHeight - 2, ctx.Theme.OnSurface, cap: SKStrokeCap.Butt);
    }

    // A selected line break shows as a small block after the line's text.
    private void DrawSelection(DrawContext ctx, int line, float x, float y, float lineHeight, int selStart, int selEnd)
    {
        int start = _lineStarts[line];
        int end = start + _lines[line].Length;
        if (selEnd < start || selStart > end) return;
        float[] xs = _caretXs[line];
        float x0 = xs[Math.Max(selStart, start) - start];
        float x1 = xs[Math.Min(selEnd, end) - start];
        if (selEnd > end) x1 += Theme.FontSizeBody * 0.3f;
        if (x1 > x0) ctx.FillRect(new Rect(x + x0, y, x1 - x0, lineHeight), ctx.Theme.Selection);
    }

    // The focused border already shows focus.
    protected override void DrawFocusRing(DrawContext ctx) { }

    // ── Focus and caret blink ─────────────────────────────────────────────────

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
        Complete();
    }

    private void Complete()
    {
        if (Value == _valueOnFocus) return;
        _valueOnFocus = Value;
        EditCompleted?.Invoke(Value);
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

    protected override void OnDetached() => StopBlink();

    // ── Pointer ───────────────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Left) return;
        e.Handled = true;
        int index = IndexAt(e.X, e.Y);
        switch (e.ClickCount)
        {
            case 1:
                _cursorPos = index;
                if (!e.Shift) _selAnchor = index;
                break;
            case 2:
                _selAnchor = WordBoundaries.WordStart(Value, index);
                _cursorPos = WordBoundaries.WordEnd(Value, index);
                break;
            default:
                _selAnchor = 0;
                _cursorPos = Value.Length;
                break;
        }
        _isDragging = true;
        _preferredX = float.NaN;
        CaretMoved();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isDragging) return;
        e.Handled = true;
        int index = IndexAt(e.X, e.Y);
        if (index == _cursorPos) return;
        _cursorPos = index;
        CaretMoved();
    }

    protected override void OnPointerCaptureLost() => _isDragging = false;

    private int IndexAt(float x, float y)
    {
        Measure();
        var inner = ContentBounds;
        int line = Math.Clamp((int)MathF.Floor((y - inner.Y + _scrollY) / LineHeight), 0, _lines.Length - 1);
        return _lineStarts[line] + ColumnAt(line, x - inner.X + _scrollX);
    }

    private int ColumnAt(int line, float x)
    {
        float[] xs = _caretXs[line];
        int length = _lines[line].Length;
        if (x <= 0) return 0;
        for (int i = 1; i <= length; i++)
        {
            if (xs[i] >= x) return x - xs[i - 1] < xs[i] - x ? i - 1 : i;
        }
        return length;
    }

    // ── Keys ──────────────────────────────────────────────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        bool word = e.Ctrl;
        bool keepColumn = false;
        switch (e.Key)
        {
            case Key.Backspace:
                if (HasSelection) DeleteSelection();
                else if (_cursorPos > 0) DeleteRange(word ? WordBoundaries.PreviousStop(Value, _cursorPos) : _cursorPos - 1, _cursorPos);
                break;
            case Key.Delete:
                if (HasSelection) DeleteSelection();
                else if (_cursorPos < Value.Length) DeleteRange(_cursorPos, word ? WordBoundaries.NextStop(Value, _cursorPos) : _cursorPos + 1);
                break;
            case Key.Left:
                if (!e.Shift && HasSelection && !word) MoveCaret(SelectionStart, false);
                else MoveCaret(word ? WordBoundaries.PreviousStop(Value, _cursorPos) : Math.Max(0, _cursorPos - 1), e.Shift);
                break;
            case Key.Right:
                if (!e.Shift && HasSelection && !word) MoveCaret(SelectionEnd, false);
                else MoveCaret(word ? WordBoundaries.NextStop(Value, _cursorPos) : Math.Min(Value.Length, _cursorPos + 1), e.Shift);
                break;
            case Key.Up:
                MoveVertically(-1, e.Shift);
                keepColumn = true;
                break;
            case Key.Down:
                MoveVertically(1, e.Shift);
                keepColumn = true;
                break;
            case Key.Home:
                MoveCaret(word ? 0 : _lineStarts[LineOf(_cursorPos).Line], e.Shift);
                break;
            case Key.End:
            {
                int line = LineOf(_cursorPos).Line;
                MoveCaret(word ? Value.Length : _lineStarts[line] + _lines[line].Length, e.Shift);
                break;
            }
            case Key.Enter or Key.KeypadEnter when e.Ctrl:
                Complete();
                break;
            case Key.Enter or Key.KeypadEnter:
                InsertText("\n");
                break;
            case Key.Escape:
                Value = _valueOnFocus;
                _cursorPos = _selAnchor = Value.Length;
                Host?.ClearFocus();
                e.Handled = true;
                return;
            case Key.A when e.Ctrl:
                _selAnchor = 0;
                _cursorPos = Value.Length;
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
        if (!keepColumn) _preferredX = float.NaN;
        CaretMoved();
    }

    protected override void OnTextEntered(TextInputEventArgs e)
    {
        e.Handled = true;
        InsertText(e.Text);
        _preferredX = float.NaN;
        CaretMoved();
    }

    private void MoveVertically(int lines, bool extendSelection)
    {
        Measure();
        (int line, int column) = LineOf(_cursorPos);
        if (float.IsNaN(_preferredX)) _preferredX = _caretXs[line][column];
        int target = line + lines;
        int index = target < 0 ? 0
            : target >= _lines.Length ? Value.Length
            : _lineStarts[target] + ColumnAt(target, _preferredX);
        MoveCaret(index, extendSelection);
    }

    // ── Editing ───────────────────────────────────────────────────────────────

    private void InsertText(string text)
    {
        if (HasSelection) DeleteSelection();
        int at = _cursorPos;
        text = Normalize(text);
        Value = Value.Insert(at, text);
        _cursorPos = _selAnchor = at + text.Length;
    }

    private void MoveCaret(int index, bool extendSelection)
    {
        _cursorPos = index;
        if (!extendSelection) _selAnchor = index;
        Invalidate();
    }

    private void DeleteSelection() => DeleteRange(SelectionStart, SelectionEnd);

    // The Value setter clamps the caret, so the new position is set after it.
    private void DeleteRange(int start, int end)
    {
        Value = Value.Remove(start, end - start);
        _cursorPos = _selAnchor = start;
    }

    private bool Copy()
    {
        if (!HasSelection || Host is null) return false;
        Host.Platform.SetClipboardText(Value[SelectionStart..SelectionEnd]);
        return true;
    }

    // Line breaks stay; tabs become spaces and other control characters are dropped.
    private void Paste()
    {
        var text = Host?.Platform.GetClipboardText();
        if (string.IsNullOrEmpty(text)) return;
        text = Normalize(text).Replace('\t', ' ');
        if (text.Any(c => c != '\n' && char.IsControl(c))) text = new string(text.Where(c => c == '\n' || !char.IsControl(c)).ToArray());
        InsertText(text);
    }

    private static string Normalize(string text) =>
        text.Contains('\r') ? text.Replace("\r\n", "\n").Replace('\r', '\n') : text;

    // ── Lines and measuring ───────────────────────────────────────────────────

    private static int CountLines(string text)
    {
        int count = 1;
        foreach (char c in text)
            if (c == '\n') count++;
        return count;
    }

    private (int Line, int Column) LineOf(int index)
    {
        Measure();
        int line = _lineStarts.Length - 1;
        while (line > 0 && _lineStarts[line] > index) line--;
        return (line, Math.Min(index - _lineStarts[line], _lines[line].Length));
    }

    private void Measure()
    {
        float size = Theme.FontSizeBody;
        string family = Theme.FontFamily;
        if (ReferenceEquals(_linesSource, Value) && _measuredFamily == family && _measuredSize == size) return;

        _lines = Value.Split('\n');
        _lineStarts = new int[_lines.Length];
        _caretXs = new float[_lines.Length][];
        using var font = LayoutFont.Get(Host, size);
        int start = 0;
        for (int line = 0; line < _lines.Length; line++)
        {
            string text = _lines[line];
            _lineStarts[line] = start;
            var xs = new float[text.Length + 1];
            for (int i = 1; i <= text.Length; i++) xs[i] = font.Font.MeasureText(text.AsSpan(0, i));
            _caretXs[line] = xs;
            start += text.Length + 1;
        }

        _linesSource = Value;
        _measuredFamily = family;
        _measuredSize = size;
    }

    private void ScrollToCaret()
    {
        var inner = ContentBounds;
        if (inner.Width <= 0f || inner.Height <= 0f)
        {
            _scrollX = _scrollY = 0f;
            return;
        }

        (int line, int column) = LineOf(_cursorPos);
        float caretX = _caretXs[line][column];
        if (caretX - _scrollX > inner.Width) _scrollX = caretX - inner.Width;
        if (caretX < _scrollX) _scrollX = caretX;
        float widest = 0f;
        foreach (float[] xs in _caretXs) widest = MathF.Max(widest, xs[^1]);
        _scrollX = Math.Clamp(_scrollX, 0f, MathF.Max(0f, widest - inner.Width));

        float lineHeight = LineHeight;
        float top = line * lineHeight;
        if (top + lineHeight - _scrollY > inner.Height) _scrollY = top + lineHeight - inner.Height;
        if (top < _scrollY) _scrollY = top;
        _scrollY = Math.Clamp(_scrollY, 0f, MathF.Max(0f, _lines.Length * lineHeight - inner.Height));
    }

    private void CaretMoved()
    {
        ScrollToCaret();
        ResetBlink();
    }
}
