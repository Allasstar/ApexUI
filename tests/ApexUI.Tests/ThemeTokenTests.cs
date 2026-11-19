using ApexUI.Extensions;

namespace ApexUI.Tests;

public class ThemeRegistryTests
{
    [Fact]
    public void Starts_with_the_built_in_themes_in_order()
    {
        var themes = new ThemeRegistry();

        Assert.Equal(
            [BuiltInThemes.Default, BuiltInThemes.Contrast, BuiltInThemes.Forest, BuiltInThemes.Desert, BuiltInThemes.Space],
            themes.Names);
        Assert.Same(Theme.Dark, themes.Get(BuiltInThemes.Default, dark: true));
    }

    [Fact]
    public void Replacing_a_theme_keeps_its_position()
    {
        var themes = new ThemeRegistry(includeBuiltIns: false);
        var light  = new Theme { Primary = SKColors.Red };
        themes.Register("A", Theme.Light, Theme.Dark);
        themes.Register("B", Theme.Light, Theme.Dark);

        themes.Register("A", light, Theme.Dark);

        Assert.Equal(["A", "B"], themes.Names);
        Assert.Same(light, themes.Get("a", dark: false));
    }

    [Fact]
    public void Unknown_names_fall_back_to_the_first_theme()
    {
        var themes = new ThemeRegistry();
        themes.Remove(BuiltInThemes.Default);

        Assert.Equal(BuiltInThemes.Contrast, themes.DefaultName);
        Assert.Same(themes.Get(BuiltInThemes.Contrast, dark: true), themes.Get("missing", dark: true));
    }

    [Fact]
    public void Two_hosts_have_separate_registries()
    {
        var (first, _)  = TestPlatform.Show(new FixedBox(1, 1));
        var (second, _) = TestPlatform.Show(new FixedBox(1, 1));

        first.Themes.Register("Custom", Theme.Light, Theme.Dark);

        Assert.Contains("Custom", first.Themes.Names);
        Assert.DoesNotContain("Custom", second.Themes.Names);
    }

    [Fact]
    public void Host_binds_theme_dark_mode_and_font_family_together()
    {
        var custom = new Theme { Primary = SKColors.Red };
        var (host, _) = TestPlatform.Show(new FixedBox(1, 1));
        host.Themes.Register("Custom", custom, custom with { Background = SKColors.Black });

        var preset = new Bindable<string>("Custom");
        var dark   = new Bindable<bool>(false);
        var font   = new Bindable<string>("Arial");
        host.BindTheme(preset).BindDarkMode(dark).BindFontFamily(font);

        Assert.Equal(SKColors.Red, host.Theme.Primary);
        Assert.Equal("Arial", host.Theme.FontFamily);

        dark.Value = true;

        Assert.Equal(SKColors.Black, host.Theme.Background);
        Assert.Equal("Arial", host.Theme.FontFamily);
    }
}

public class ThemeTokenTests
{
    [Fact]
    public void Derived_colors_follow_their_base_token_unless_set()
    {
        var theme = Theme.Light with { Primary = SKColors.Red, Border = SKColors.Blue };

        Assert.Equal(SKColors.Red, theme.FocusRing);
        Assert.Equal(SKColors.Red.WithAlpha(0.3f), theme.Selection);
        Assert.Equal(SKColors.Blue.WithAlpha(0.4f), theme.BorderSubtle);

        var set = theme with { FocusRing = SKColors.Green };
        Assert.Equal(SKColors.Green, set.FocusRing);
    }

