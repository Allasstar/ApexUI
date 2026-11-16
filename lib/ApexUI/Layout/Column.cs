namespace ApexUI.Layout;

/// Children arranged top-to-bottom with optional spacing. See LinearPanel.
public class Column : LinearPanel
{
    public Column(params Widget[] children) : base(Orientation.Vertical)
    {
        foreach (var c in children) AddChild(c);
    }

    public Column Add(Widget child) { AddChild(child); return this; }

    public Column WithSpacing(float spacing) { Spacing = spacing; return this; }
}
