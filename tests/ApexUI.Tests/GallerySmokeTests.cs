using ApexUI.Gallery.Examples;

namespace ApexUI.Tests;

public class GallerySmokeTests
{
    public static TheoryData<string> ThemeNames => new() { "light", "dark" };

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Every_gallery_tab_lays_out_and_draws(string themeName)
    {
        var theme   = themeName == "dark" ? Theme.Dark : Theme.Light;
        var gallery = new TabsExample();
        var tabs    = Assert.IsType<Tabs>(Assert.Single(gallery.Children));

        Assert.True(tabs.TabCount > 0);
        for (int i = 0; i < tabs.TabCount; i++)
        {
            tabs.SelectedIndex = i;
            TestCanvas.Draw(gallery, 900, 700, theme);
        }
    }
}
