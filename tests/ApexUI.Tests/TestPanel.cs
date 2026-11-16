namespace ApexUI.Tests;

/// Test container with public Add/Remove; lays children over each other like Stack.
internal sealed class TestPanel : Widget
{
    public TestPanel(params Widget[] children)
    {
        foreach (var c in children) AddChild(c);
    }

    public void Add(Widget child)    => AddChild(child);
    public void Remove(Widget child) => RemoveChild(child);
}
