namespace ApexUI.App.Examples;

/// All examples as tabs, each with its own vertical Scroll.
/// Tab bar also uses Scroll (horizontal, mouse-wheel scrollable).
public class TabsExample : Widget
{
    /// Bind to Application.UiScale.
    public Bindable<float> Scale { get; }

    /// Bind to Application.Theme (true = Dark).
    public Bindable<bool> DarkMode { get; }

    public TabsExample()
    {
        var scaleEx = new ScaleExample();
        Scale    = scaleEx.Scale;
        DarkMode = scaleEx.DarkMode;

        var tabs = new Tabs(TabPosition.Top)
            .AddTab("Counter",    new Scroll(new CounterExample()))
            .AddTab("Images",     new Scroll(new ImageToggleExample()))
            .AddTab("Sliders",    new Scroll(new SliderExample()))
            .AddTab("UI Scale",   new Scroll(scaleEx))
            .AddTab("Primitives", new Scroll(new PrimitivesExample()));

        AddChild(tabs);
    }
}
