namespace ApexUI.Layout;

/// Empty, invisible to the pointer, and growing: in a Row or Column it takes leftover space
/// (shared with other growing children by Grow). Any widget can grow; this one only has no content.
public class Spacer : Widget
{
    public Spacer(float grow = 1f)
    {
        Grow = grow;
        IsHitTestVisible = false;
    }
}
