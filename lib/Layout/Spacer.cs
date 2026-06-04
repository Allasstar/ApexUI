namespace ApexUI.Layout;

/// Flexible gap for use in Row or Column.
/// Reports zero size during Measure so the container's natural size excludes it.
/// Row and Column detect Spacer instances and distribute leftover space equally among them.
public class Spacer : Widget
{
    public Spacer() { IsHitTestVisible = false; }

    protected override Size MeasureCore(Size available) => Size.Zero;
}
