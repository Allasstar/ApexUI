namespace ApexUI.Widgets;

/// Pick-one-from-a-list widget. Opens an Overlay with a scrollable item list on click.
public class Dropdown<T> : Widget
{
    private readonly List<(T Value, string Label)> _items = [];
    private T? _selected;
    private string _placeholder = "Select...";

    private readonly Button _toggleButton;
    private readonly Column _listColumn;
    private readonly Overlay _overlay;
    private Bindable<T>? _binding;
    private bool _suppressBinding;

    public T? SelectedValue
    {
        get => _selected;
        set
        {
            _selected = value;
            SyncButtonText();
            if (!_suppressBinding && _binding is not null)
            {
                _suppressBinding = true;
                _binding.Value = value!;
                _suppressBinding = false;
            }
            OnChanged?.Invoke(value);
        }
    }

    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; SyncButtonText(); }
    }

    public Action<T?>? OnChanged;

    public Dropdown()
    {
        _listColumn = new Column { Spacing = 2f };

        var scroll = new Scroll(_listColumn).HideScrollbar();
        scroll.MaxHeight = 200f;

        var panel = new PaddingBox(scroll, new Thickness(4f))
        {
            BackgroundSource = t => t.Surface,
            CornerRadius = 8f,
            MinWidth = 160f,
        };

        _overlay = new Overlay { DismissOnClickOutside = true };
        _overlay.Content = panel;

        _toggleButton = new Button(Placeholder).WithVariant(ButtonVariant.Secondary);
        _toggleButton.OnClick = _ =>
        {
            if (_overlay.IsVisible) _overlay.Close();
            else _overlay.Open(_toggleButton, OverlayAnchor.BelowAnchor);
        };

        AddChild(_toggleButton);
    }

    // ── Fluent API ────────────────────────────────────────────────────────────

    public Dropdown<T> AddItem(T value, string label)
    {
        _items.Add((value, label));
        RebuildList();
        return this;
    }

    public Dropdown<T> WithPlaceholder(string text) { Placeholder = text; return this; }

    public Dropdown<T> WithSelected(T value) { SelectedValue = value; return this; }

    public Dropdown<T> OnChange(Action<T?> action) { OnChanged = action; return this; }

    public Dropdown<T> Bind(Bindable<T> source)
    {
        _binding = source;
        _suppressBinding = true;
        SelectedValue = source.Value;
        _suppressBinding = false;
        source.Changed += v =>
        {
            if (_suppressBinding) return;
            _suppressBinding = true;
            SelectedValue = v;
            _suppressBinding = false;
        };
        return this;
    }

    // ── Layout pass-through ───────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        _toggleButton.Measure(available);
        return _toggleButton.DesiredSize;
    }

    protected override void ArrangeCore(Rect r)
        => _toggleButton.Arrange(r);

    // ── Internal ──────────────────────────────────────────────────────────────

    private void SyncButtonText()
    {
        if (_selected is null)
        {
            _toggleButton.Text = _placeholder;
            return;
        }
        var match = _items.FirstOrDefault(i => EqualityComparer<T>.Default.Equals(i.Value, _selected));
        _toggleButton.Text = match.Label ?? _placeholder;
    }

    private void RebuildList()
    {
        _listColumn.RemoveAllChildren();
        foreach (var (value, label) in _items)
        {
            var itemLabel = label;
            var itemValue = value;

            var row = new DropdownItem(label,
                () => EqualityComparer<T>.Default.Equals(_selected, itemValue));
            row.OnClick = _ =>
            {
                SelectedValue = itemValue;
                _overlay.Close();
            };
            _listColumn.Add(row);
        }
    }

    // ── Private item widget ───────────────────────────────────────────────────

    private sealed class DropdownItem : Widget
    {
        private readonly Label _label;
        private readonly Func<bool> _isSelected;

        public DropdownItem(string text, Func<bool> isSelected)
        {
            _isSelected = isSelected;
            _label = new Label { Text = text, IsHitTestVisible = false };
            Padding = new Thickness(12f, 6f);
            CornerRadius = 6f;
            AddChild(_label);
        }

        protected override Size MeasureCore(Size available)
        {
            _label.Measure(new Size(available.Width - Padding.Horizontal,
                available.Height - Padding.Vertical));
            return new Size(
                _label.DesiredSize.Width + Padding.Horizontal,
                _label.DesiredSize.Height + Padding.Vertical);
        }

        protected override void ArrangeCore(Rect r)
            => _label.Arrange(r.Deflate(Padding));

        protected override void DrawCore(DrawContext ctx)
        {
            var t = ctx.Theme;
            bool selected = _isSelected();

            var bg = selected   ? t.Primary.WithAlpha(0.15f)
                : IsPressed ? t.SurfacePressed
                : IsHovered ? t.SurfaceHover
                : SKColor.Empty;

            if (bg != SKColor.Empty)
            {
                using var p = ctx.MakePaint(bg);
                ctx.Canvas.DrawRoundRect(LayoutBounds.ToSKRect(), CornerRadius, CornerRadius, p);
            }

            _label.Color = selected ? t.Primary : t.OnSurface;
        }
    }
}
