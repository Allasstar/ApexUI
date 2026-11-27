namespace ApexUI.Tests;

public class ResetHookTests
{
    private static void RightClick(TestPlatform platform, float x, float y)
        => platform.Click(x, y, PointerButton.Right);

    private static (UiHost Host, TestPlatform Platform) Show(Widget root)
    {
        root.HAlign = HAlign.Left;
        root.VAlign = VAlign.Top;
        var pair = TestPlatform.Show(root);
        pair.Platform.Render().Dispose();
        return pair;
    }

    // Menu opens at (x, y); PopupPadding is 4, first item center is ~15 px below that.
    private static void ClickFirstMenuItem(TestPlatform platform, float menuX, float menuY)
    {
        platform.Render().Dispose();
        platform.Click(menuX + 24f, menuY + 4f + 15f);
    }

    private static void ClickSecondMenuItem(TestPlatform platform, float menuX, float menuY)
    {
        platform.Render().Dispose();
        platform.Click(menuX + 24f, menuY + 4f + 31.6f + 2f + 15f);
    }

    [Fact]
    public void Right_click_on_value_widget_opens_reset_menu_and_resets_when_not_default()
    {
        var field = new NumberField("X", 42f);
        field.WithReset(() => field.Value == 0f, () => field.Value = 0f);
        var (host, platform) = Show(field);

        float rx = field.LayoutBounds.CenterX, ry = field.LayoutBounds.CenterY;
        RightClick(platform, rx, ry);
        Assert.Equal(1, host.OpenOverlayCount);

        ClickFirstMenuItem(platform, rx, ry);
        Assert.Equal(0f, field.Value);
        Assert.Equal(0, host.OpenOverlayCount);
    }

    [Fact]
    public void Reset_item_is_disabled_at_default_and_re_enables_after_value_changes()
    {
        int resets = 0;
        var slider = new Slider().WithValue(0.5f);
        slider.WithReset(() => slider.Value == 0.5f, () => { resets++; slider.Value = 0.5f; });
        var (host, platform) = Show(slider);

        float rx = slider.LayoutBounds.CenterX, ry = slider.LayoutBounds.CenterY;
        RightClick(platform, rx, ry);
        Assert.Equal(1, host.OpenOverlayCount);

        // At default: clicking Reset does nothing (item is disabled).
        ClickFirstMenuItem(platform, rx, ry);
        Assert.Equal(0, resets);

        // Close menu, change value, right-click again: Reset now works.
        platform.Click(390, 290);
        slider.Value = 0.9f;
        RightClick(platform, rx, ry);
        ClickFirstMenuItem(platform, rx, ry);
        Assert.Equal(1, resets);
        Assert.Equal(0.5f, slider.Value);
    }

    [Fact]
    public void Right_click_on_tied_label_opens_target_reset_menu()
    {
        var toggle = new Toggle(isChecked: true);
        toggle.WithReset(() => !toggle.IsChecked, () => toggle.IsChecked = false);
        var label = new Label { Text = "Active" }.For(toggle);
        var row = new Row(label, toggle).WithSpacing(8f);
        var (host, platform) = Show(row);

        float rx = label.LayoutBounds.CenterX, ry = label.LayoutBounds.CenterY;
        RightClick(platform, rx, ry);
        Assert.Equal(1, host.OpenOverlayCount);

        ClickFirstMenuItem(platform, rx, ry);
        Assert.False(toggle.IsChecked);
    }

    [Fact]
    public void Right_click_on_label_tied_to_widget_without_reset_opens_no_menu()
    {
        var input = new TextInput().WithValue("hello");
        var label = new Label { Text = "Name" }.For(input);
        var (host, platform) = Show(new Row(label, input));

        RightClick(platform, label.LayoutBounds.CenterX, label.LayoutBounds.CenterY);
        Assert.Equal(0, host.OpenOverlayCount);
    }

