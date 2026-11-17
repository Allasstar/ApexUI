using System.Globalization;

namespace ApexUI.Widgets;

/// Picks a color: a saturation/brightness square, a hue strip, an alpha strip (unless ShowAlpha is off)
/// and a hex field (#RRGGBB, or #RRGGBBAA when not opaque). The picker keeps hue while the color is gray,
/// so dragging to white and back does not lose it. ColorChanged fires on every change; EditCompleted once
/// when a drag or a typed hex value ends with a different color.
public class ColorPicker : Widget
{
    private readonly ColorArea _area;
    private readonly HueStrip _hue;
    private readonly AlphaStrip _alpha;
    private readonly TextInput _hex;
    private float _h;
    private float _s;
    private float _v;
    private byte _a = 255;
    private SKColor _color;
    private SKColor _colorBeforeEdit;

    public ColorPicker(SKColor? color = null)
    {
        _area = new ColorArea(this);
        _hue = new HueStrip(this);
        _alpha = new AlphaStrip(this);
        _hex = new TextInput();
        _hex.Submitted += _ => CommitHex();
        _hex.GotFocus += () => _colorBeforeEdit = Color;
        _hex.LostFocus += CommitHex;
        AddChild(new Column(_area, _hue, _alpha, _hex) { SpacingSource = t => t.SpacingSm });
        HAlign = HAlign.Left;
        SetColor(color ?? SKColors.White, notify: false);
    }

    // As wide as the color square: the hex field would otherwise take all the width it is offered.
    protected override Size MeasureCore(Size available)
    {
        var size = base.MeasureCore(new Size(Theme.ColorAreaSize, available.Height));
        return new Size(Theme.ColorAreaSize, size.Height);
    }

    public SKColor Color
    {
        get => _color;
        set => SetColor(value, notify: true);
    }

    public bool ShowAlpha
    {
        get => _alpha.IsVisible;
        set => _alpha.IsVisible = value;
    }

    public event Action<SKColor>? ColorChanged;

    public event Action<SKColor>? EditCompleted;

    public ColorPicker OnChange(Action<SKColor> action) { ColorChanged += action; return this; }

    public ColorPicker OnEditCompleted(Action<SKColor> action) { EditCompleted += action; return this; }

