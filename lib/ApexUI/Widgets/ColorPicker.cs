using System.Globalization;

namespace ApexUI.Widgets;

/// Picks a color: a saturation/brightness square, a hue strip, an alpha strip (unless ShowAlpha is off), a hex field
/// (#RRGGBB, or #RRGGBBAA when not opaque) with an eyedropper button, R, G, B and A fields, and rows of swatches
/// the host supplies (AddSwatches). The picker keeps hue while the color is gray, so dragging to white and back does
/// not lose it. ColorChanged fires on every change; EditCompleted once when a drag, a typed value, a swatch or an
/// eyedropper pick ends with a color different from the last completed one.
public class ColorPicker : Widget
{
    // A pipette, tip at the bottom left.
    private const string EyedropperIcon = "M3 21l1-4 9-9 3 3-9 9zM13 8l2-2a2.1 2.1 0 0 1 3 3l-2 2";

    private readonly ColorArea _area;
    private readonly HueStrip _hue;
    private readonly AlphaStrip _alpha;
    private readonly TextInput _hex;
    private readonly IconButton _eyedropper;
    private readonly NumberField[] _channels = new NumberField[4];
    private readonly Column _swatchRows;
    private float _h;
    private float _s;
    private float _v;
    private byte _a = 255;
    private SKColor _color;
    private SKColor _committed;
    private bool _showingChannels;

    public ColorPicker(SKColor? color = null)
    {
        _area = new ColorArea(this);
        _hue = new HueStrip(this);
        _alpha = new AlphaStrip(this);
        _hex = new TextInput { Grow = 1f };
        _hex.Submitted += _ => CommitHex();
        _hex.LostFocus += CommitHex;
        _eyedropper = new IconButton(new Icon(EyedropperIcon, IconStyle.Stroke), StartEyedropper);

        var channels = new ChannelRow();
        string[] captions = ["R", "G", "B", "A"];
        for (int i = 0; i < 4; i++)
        {
            var field = new NumberField(captions[i]).WithRange(0f, 255f).WithStep(1f).WithFormat("0");
            field.ValueChanged += _ => ChannelChanged();
            field.EditCompleted += _ => CompleteEdit();
            _channels[i] = field;
            channels.Add(field);
        }

        _swatchRows = new Column { SpacingSource = t => t.SpacingXs };
        AddChild(new Column(_area, _hue, _alpha, new Row(_hex, _eyedropper) { SpacingSource = t => t.SpacingXs }, channels, _swatchRows)
        {
            SpacingSource = t => t.SpacingSm,
        });
        HAlign = HAlign.Left;
        SetColor(color ?? SKColors.White, notify: false);
        _committed = _color;
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
        set
        {
            if (value == _color) return;
            SetColor(value, notify: true);
            _committed = value;
        }
    }

    public bool ShowAlpha
    {
        get => _alpha.IsVisible;
        set
        {
            _alpha.IsVisible = value;
            _channels[3].IsVisible = value;
        }
    }

    /// Whether the picker is waiting for an eyedropper click.
    public bool IsPickingPixel => _eyedropper.IsSelected;

    public event Action<SKColor>? ColorChanged;

    public event Action<SKColor>? EditCompleted;

    public ColorPicker OnChange(Action<SKColor> action) { ColorChanged += action; return this; }

    public ColorPicker OnEditCompleted(Action<SKColor> action) { EditCompleted += action; return this; }

    /// Adds a titled row of swatches that follows colors (for example recent or document colors); clicking one
    /// picks it. The row hides while the list is empty.
    public ColorPicker AddSwatches(string title, ObservableList<SKColor> colors)
    {
        _swatchRows.Add(new SwatchRow(this, title, colors));
        return this;
    }

    /// Starts the eyedropper (UiHost.PickPixel): the next click anywhere in the window picks the color drawn there;
    /// Escape cancels. Does nothing when the platform cannot read its frames.
    public void StartEyedropper()
    {
        if (Host is not { CanPickPixels: true } host) return;
        _eyedropper.IsSelected = true;
        host.PickPixel(Picked, () => _eyedropper.IsSelected = false);
    }

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

    protected override void OnAttached(UiHost host) => _eyedropper.IsVisible = host.CanPickPixels;

    protected override void OnDetached()
    {
        if (_eyedropper.IsSelected) Host?.CancelPixelPick();
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
        _showingChannels = true;
        _channels[0].Value = _color.Red;
        _channels[1].Value = _color.Green;
        _channels[2].Value = _color.Blue;
        _channels[3].Value = _color.Alpha;
        _showingChannels = false;
        _area.Invalidate();
        _hue.Invalidate();
        _alpha.Invalidate();
        if (notify) ColorChanged?.Invoke(Color);
    }

    private void ChannelChanged()
    {
        if (_showingChannels) return;
        SetColor(new SKColor(Channel(0), Channel(1), Channel(2), Channel(3)), notify: true);
    }

    private byte Channel(int index) => (byte)Math.Clamp(MathF.Round(_channels[index].Value), 0f, 255f);

    // Drags, typed values, swatches and the eyedropper end here: one EditCompleted per change that stays.
    private void CompleteEdit()
    {
        if (_color == _committed) return;
        _committed = _color;
        EditCompleted?.Invoke(_color);
    }