    [Fact]
    public void Host_can_add_extra_items_to_reset_menu()
    {
        int copied = 0;
        var swatch = new ColorSwatch(SKColors.Red);
        swatch.WithReset(
            () => swatch.Color == SKColors.Black,
            () => swatch.Color = SKColors.Black,
            menu => menu.AddItem("Copy hex", () => copied++));
        var (_, platform) = Show(swatch);

        float rx = swatch.LayoutBounds.CenterX, ry = swatch.LayoutBounds.CenterY;
        RightClick(platform, rx, ry);
        ClickSecondMenuItem(platform, rx, ry);
        Assert.Equal(1, copied);
    }

    [Fact]
    public void Label_with_own_reset_resets_both_values_in_a_two_value_row()
    {
        var x = new NumberField("X", 2f);
        var y = new NumberField("Y", 3f);
        x.WithReset(() => x.Value == 1f, () => x.Value = 1f);
        y.WithReset(() => y.Value == 1f, () => y.Value = 1f);
        var label = new Label { Text = "Scale" }.WithReset(
            () => x.Value == 1f && y.Value == 1f,
            () => { x.Value = 1f; y.Value = 1f; });
        var (_, platform) = Show(new Row(label, x, y).WithSpacing(4f));

        float rx = label.LayoutBounds.CenterX, ry = label.LayoutBounds.CenterY;
        RightClick(platform, rx, ry);
        ClickFirstMenuItem(platform, rx, ry);
        Assert.Equal(1f, x.Value);
        Assert.Equal(1f, y.Value);
    }

    [Fact]
    public void All_value_widgets_support_WithReset()
    {
        var text = new TextInput().WithValue("b").WithReset(() => false, () => { });
        var check = new Checkbox(true).WithReset(() => false, () => { });
        var group = new ToggleGroup<int>().AddOption(1, "One").AddOption(2, "Two").WithSelected(2).WithReset(() => false, () => { });
        var drop = new Dropdown<int>().AddItem(1, "One").AddItem(2, "Two").WithSelected(2).WithReset(() => false, () => { });
        var dir = new DirectionPicker().WithDirection(90f, 30f).WithReset(() => false, () => { });

        Widget[] widgets = [text, check, group, drop, dir];
        foreach (Widget w in widgets)
        {
            var (host, platform) = Show(w);
            RightClick(platform, w.LayoutBounds.CenterX, w.LayoutBounds.CenterY);
            Assert.Equal(1, host.OpenOverlayCount);
        }
    }

    [Fact]
    public void Section_header_accepts_reset_hook_without_triggering_on_body()
    {
        int resets = 0;
        var body = new FixedBox(120, 60);
        var section = new Section("Transform", body);
        ResetHook.Attach(section.Header, () => false, () => resets++, "Reset section");
        var (host, platform) = Show(section);

        // Right-clicking body opens no menu.
        RightClick(platform, body.LayoutBounds.CenterX, body.LayoutBounds.CenterY);
        Assert.Equal(0, host.OpenOverlayCount);

        // Right-clicking header opens Reset section menu.
        float hx = section.Header.LayoutBounds.CenterX, hy = section.Header.LayoutBounds.CenterY;
        RightClick(platform, hx, hy);
        Assert.Equal(1, host.OpenOverlayCount);
        ClickFirstMenuItem(platform, hx, hy);
        Assert.Equal(1, resets);
    }

    [Fact]
    public void ContextMenu_dynamic_enabled_updates_each_time_it_opens()
    {
        bool canRun = false;
        int runs = 0;
        var box = new FixedBox(80, 40);
        var menu = new ContextMenu().AddItem("Action", () => runs++, () => canRun);
        ContextMenu.Attach(box, menu);
        var (_, platform) = Show(box);

        RightClick(platform, 20, 20);
        ClickFirstMenuItem(platform, 20, 20);
        Assert.Equal(0, runs);

        platform.Click(300, 200);
        canRun = true;
        RightClick(platform, 20, 20);
        ClickFirstMenuItem(platform, 20, 20);
        Assert.Equal(1, runs);
    }
}