    /// "#RRGGBB" for opaque colors, otherwise "#RRGGBBAA".
    public static string ToHex(SKColor c) => c.Alpha == 255
        ? $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}"
        : $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}{c.Alpha:X2}";

    /// Accepts RRGGBB or RRGGBBAA, with or without a leading #.
    public static bool TryParseHex(string text, out SKColor color)
    {
        color = default;
        var s = text.AsSpan().Trim();
        if (s.Length > 0 && s[0] == '#') s = s[1..];
        if (s.Length is not (6 or 8)) return false;
        if (!uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value)) return false;
        uint rgba = s.Length == 6 ? (value << 8) | 0xFF : value;
        color = new SKColor((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
        return true;
    }

    // ── State ─────────────────────────────────────────────────────────────────

    private void SetColor(SKColor color, bool notify)
    {
        if (notify && color == _color) return;
        color.ToHsv(out float h, out float s, out float v);
        // Gray has no hue and black no saturation either; keep what the user picked last.
        if (s > 0f && v > 0f) _h = h;
        if (v > 0f) _s = s;
        _v = v;
        _a = color.Alpha;
        _color = color;
        Changed(notify);
    }

    private void SetHsv(float h, float s, float v)
    {
        if (h == _h && s == _s && v == _v) return;
        _h = h;
        _s = s;
        _v = v;
        _color = SKColor.FromHsv(_h, _s, _v, _a);
        Changed(notify: true);
    }

    private void SetAlpha(byte a)
    {
        if (a == _a) return;
        _a = a;
        _color = _color.WithAlpha(a);
        Changed(notify: true);
    }

    private void Changed(bool notify)
    {
        _hex.Value = ToHex(Color);
        _area.Invalidate();
        _hue.Invalidate();
        _alpha.Invalidate();
        if (notify) ColorChanged?.Invoke(Color);
    }

    private void BeginDrag() => _colorBeforeEdit = Color;

    private void EndDrag()
    {
        if (Color != _colorBeforeEdit) EditCompleted?.Invoke(Color);
    }

    private void CommitHex()
    {
        if (TryParseHex(_hex.Value, out SKColor parsed)) SetColor(parsed, notify: true);
        else _hex.Value = ToHex(Color);

        if (Color == _colorBeforeEdit) return;
        _colorBeforeEdit = Color;
        EditCompleted?.Invoke(Color);
    }

    // ── Parts ─────────────────────────────────────────────────────────────────

    /// A strip or area that sets the color from where it is pressed and dragged.
    private abstract class DragArea : Widget
    {
        protected readonly ColorPicker Picker;
        private bool _dragging;

        protected DragArea(ColorPicker picker)
        {
            Picker = picker;
            Cursor = CursorShape.Crosshair;
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Button != PointerButton.Left) return;
            e.Handled = true;
            _dragging = true;
            Picker.BeginDrag();
            Pick(Fraction(e.X, LayoutBounds.X, LayoutBounds.Width), Fraction(e.Y, LayoutBounds.Y, LayoutBounds.Height));
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!_dragging) return;
            e.Handled = true;
            Pick(Fraction(e.X, LayoutBounds.X, LayoutBounds.Width), Fraction(e.Y, LayoutBounds.Y, LayoutBounds.Height));
        }

        protected override void OnPointerCaptureLost()
        {
            if (!_dragging) return;
            _dragging = false;
            Picker.EndDrag();
        }

        protected abstract void Pick(float fx, float fy);

        private static float Fraction(float p, float start, float length) => length > 0f ? Math.Clamp((p - start) / length, 0f, 1f) : 0f;
    }

    private sealed class ColorArea(ColorPicker picker) : DragArea(picker)
    {
        private SKPaint? _paint;
        private SKShader? _white;
        private SKShader? _black;
        private Rect _shaderRect;

        protected override Size MeasureCore(Size available) => new(Theme.ColorAreaSize, Theme.ColorAreaSize);

        protected override void Pick(float fx, float fy) => Picker.SetHsv(Picker._h, fx * 100f, (1f - fy) * 100f);

        protected override void DrawCore(DrawContext ctx)
        {
            var b = LayoutBounds;
            if (_white is null || b != _shaderRect) BuildShaders(b);
            _paint ??= new SKPaint { IsAntialias = true };

            ctx.FillRect(b, SKColor.FromHsv(Picker._h, 100f, 100f));
            _paint.Shader = _white;
            ctx.Canvas.DrawRect(b.ToSKRect(), _paint);
            _paint.Shader = _black;
            ctx.Canvas.DrawRect(b.ToSKRect(), _paint);
            _paint.Shader = null;

            float x = b.X + Picker._s / 100f * b.Width;
            float y = b.Y + (1f - Picker._v / 100f) * b.Height;
            float r = ctx.Theme.ColorStripHeight * 0.5f;
            ctx.StrokeCircle(x, y, r + 1f, SKColors.Black.WithAlpha(0.4f), 1f);
            ctx.StrokeCircle(x, y, r, SKColors.White, 2f);
        }

        protected override void OnDetached()
        {
            _white?.Dispose();
            _black?.Dispose();
            _white = _black = null;
            _paint?.Dispose();
            _paint = null;
        }

        private void BuildShaders(Rect b)
        {
            _white?.Dispose();
            _black?.Dispose();
            _shaderRect = b;
            _white = SKShader.CreateLinearGradient(new SKPoint(b.X, b.Y), new SKPoint(b.Right, b.Y),
                [SKColors.White, SKColors.White.WithAlpha(0)], SKShaderTileMode.Clamp);
            _black = SKShader.CreateLinearGradient(new SKPoint(b.X, b.Y), new SKPoint(b.X, b.Bottom),
                [SKColors.Black.WithAlpha(0), SKColors.Black], SKShaderTileMode.Clamp);
        }
    }

    private abstract class Strip(ColorPicker picker) : DragArea(picker)
    {
        private SKPaint? _paint;
        private SKShader? _shader;
        private Rect _shaderRect;
        private SKColor _shaderEnd;

        protected override Size MeasureCore(Size available) => new(Theme.ColorAreaSize, Theme.ColorStripHeight);

        protected void FillGradient(DrawContext ctx, SKColor[] colors, SKColor key)
        {
            var b = LayoutBounds;
            if (_shader is null || b != _shaderRect || key != _shaderEnd)
            {
                _shader?.Dispose();
                _shader = SKShader.CreateLinearGradient(new SKPoint(b.X, b.Y), new SKPoint(b.Right, b.Y), colors, SKShaderTileMode.Clamp);
                _shaderRect = b;
                _shaderEnd = key;
            }

            _paint ??= new SKPaint { IsAntialias = true };
            _paint.Shader = _shader;
            ctx.Canvas.DrawRoundRect(b.ToSKRect(), b.Height * 0.5f, b.Height * 0.5f, _paint);
            _paint.Shader = null;
        }

        protected void DrawMarker(DrawContext ctx, float fraction)
        {
            var b = LayoutBounds;
            float x = b.X + fraction * b.Width;
            float r = b.Height * 0.5f;
            ctx.FillCircle(Math.Clamp(x, b.X + r, b.Right - r), b.CenterY, r, SKColors.White);
            ctx.StrokeCircle(Math.Clamp(x, b.X + r, b.Right - r), b.CenterY, r, SKColors.Black.WithAlpha(0.4f), 1f);
        }

        protected override void OnDetached()
        {
            _shader?.Dispose();
            _shader = null;
            _paint?.Dispose();
            _paint = null;
        }
    }

    private sealed class HueStrip(ColorPicker picker) : Strip(picker)
    {
        private static readonly SKColor[] Hues =
            [SKColors.Red, SKColors.Yellow, SKColors.Lime, SKColors.Cyan, SKColors.Blue, SKColors.Magenta, SKColors.Red];

        protected override void Pick(float fx, float fy) => Picker.SetHsv(Math.Min(fx * 360f, 359.99f), Picker._s, Picker._v);

        protected override void DrawCore(DrawContext ctx)
        {
            FillGradient(ctx, Hues, SKColors.Red);
            DrawMarker(ctx, Picker._h / 360f);
        }
    }

    private sealed class AlphaStrip(ColorPicker picker) : Strip(picker)
    {
        private readonly SKColor[] _colors = new SKColor[2];

        protected override void Pick(float fx, float fy) => Picker.SetAlpha((byte)MathF.Round(fx * 255f));

        protected override void DrawCore(DrawContext ctx)
        {
            var b = LayoutBounds;
            var t = ctx.Theme;
            using (ctx.PushClip(b, b.Height * 0.5f))
                ctx.FillCheckerboard(b, b.Height * 0.5f, t.Surface, t.SurfacePressed);

            SKColor opaque = Picker.Color.WithAlpha(255);
            _colors[0] = opaque.WithAlpha(0);
            _colors[1] = opaque;
            FillGradient(ctx, _colors, opaque);
            DrawMarker(ctx, Picker._a / 255f);
        }
    }
}
