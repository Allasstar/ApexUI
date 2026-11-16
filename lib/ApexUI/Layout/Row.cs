namespace ApexUI.Layout;

/// Children arranged left-to-right with optional spacing. See LinearPanel.
public class Row : LinearPanel
{
    public Row(params Widget[] children) : base(Orientation.Horizontal)
    {
        foreach (var c in children) AddChild(c);
    }

    public Row Add(Widget child) { AddChild(child); return this; }

    public Row WithSpacing(float spacing) { Spacing = spacing; return this; }
}