    [Fact]
    public void Widget_padding_and_radius_come_from_the_theme_until_set()
    {
        var button = new Button("OK") { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (host, platform) = TestPlatform.Show(new Column(button));
        platform.Render().Dispose();

        Assert.Equal(Theme.Light.ButtonPadding, button.Padding);
        Assert.Equal(Theme.Light.CornerRadiusControl, button.CornerRadius);
        float width = button.LayoutBounds.Width;

        host.Theme = Theme.Light with { ButtonPadding = new Thickness(30f, 8f), CornerRadiusControl = 2f };
        platform.Render().Dispose();

        Assert.Equal(width + 28f, button.LayoutBounds.Width, 3);
        Assert.Equal(2f, button.CornerRadius);

        button.Padding      = new Thickness(1f);
        button.CornerRadius = 0f;
        host.Theme          = Theme.Dark;

        Assert.Equal(new Thickness(1f), button.Padding);
        Assert.Equal(0f, button.CornerRadius);
    }

    [Fact]
    public void Control_sizes_come_from_the_theme()
    {
        var toggle   = new Toggle { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var checkbox = new Checkbox { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (host, platform) = TestPlatform.Show(new Column(toggle, checkbox));

        host.Theme = Theme.Light with { ToggleWidth = 60f, ToggleHeight = 30f, CheckboxSize = 22f };
        platform.Render().Dispose();

        Assert.Equal(new Rect(0, 0, 60f, 30f), toggle.LayoutBounds);
        Assert.Equal(22f, checkbox.LayoutBounds.Width);
        Assert.Equal(22f, checkbox.LayoutBounds.Height);
    }

    [Fact]
    public void Label_role_picks_the_theme_font_size_and_line_height()
    {
        var label = new Label { Text = "Title", Role = TextRole.Title, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (host, platform) = TestPlatform.Show(new Column(label));
        host.Theme = Theme.Light with { FontSizeTitle = 30f, LineHeight = 1.5f };
        platform.Render().Dispose();

        Assert.Equal(45f, label.LayoutBounds.Height, 3);

        label.FontSize = 10f;
        platform.Render().Dispose();

        Assert.Equal(15f, label.LayoutBounds.Height, 3);
    }

    [Fact]
    public void Label_font_size_source_wins_over_size_and_role()
    {
        var label = new Label { Text = "Hint", FontSize = 10f, Role = TextRole.Title, FontSizeSource = t => t.FontSizeSmall * 2f, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (host, platform) = TestPlatform.Show(new Column(label));
        host.Theme = Theme.Light with { FontSizeSmall = 11f, LineHeight = 1.5f };
        platform.Render().Dispose();

        Assert.Equal(33f, label.LayoutBounds.Height, 3);
    }

    [Fact]
    public void Key_hint_tokens_follow_the_small_font_muted_text_and_border_unless_set()
    {
        var theme = Theme.Dark with { FontSizeSmall = 11f, OnSurfaceMuted = SKColors.Gray, Border = SKColors.Blue };

        Assert.Equal(11f, theme.KeyHintFontSize);
        Assert.Equal(SKColors.Gray, theme.KeyHintText);
        Assert.Equal(SKColors.Blue, theme.KeyHintBorder);

        var set = theme with { KeyHintFontSize = 15f, KeyHintText = SKColors.White, KeyHintBorder = SKColors.Red };
        Assert.Equal(15f, set.KeyHintFontSize);
        Assert.Equal(SKColors.White, set.KeyHintText);
        Assert.Equal(SKColors.Red, set.KeyHintBorder);
    }

    [Fact]
    public void Key_hint_text_and_chip_grow_with_the_key_hint_font_size()
    {
        var hint = new KeyHint("Shift", "Add") { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (host, platform) = TestPlatform.Show(new Column(hint));
        host.Theme = Theme.Light with { LineHeight = 1.5f };
        platform.Render().Dispose();
        Widget chip = hint.Children[0];
        float chipWidth = chip.LayoutBounds.Width;
        Assert.Equal(Theme.Light.FontSizeSmall * 1.5f, hint.LayoutBounds.Height, 3);
        Assert.Equal(Theme.Light.FontSizeSmall * 0.5f, chip.Padding.Left, 3);

        host.Theme = Theme.Light with { LineHeight = 1.5f, KeyHintFontSize = Theme.Light.FontSizeSmall * 1.25f };
        platform.Render().Dispose();

        Assert.Equal(Theme.Light.FontSizeSmall * 1.25f * 1.5f, hint.LayoutBounds.Height, 3);
        Assert.Equal(Theme.Light.FontSizeSmall * 1.25f * 0.5f, chip.Padding.Left, 3);
        Assert.True(chip.LayoutBounds.Width > chipWidth * 1.2f);
    }

    [Fact]
    public void Panel_spacing_comes_from_the_theme_until_set()
    {
        var group = new RadioGroup<int>(Orientation.Horizontal);
        var (host, _) = TestPlatform.Show(group);
        host.Theme = Theme.Light with { SpacingSm = 13f };

        Assert.Equal(13f, group.Spacing);

        group.Spacing = 2f;
        Assert.Equal(2f, group.Spacing);
    }
}
