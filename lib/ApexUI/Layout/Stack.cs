namespace ApexUI.Layout;

/// Children all occupy the same space, drawn in order (last = on top). This is Widget's default layout.
public class Stack : Widget
{
    public Stack(params Widget[] children)
    {
        foreach (var c in children) AddChild(c);
    }
}
