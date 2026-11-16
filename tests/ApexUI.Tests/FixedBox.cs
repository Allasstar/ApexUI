namespace ApexUI.Tests;

/// Test widget that always asks for the same content size.
internal sealed class FixedBox(float width, float height) : Widget
{
    protected override Size MeasureCore(Size available) => new(width, height);
}
