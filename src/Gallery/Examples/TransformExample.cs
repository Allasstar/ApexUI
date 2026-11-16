namespace ApexUI.Gallery.Examples;

/// Demonstrates Widget.ChildTransform: a pan and zoom view whose content is laid out once
/// and then only redrawn, hit-tested and sent pointer positions through the transform.
public class TransformExample : Widget
{
    public TransformExample()
    {
        var zoomLabel = new Label { Text = "Zoom 100%" };
        var clicks    = new Label { Text = "Button clicks: 0" };
        int count     = 0;

        var button = new Button("Click me, zoomed");
        button.OnPressed = () => clicks.Text = $"Button clicks: {++count}";

        var content = new Canvas()
            .Add(MakeTile("A", new SKColor(0x33, 0x6B, 0xE6)), 0, 0, 120, 120)
            .Add(MakeTile("B", new SKColor(0x4C, 0xAF, 0x50)), 140, 0, 120, 120)
            .Add(MakeTile("C", new SKColor(0xF4, 0x43, 0x36)), 0, 140, 120, 120)
            .Add(button, 140, 180)
            .Add(new Label { Text = "Wheel zooms around the pointer, drag pans.", FontSize = 12f }, 0, 280);

        AddChild(new Column(
            new Label { Text = "Child transform", Bold = true, FontSize = 18f },
            new Label
            {
                Text     = "The content below is laid out once. Zooming and panning change only the view's ChildTransform, so they redraw without a layout pass, and clicks still reach the right widget.",
                WordWrap = true,
            },
            new Row(zoomLabel, clicks).WithSpacing(24f),
            new ZoomView(content, z => zoomLabel.Text = $"Zoom {z * 100f:0}%")
            {
                Height           = 400f,
                BackgroundSource = t => t.Surface,
                CornerRadius     = 8f,
            }
        ) { Spacing = 12f, Padding = new Thickness(24f) });
    }

    private static Widget MakeTile(string text, SKColor color)
        => new Label
        {
            Text       = text,
            FontSize   = 40f,
            Bold       = true,
            Color      = SKColors.White,
            Background = color,
            CornerRadius = 8f,
            Padding    = new Thickness(44f, 32f),
        };

    private sealed class ZoomView : Widget
    {
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 8f;

        private readonly Action<float> _onZoom;
        private float _zoom = 1f;
        private float _panX, _panY;
        private bool  _panning;
        private float _dragStartX, _dragStartY, _panStartX, _panStartY;

        public ZoomView(Widget content, Action<float> onZoom)
        {
            _onZoom = onZoom;
            Cursor  = CursorShape.Move;
            AddChild(content);
        }

        // Content keeps its natural size; the transform does the rest.
        protected override void ArrangeCore(Rect r)
        {
            foreach (var child in Children)
            {
                child.Measure(Size.Infinite);
                child.Arrange(new Rect(r.X + 16f, r.Y + 16f, child.DesiredSize.Width, child.DesiredSize.Height));
            }
        }

        protected override void OnWheel(WheelEventArgs e)
        {
            e.Handled = true;
            float next = Math.Clamp(_zoom * MathF.Pow(1.15f, e.DeltaY), MinZoom, MaxZoom);
            // Keep the content point under the pointer where it is.
            float cx = (e.X - _panX) / _zoom;
            float cy = (e.Y - _panY) / _zoom;
            _zoom = next;
            _panX = e.X - cx * _zoom;
            _panY = e.Y - cy * _zoom;
            Apply();
            _onZoom(_zoom);
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Button != PointerButton.Left) return;
            _panning    = true;
            _dragStartX = e.X;
            _dragStartY = e.Y;
            _panStartX  = _panX;
            _panStartY  = _panY;
            CapturePointer();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!_panning) return;
            e.Handled = true;
            _panX = _panStartX + e.X - _dragStartX;
            _panY = _panStartY + e.Y - _dragStartY;
            Apply();
        }

        protected override void OnPointerCaptureLost() => _panning = false;

        private void Apply() => ChildTransform = new Transform2D(_panX, _panY, _zoom);
    }
}
