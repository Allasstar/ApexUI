namespace ApexUI.Gallery.Examples;

public class TabsExample : Widget
{
    public Bindable<float>  Scale      { get; }
    public Bindable<bool>   DarkMode   { get; }
    public Bindable<string> FontFamily { get; }
    public Bindable<string> Preset     { get; }

    public TabsExample(ThemeRegistry themes)
    {
        var settings = new SettingsExample(themes);
        Scale      = settings.Scale;
        DarkMode   = settings.DarkMode;
        FontFamily = settings.FontFamily;
        Preset     = settings.Preset;

        var tabs = new Tabs(TabPosition.Top)
            .AddTab("Counter",    new Scroll(new CounterExample()))
            .AddTab("Images",     new Scroll(new ImageToggleExample()))
            .AddTab("Sliders",    new Scroll(new SliderExample()))
            .AddTab("Forms",      new Scroll(new ScaleExample()))
            .AddTab("Primitives", new Scroll(new PrimitivesExample()))
            .AddTab("Layout",     new Scroll(new LayoutExample()))
            .AddTab("Viewport",   new ViewportExample())
            .AddTab("Icons",      new Scroll(new IconsExample()))
            .AddTab("Panels",     new PanelsExample())
            .AddTab("Inputs",     new Scroll(new InputsExample()))
            .AddTab("Composite",   new Scroll(new CompositeExample()))
            .AddTab("Collections", new Scroll(new CollectionExample()))
            .AddTab("Lists",       new Scroll(new ListsExample()))
            .AddTab("Colors",      new Scroll(new ColorsExample()))
            .AddTab("Settings",    new Scroll(settings));

        AddChild(tabs);
    }
}
