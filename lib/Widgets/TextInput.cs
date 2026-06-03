using System.Globalization;

namespace ApexUI.Widgets;

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

    public bool IsFocused { get; internal set; }
    public Action<string>? OnChanged;
    public Action<string>? OnSubmit;   // fired on Enter key

    // Internal cursor state
    private int _cursorPos;
    private float _blinkTimer;
    private bool _cursorVisible = true;

    public TextInput(string placeholder = "")
    {
        Placeholder = placeholder;
        Padding = new Thickness(10f, 6f);
        CornerRadius = 6f;
        MinWidth = 120f;

        // Handle text input via key events
        OnKeyDown = HandleKey;
    }

    public TextInput WithPlaceholder(string ph) { Placeholder = ph; return this; }
    public TextInput WithValue(string v)         { Value = v; _cursorPos = v.Length; return this; }
    public TextInput OnChange(Action<string> a)  { OnChanged = a; return this; }

    public TextInput BindFloat(Bindable<float> source, string format = "F2")
    {
        Value = source.Value.ToString(format, CultureInfo.InvariantCulture);
        OnChanged      += s => { if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) source.Value = v; };
        source.Changed += v => Value = v.ToString(format, CultureInfo.InvariantCulture);
        return this;
    }

    public TextInput BindInt(Bindable<int> source)
    {
        Value = source.Value.ToString();
        OnChanged      += s => { if (int.TryParse(s, out var v)) source.Value = v; };
        source.Changed += v => Value = v.ToString();
        return this;
    }

    protected override Size MeasureCore(Size available)
    {
        // Fixed height based on font + padding; width = available or MinWidth
        float h = 14f * 1.4f + Padding.Vertical;
        float w = float.IsNaN(Width) ? Math.Max(MinWidth, available.Width) : Width;
        return new Size(w, h);
    }

    protected override void DrawCore(DrawContext ctx)
    {
        var borderColor = IsFocused ? ctx.Theme.Primary : ctx.Theme.Border;
        var bgColor = ctx.Theme.Background;

        // Background
        using (var bg = ctx.MakePaint(bgColor))
            ctx.Canvas.DrawRoundRect(LayoutBounds.ToSKRect(), CornerRadius, CornerRadius, bg);

        // Border (thicker when focused)
        using (var border = ctx.MakePaint(borderColor))
        {
            border.IsStroke = true;
            border.StrokeWidth = IsFocused ? 2f : 1f;
            ctx.Canvas.DrawRoundRect(LayoutBounds.ToSKRect(), CornerRadius, CornerRadius, border);
        }

        var inner = LayoutBounds.Deflate(Padding);
        var metrics = default(SKFontMetrics);

        if (!string.IsNullOrEmpty(Value))
        {
            using var font  = ctx.MakeTextFont(ctx.Theme.FontSizeBase);
            using var paint = ctx.MakeTextPaint(ctx.Theme.OnSurface);
            metrics = font.Metrics;
            float y = inner.Y + (inner.Height - (metrics.Descent - metrics.Ascent)) * 0.5f - metrics.Ascent;
            ctx.Canvas.DrawText(Value, inner.X, y, SKTextAlign.Left, font, paint);

            if (IsFocused && _cursorVisible)
            {
                float cx = inner.X + font.MeasureText(Value[.._cursorPos]);
                using var cursor = ctx.MakePaint(ctx.Theme.OnSurface);
                ctx.Canvas.DrawLine(cx, inner.Y + 2, cx, inner.Bottom - 2, cursor);
            }
        }
        else if (!string.IsNullOrEmpty(Placeholder) && !IsFocused)
        {
            using var font  = ctx.MakeTextFont(ctx.Theme.FontSizeBase);
            using var paint = ctx.MakeTextPaint(ctx.Theme.OnSurfaceMuted);
            metrics = font.Metrics;
            float y = inner.Y + (inner.Height - (metrics.Descent - metrics.Ascent)) * 0.5f - metrics.Ascent;
            ctx.Canvas.DrawText(Placeholder, inner.X, y, SKTextAlign.Left, font, paint);
        }
    }

    // Called by the app host each frame to blink the cursor (~530ms interval)
    public void Tick(float deltaSeconds)
    {
        if (!IsFocused) return;
        _blinkTimer += deltaSeconds;
        if (_blinkTimer >= 0.53f)
        {
            _blinkTimer = 0;
            _cursorVisible = !_cursorVisible;
            Invalidate();
        }
    }

    private void HandleKey(KeyEvent e)
    {
        if (!e.IsDown) return;
        switch (e.Key)
        {
            case "Backspace":
                if (_cursorPos > 0)
                {
                    Value = Value.Remove(_cursorPos - 1, 1);
                    _cursorPos--;
                }
                break;
            case "Delete":
                if (_cursorPos < Value.Length)
                    Value = Value.Remove(_cursorPos, 1);
                break;
            case "ArrowLeft":  _cursorPos = Math.Max(0, _cursorPos - 1); Invalidate(); break;
            case "ArrowRight": _cursorPos = Math.Min(Value.Length, _cursorPos + 1); Invalidate(); break;
            case "Home":       _cursorPos = 0; Invalidate(); break;
            case "End":        _cursorPos = Value.Length; Invalidate(); break;
            case "Enter":      OnSubmit?.Invoke(Value); break;
            default:
                // Printable character — insert at cursor
                if (e.Key.Length == 1)
                {
                    Value = Value.Insert(_cursorPos, e.Key);
                    _cursorPos++;
                }
                break;
        }
    }
}