namespace ApexUI.Core;

// Eyedropper: while picking, the pointer belongs to UiHost. Every frame reads the pixel under it (after the tree and
// overlays are drawn, before the preview chip), and a press picks what the next frame shows there.
public sealed partial class UiHost
{
    private Action<SKColor>? _picked;
    private Action? _pickCancelled;
    private bool _pickPressed;
    private float _pickX = float.NaN;
    private float _pickY = float.NaN;

    /// Whether PickPixel can work: the platform reads back its rendered frames.
    public bool CanPickPixels => Platform.CanReadPixels;

    public bool IsPickingPixel => _picked is not null;

    /// The color under the pointer in the latest frame while picking.
    public SKColor PickPreviewColor { get; private set; }

    /// Starts an eyedropper: the next left press anywhere in the window picks the color drawn under it and calls
    /// picked; Escape or a right press cancels. Pointer events do not reach widgets until it ends. A pick already
    /// running is cancelled first.
    public void PickPixel(Action<SKColor> picked, Action? cancelled = null)
    {
        if (!CanPickPixels) return;
        CancelPixelPick();
        _picked = picked;
        _pickCancelled = cancelled;
        _pickPressed = false;
        _pickX = _pointerX;
        _pickY = _pointerY;
        _cursor = CursorShape.Crosshair;
        Platform.SetCursor(CursorShape.Crosshair);
        RequestFrame();
    }

    public void CancelPixelPick()
    {
        if (_picked is null) return;
        var cancelled = _pickCancelled;
        EndPixelPick();
        cancelled?.Invoke();
    }

    private void EndPixelPick()
    {
        _picked = null;
        _pickCancelled = null;
        _pickPressed = false;
        UpdateCursor();
        RequestFrame();
    }

    private bool PickMove(float x, float y)
    {
        if (_picked is null) return false;
        _pickX = x;
        _pickY = y;
        RequestFrame();
        return true;
    }

    private bool PickPress(float x, float y, PointerButton button)
    {
        if (_picked is null) return false;
        if (button != PointerButton.Left)
        {
            CancelPixelPick();
            return true;
        }

        _pickX = x;
        _pickY = y;
        _pickPressed = true;
        RequestFrame();
        return true;
    }

    private bool PickKey(Key key)
    {
        if (_picked is null) return false;
        if (key == Key.Escape) CancelPixelPick();
        return true;
    }

    // Called inside Render, with the canvas still scaled to logical units.
    private void RenderPixelPick(DrawContext ctx)
    {
        if (_picked is null || float.IsNaN(_pickX)) return;

        int px = (int)MathF.Floor(_pickX * PixelScale);
        int py = (int)MathF.Floor(_pickY * PixelScale);
        if (Platform.TryReadPixel(px, py, out SKColor color)) PickPreviewColor = color;

        if (_pickPressed)
        {
            var picked = _picked;
            EndPixelPick();
            picked(PickPreviewColor);
            return;
        }

        var t = ctx.Theme;
        float size = t.SwatchSize;
        var chip = new Rect(_pickX + t.SpacingSm, _pickY + t.SpacingSm, size, size);
        ctx.FillRoundRect(chip, t.CornerRadiusSmall, PickPreviewColor.WithAlpha(255));
        ctx.StrokeRoundRect(chip, t.CornerRadiusSmall, t.Border);
    }
}
