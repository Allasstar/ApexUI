namespace ApexUI.Widgets;

/// A shaded sphere for picking a direction, such as where a light comes from. Dragging sets the
/// azimuth (the angle around the centre, degrees clockwise on screen from the +x axis) and the
/// elevation (0 on the rim, 90 in the middle, straight towards the viewer). The sphere is lit from
/// the chosen direction and a handle marks it.
public class DirectionPicker : Widget
{
    /// Keyboard step in degrees: Left/Right turn the azimuth, Up/Down raise or lower the elevation.
    public const float KeyStep = 5f;

    private const float RimInset = 2f;

    private bool _dragging;
    private bool _changedSincePress;
    private SKShader? _shader;
    private SKPaint? _paint;
    private (float Az, float El, Rect Bounds, SKColor Lit, SKColor Shade) _shaderKey;

    /// Degrees in [0, 360), clockwise on screen from the +x axis.
    public float Azimuth { get; private set; }

    /// Degrees in [0, 90]; 90 points at the viewer.
    public float Elevation { get; private set; } = 45f;

    /// Raised on every change made by the user while dragging or by a key.
    public event Action<float, float>? DirectionChanged;

    /// Raised once a drag that changed the direction ends, and after each key step.
    public event Action<float, float>? EditCompleted;

    public DirectionPicker()
    {
        Focusable = true;
    }

    /// Sets the direction from code; raises no event.
    public void SetDirection(float azimuth, float elevation)
    {
        float az = Normalize(azimuth), el = Math.Clamp(elevation, 0f, 90f);
        if (az == Azimuth && el == Elevation) return;
        Azimuth = az;
        Elevation = el;
        Invalidate();
    }

    /// The direction a point in the widget picks: its angle around the centre and its distance
    /// from the rim. At the very centre the azimuth stays as it is.
    public (float Azimuth, float Elevation) DirectionAt(float x, float y)
    {
        var (cx, cy, r) = Sphere();
        float dx = x - cx, dy = y - cy;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        float az = distance > 0.5f ? Normalize(MathF.Atan2(dy, dx) * 180f / MathF.PI) : Azimuth;
        float el = r > 0f ? 90f * (1f - Math.Clamp(distance / r, 0f, 1f)) : 90f;
        return (az, el);
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public DirectionPicker WithDirection(float azimuth, float elevation) { SetDirection(azimuth, elevation); return this; }
    public DirectionPicker OnChange(Action<float, float> a)              { DirectionChanged += a; return this; }
    public DirectionPicker OnCommit(Action<float, float> a)              { EditCompleted += a; return this; }

    // ── Input ────────────────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Left) return;
        e.Handled = true;
        _dragging = true;
        _changedSincePress = false;
        PickAt(e.X, e.Y);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging) return;
        e.Handled = true;
        PickAt(e.X, e.Y);
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (!_dragging) return;
        e.Handled = true;
        EndDrag();
    }

    protected override void OnPointerCaptureLost() => EndDrag();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        float az = Azimuth, el = Elevation;
        switch (e.Key)
        {
            case Key.Left:  az -= KeyStep; break;
            case Key.Right: az += KeyStep; break;
            case Key.Up:    el += KeyStep; break;
            case Key.Down:  el -= KeyStep; break;
            default: return;
        }

        e.Handled = true;
        if (!Change(az, el)) return;
        EditCompleted?.Invoke(Azimuth, Elevation);
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        float w = float.IsNaN(Width) ? Theme.DirectionPickerSize : Width;
        float h = float.IsNaN(Height) ? w : Height;
        return new Size(w, h);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        var (cx, cy, r) = Sphere();
        if (r <= 0f) return;

        var t = ctx.Theme;
        var key = (Azimuth, Elevation, LayoutBounds, t.SphereLit, t.SphereShade);
        if (_shader is null || key != _shaderKey) BuildShader(cx, cy, r, key);
        _paint ??= new SKPaint { IsAntialias = true };
        _paint.Shader = _shader;
        ctx.Canvas.DrawCircle(cx, cy, r, _paint);
        _paint.Shader = null;
        ctx.StrokeCircle(cx, cy, r, t.Border, 1f);

        var (hx, hy) = HandlePosition(cx, cy, r);
        float handle = t.SliderThumbSize * 0.25f;
        ctx.FillCircle(hx, hy, handle, IsPressed || IsHovered ? t.PrimaryHover : t.Primary);
        ctx.StrokeCircle(hx, hy, handle, t.Knob, 1.5f);
    }

    protected override void OnDetached()
    {
        _shader?.Dispose();
        _shader = null;
        _paint?.Dispose();
        _paint = null;
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void PickAt(float x, float y)
    {
        var (az, el) = DirectionAt(x, y);
        _changedSincePress |= Change(az, el);
    }

    private void EndDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        if (_changedSincePress) EditCompleted?.Invoke(Azimuth, Elevation);
        _changedSincePress = false;
    }

    private bool Change(float azimuth, float elevation)
    {
        float az = Normalize(azimuth), el = Math.Clamp(elevation, 0f, 90f);
        if (az == Azimuth && el == Elevation) return false;
        Azimuth = az;
        Elevation = el;
        Invalidate();
        DirectionChanged?.Invoke(az, el);
        return true;
    }

    private (float X, float Y, float R) Sphere()
    {
        var b = LayoutBounds;
        return (b.CenterX, b.CenterY, MathF.Max(0f, MathF.Min(b.Width, b.Height) * 0.5f - RimInset));
    }

    private (float X, float Y) HandlePosition(float cx, float cy, float r)
    {
        float az = Azimuth * MathF.PI / 180f;
        float reach = r * (1f - Elevation / 90f);
        return (cx + MathF.Cos(az) * reach, cy + MathF.Sin(az) * reach);
    }

    // A radial gradient from the lit point to the shaded rim reads as a sphere lit from there.
    private void BuildShader(float cx, float cy, float r, (float Az, float El, Rect Bounds, SKColor Lit, SKColor Shade) key)
    {
        _shader?.Dispose();
        var (hx, hy) = HandlePosition(cx, cy, r);
        _shader = SKShader.CreateRadialGradient(new SKPoint(hx, hy), r * 1.6f, [key.Lit, key.Shade], [0f, 1f], SKShaderTileMode.Clamp);
        _shaderKey = key;
    }

    private static float Normalize(float degrees)
    {
        float d = degrees % 360f;
        return d < 0f ? d + 360f : d;
    }
}
