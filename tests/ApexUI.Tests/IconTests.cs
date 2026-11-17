namespace ApexUI.Tests;

public class IconTests
{
    [Fact]
    public void Icon_takes_the_theme_size_until_set()
    {
        var icon = new Icon("M0 0H24V24Z");
        icon.Measure(Size.Infinite);
        Assert.Equal(new Size(Theme.Light.IconSize, Theme.Light.IconSize), icon.DesiredSize);

        icon.Size = 32;
        icon.Measure(Size.Infinite);
        Assert.Equal(new Size(32, 32), icon.DesiredSize);
    }

    [Fact]
    public void Icon_releases_its_path_when_it_leaves_the_screen()
    {
        var icon  = new Icon("M0 0H24V24Z");
        var panel = new TestPanel(icon);
        var (_, platform) = TestPlatform.Show(panel);
        platform.Render().Dispose();
        Assert.True(icon.HasParsedPath);

        panel.Remove(icon);

        Assert.False(icon.HasParsedPath);
    }

    [Fact]
    public void Invalid_path_data_draws_nothing()
    {
        var icon = new Icon("not a path", IconStyle.Stroke);
        TestCanvas.Draw(icon, 32, 32);
        Assert.False(icon.HasParsedPath);
    }

    [Fact]
    public void Icon_button_presses_on_click_and_activation_keys()
    {
        int presses = 0;
        var button  = new IconButton("M0 0H24V24Z", () => presses++);
        var (host, platform) = TestPlatform.Show(new Row(button));
        platform.Render().Dispose();

        var b = button.WindowBounds;
        platform.Click(b.CenterX, b.CenterY);
        host.MoveFocus(forward: true);
        platform.Key(Key.Space);

        Assert.Equal(2, presses);
    }

    [Fact]
    public void Icon_button_is_its_icon_plus_padding()
    {
        var t      = Theme.Light;
        var button = new IconButton("M0 0H24V24Z");
        button.Measure(Size.Infinite);

        Assert.Equal(t.IconSize + t.IconButtonPadding.Horizontal, button.DesiredSize.Width);
        Assert.Equal(t.IconSize + t.IconButtonPadding.Vertical, button.DesiredSize.Height);
    }

    [Fact]
    public void Disabled_icon_button_ignores_clicks()
    {
        int presses = 0;
        var button  = new IconButton("M0 0H24V24Z", () => presses++) { IsEnabled = false };
        var (_, platform) = TestPlatform.Show(new Row(button));
        platform.Render().Dispose();

        var b = button.WindowBounds;
        platform.Click(b.CenterX, b.CenterY);

        Assert.Equal(0, presses);
    }

    [Fact]
    public void Selected_icon_button_shows_its_icon_in_the_on_primary_color_and_redraws()
    {
        var t      = Theme.Light;
        var button = new IconButton("M0 0H24V24Z");
        var (_, platform) = TestPlatform.Show(new Row(button));
        platform.Render().Dispose();
        int frames = platform.FramesRequested;

        button.IsSelected = true;

        Assert.Equal(t.OnPrimary, button.Icon.ColorSource!(t));
        Assert.True(platform.FramesRequested > frames);
    }
}
