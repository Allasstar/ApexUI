namespace ApexUI.Widgets;

public enum RadioOrientation { Vertical, Horizontal }

/// Mutually-exclusive group of radio options backed by a Bindable<T>.
/// Use AddOption() to populate, then Bind() or SelectedValue to control selection.
public class RadioGroup<T> : Widget
{
    // ── Inner radio button ────────────────────────────────────────────────────

    private sealed class RadioItem : Widget
    {
        private const float CircleR = 9f;
        private const float DotR    = 5f;
        private const float Gap     = 8f;

        public string ItemLabel { get; }
        public bool IsSelected  { get; set { field = value; Invalidate(); } }

        public RadioItem(string label) => ItemLabel = label;

        protected override Size MeasureCore(Size available)
        {
            using var font = new SKFont(
                SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), 14f);
            float textW = string.IsNullOrEmpty(ItemLabel) ? 0f : font.MeasureText(ItemLabel) + 2f;
            return new Size(CircleR * 2 + Gap + textW, MathF.Max(CircleR * 2 + 4f, 14f * 1.4f));
        }

        protected override void DrawCore(DrawContext ctx)
        {
            float cx = LayoutBounds.X + CircleR;
            float cy = LayoutBounds.CenterY;

            // Hover ripple
            if ((IsHovered || IsPressed) && IsEnabled)
            {
                using var ripple = ctx.MakePaint(ctx.Theme.Primary.WithAlpha(0.12f));
                ctx.Canvas.DrawCircle(cx, cy, CircleR + 5f, ripple);
            }

            // Outer ring
            var ringColor = IsSelected && IsEnabled ? ctx.Theme.Primary : ctx.Theme.Border;
            using (var ring = ctx.MakePaint(ringColor))
            {
                ring.IsStroke = true;
                ring.StrokeWidth = 2f;
                ctx.Canvas.DrawCircle(cx, cy, CircleR, ring);
            }

            // Inner dot when selected
            if (IsSelected)
            {
                using var dot = ctx.MakePaint(IsEnabled ? ctx.Theme.Primary : ctx.Theme.Border);
                ctx.Canvas.DrawCircle(cx, cy, DotR, dot);
            }

            // Label text
            if (!string.IsNullOrEmpty(ItemLabel))
            {
                using var font  = ctx.MakeTextFont(14f);
                using var paint = ctx.MakeTextPaint(IsEnabled ? ctx.Theme.OnSurface : ctx.Theme.OnSurfaceMuted);
                var m  = font.Metrics;
                float ty = cy - (m.Ascent + m.Descent) * 0.5f;
                ctx.Canvas.DrawText(ItemLabel, LayoutBounds.X + CircleR * 2 + Gap, ty, SKTextAlign.Left, font, paint);
            }
        }
    }

    // ── RadioGroup<T> ─────────────────────────────────────────────────────────

    private readonly RadioOrientation _orientation;
    private readonly float            _spacing;
    private readonly List<(T Value, RadioItem Item)> _options = [];

    private T? _selected;

    public T? SelectedValue
    {
        get => _selected;
        set
        {
            _selected = value;
            foreach (var (v, item) in _options)
                item.IsSelected = EqualityComparer<T>.Default.Equals(v, value);
            OnChanged?.Invoke(value);
        }
    }

    public Action<T?>? OnChanged;

    public RadioGroup(RadioOrientation orientation = RadioOrientation.Vertical, float spacing = 8f)
    {
        _orientation = orientation;
        _spacing     = spacing;
    }

    // ── Fluent ────────────────────────────────────────────────────────────────

    public RadioGroup<T> AddOption(T value, string label)
    {
        var item = new RadioItem(label);
        item.OnClick = _ =>
        {
            if (!item.IsEnabled) return;
            SelectedValue = value;
        };
        _options.Add((value, item));
        AddChild(item);
        return this;
    }

    public RadioGroup<T> WithSelected(T value)    { SelectedValue = value; return this; }
    public RadioGroup<T> OnChange(Action<T?> action) { OnChanged  = action; return this; }

    public RadioGroup<T> Bind(Bindable<T> source)
    {
        SelectedValue   = source.Value;
        OnChanged      += v => { if (v is not null) source.Value = v; };
        source.Changed += v => SelectedValue = v;
        return this;
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        if (_orientation == RadioOrientation.Vertical)
        {
            float totalH = 0f, maxW = 0f;
            bool first = true;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                if (!first) totalH += _spacing;
                child.Measure(new Size(available.Width, MathF.Max(0f, available.Height - totalH)));
                totalH += child.DesiredSize.Height;
                maxW    = MathF.Max(maxW, child.DesiredSize.Width);
                first   = false;
            }
            return new Size(maxW, totalH);
        }
        else
        {
            float totalW = 0f, maxH = 0f;
            bool first = true;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                if (!first) totalW += _spacing;
                child.Measure(new Size(MathF.Max(0f, available.Width - totalW), available.Height));
                totalW += child.DesiredSize.Width;
                maxH    = MathF.Max(maxH, child.DesiredSize.Height);
                first   = false;
            }
            return new Size(totalW, maxH);
        }
    }

    protected override void ArrangeCore(Rect rect)
    {
        if (_orientation == RadioOrientation.Vertical)
        {
            float y = rect.Y;
            bool first = true;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                if (!first) y += _spacing;
                child.Measure(new Size(rect.Width, MathF.Max(0f, rect.Bottom - y)));
                child.Arrange(new Rect(rect.X, y, rect.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height;
                first = false;
            }
        }
        else
        {
            float x = rect.X;
            bool first = true;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                if (!first) x += _spacing;
                child.Measure(new Size(MathF.Max(0f, rect.Right - x), rect.Height));
                child.Arrange(new Rect(x, rect.Y, child.DesiredSize.Width, rect.Height));
                x += child.DesiredSize.Width;
                first = false;
            }
        }
    }
}