    private void Pick(SKColor color)
    {
        SetColor(color, notify: true);
        CompleteEdit();
    }

    private void Picked(SKColor color)
    {
        _eyedropper.IsSelected = false;
        Pick(ShowAlpha ? color : color.WithAlpha(255));
    }

    private void CommitHex()
    {
        if (TryParseHex(_hex.Value, out SKColor parsed)) SetColor(parsed, notify: true);
        else _hex.Value = ToHex(Color);
        CompleteEdit();
    }

    // Two equal columns that share the picker's width, whatever the fields would like: R G over B A.
    private sealed class ChannelRow : Widget
    {
        private const int Columns = 2;

        public void Add(Widget child) => AddChild(child);

        protected override Size MeasureCore(Size available)
        {
            float width = ColumnWidth(available.Width);
            float rowHeight = 0f;
            int count = 0;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                child.Measure(new Size(width, available.Height));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
                count++;
            }

            int rows = (count + Columns - 1) / Columns;
            float height = rows * rowHeight + Math.Max(0, rows - 1) * Theme.SpacingXs;
            return new Size(float.IsInfinity(available.Width) ? 0f : available.Width, height);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            float width = ColumnWidth(finalRect.Width);
            float rowHeight = 0f;
            foreach (var child in Children) rowHeight = Math.Max(rowHeight, child.IsVisible ? child.DesiredSize.Height : 0f);
            int i = 0;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                float x = finalRect.X + i % Columns * (width + Theme.SpacingSm);
                float y = finalRect.Y + i / Columns * (rowHeight + Theme.SpacingXs);
                child.Arrange(new Rect(x, y, width, rowHeight));
                i++;
            }
        }

        private float ColumnWidth(float available) =>
            float.IsInfinity(available) ? available : Math.Max(0f, (available - (Columns - 1) * Theme.SpacingSm) / Columns);
    }

    private sealed class SwatchRow : Widget
    {
        private readonly ColorPicker _picker;
        private readonly ObservableList<SKColor> _colors;
        private readonly ChipFlow _chips = new();

        public SwatchRow(ColorPicker picker, string title, ObservableList<SKColor> colors)
        {
            _picker = picker;
            _colors = colors;
            var label = new Label { Text = title, Role = TextRole.Small, ColorSource = t => t.OnSurfaceMuted };
            AddChild(new Column(label, _chips) { SpacingSource = t => t.SpacingXs });
            Rebuild();
            Observe(colors, Rebuild);
        }

        private void Rebuild()
        {
            _chips.RemoveAllChildren();
            for (int i = 0; i < _colors.Count; i++) _chips.Add(new Chip(_picker, _colors[i]));
            IsVisible = _colors.Count > 0;
        }
    }

    // Swatch-sized chips left to right, wrapping, SpacingXs apart.
    private sealed class ChipFlow : Widget
    {
        public void Add(Widget chip) => AddChild(chip);

        protected override Size MeasureCore(Size available)
        {
            float size = Theme.SwatchSize;
            float gap = Theme.SpacingXs;
            int count = Children.Count;
            if (count == 0) return Size.Zero;
            foreach (var child in Children) child.Measure(new Size(size, size));
            int perLine = PerLine(available.Width);
            int lines = (count + perLine - 1) / perLine;
            int columns = Math.Min(count, perLine);
            return new Size(columns * size + (columns - 1) * gap, lines * size + (lines - 1) * gap);
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            float size = Theme.SwatchSize;
            float gap = Theme.SpacingXs;
            int perLine = PerLine(finalRect.Width);
            int i = 0;
            foreach (var child in Children)
            {
                float x = finalRect.X + i % perLine * (size + gap);
                float y = finalRect.Y + i / perLine * (size + gap);
                child.Arrange(new Rect(x, y, size, size));
                i++;
            }
        }

        private int PerLine(float width)
        {
            if (float.IsInfinity(width)) return Math.Max(1, Children.Count);
            return Math.Max(1, (int)((width + Theme.SpacingXs) / (Theme.SwatchSize + Theme.SpacingXs)));
        }
    }

    private sealed class Chip : Widget
    {
        private readonly ColorPicker _picker;
        private readonly SKColor _color;

        public Chip(ColorPicker picker, SKColor color)
        {
            _picker = picker;
            _color = color;
            Cursor = CursorShape.Hand;
        }

        protected override Size MeasureCore(Size available) => new(Theme.SwatchSize, Theme.SwatchSize);

        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

        protected override void OnClick(PointerEventArgs e)
        {
            e.Handled = true;
            _picker.Pick(_color);
        }

        protected override void DrawCore(DrawContext ctx)
        {
            var t = ctx.Theme;
            var b = LayoutBounds;
            using (ctx.PushClip(b, CornerRadius))
            {
                if (_color.Alpha < 255) ctx.FillCheckerboard(b, b.Height * 0.5f, t.Surface, t.SurfacePressed);
                ctx.FillRect(b, _color);
            }

            ctx.StrokeRoundRect(b, CornerRadius, IsHovered ? t.OnSurfaceMuted : t.Border);
        }
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
            Picker.CompleteEdit();
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
