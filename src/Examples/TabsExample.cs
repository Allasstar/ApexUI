namespace ApexUI.App.Examples;

public class TabsExample : Widget
{
    public Bindable<float>  Scale      { get; }
    public Bindable<bool>   DarkMode   { get; }
    public Bindable<string> FontFamily { get; }

    public TabsExample()
    {
        var settings = new SettingsExample();
        Scale      = settings.Scale;
        DarkMode   = settings.DarkMode;
        FontFamily = settings.FontFamily;

        var tabs = new Tabs(TabPosition.Top)
            .AddTab("Counter",    new Scroll(new CounterExample()))
            .AddTab("Images",     new Scroll(new ImageToggleExample()))
            .AddTab("Sliders",    new Scroll(new SliderExample()))
            .AddTab("Forms",      new Scroll(new ScaleExample()))
            .AddTab("Primitives", new Scroll(new PrimitivesExample()))
            .AddTab("Layout",     new Scroll(new LayoutExample()))
            .AddTab("Inputs",     new Scroll(new InputsExample()))
            .AddTab("Composite",  new Scroll(new CompositeExample()))
            .AddTab("Settings",   new Scroll(settings));

        AddChild(tabs);
    }
}
