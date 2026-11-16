namespace ApexUI.Layout;

/// Wraps a single child with padding on all sides. Shorthand for a widget with Padding set;
/// every widget applies its own Padding.
public class PaddingBox : Widget
{
    public PaddingBox(Widget child, Thickness padding)
    {
        Padding = padding;
        AddChild(child);
    }

    public PaddingBox(Widget child, float all)
        : this(child, new Thickness(all)) { }
}
