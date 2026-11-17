namespace ApexUI.Tests;

public class MenuButtonTests
{
    private static (MenuButton Button, UiHost Host, TestPlatform Platform) Show(MenuButton button)
    {
        button.HAlign = HAlign.Left;
        button.VAlign = VAlign.Top;
        var (host, platform) = TestPlatform.Show(button);
        platform.Render().Dispose();
        return (button, host, platform);
    }

    // The menu opens right below the button: popup padding 4, then the first item, 31.6 high.
    private static float FirstItemY(MenuButton button) => button.LayoutBounds.Bottom + 4f + 15f;

    [Fact]
    public void Pressing_opens_the_menu_and_pressing_again_closes_it()
    {
        var (button, host, platform) = Show(new MenuButton("Shapes").AddItem("Rectangle", () => { }));

        platform.Click(10, 10);
        Assert.True(button.IsOpen);
        Assert.Equal(1, host.OpenOverlayCount);

        platform.Render().Dispose();
        platform.Click(10, 10);
        Assert.False(button.IsOpen);
        Assert.Equal(0, host.OpenOverlayCount);
    }

    [Fact]
    public void Clicking_an_item_runs_it_and_closes_the_menu()
    {
        int rectangles = 0;
        var (button, host, platform) = Show(new MenuButton("Shapes")
            .AddItem("Rectangle", () => rectangles++)
            .AddItem("Ellipse", () => { }));

        platform.Click(10, 10);
        platform.Render().Dispose();
        platform.Click(30, FirstItemY(button));

        Assert.Equal(1, rectangles);
        Assert.False(button.IsOpen);
        Assert.Equal(0, host.OpenOverlayCount);
    }

    [Fact]
    public void A_disabled_item_does_nothing()
    {
        int runs = 0;
        var (button, _, platform) = Show(new MenuButton("Shapes").AddItem("Rectangle", () => runs++, enabled: false));

        platform.Click(10, 10);
        platform.Render().Dispose();
        platform.Click(30, FirstItemY(button));

        Assert.Equal(0, runs);
    }

    [Theory]
    [InlineData(Key.Down)]
    [InlineData(Key.Enter)]
    [InlineData(Key.Space)]
    public void Keys_open_the_menu_when_focused(Key key)
    {
        var (button, _, platform) = Show(new MenuButton("Shapes").AddItem("Rectangle", () => { }));
        button.Focus();

        platform.Key(key);

        Assert.True(button.IsOpen);
    }

    [Fact]
    public void Items_added_after_the_first_open_appear_on_the_next_open()
    {
        int ellipses = 0;
        var (button, _, platform) = Show(new MenuButton("Shapes"));
        platform.Click(10, 10);
        button.Close();

        button.AddItem("Ellipse", () => ellipses++);
        platform.Click(10, 10);
        platform.Render().Dispose();
        platform.Click(30, FirstItemY(button));

        Assert.Equal(1, ellipses);
    }
}
