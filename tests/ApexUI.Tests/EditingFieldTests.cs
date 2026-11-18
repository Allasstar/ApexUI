namespace ApexUI.Tests;

public class EditableLabelTests
{
    private static (EditableLabel Label, UiHost Host, TestPlatform Platform) Show(string text = "Layer 1")
    {
        var label = new EditableLabel(text);
        var other = new Button("Other");
        var (host, platform) = TestPlatform.Show(new Column(label, other));
        platform.Render().Dispose();
        return (label, host, platform);
    }

    private static void DoubleClick(TestPlatform platform, float x, float y)
    {
        platform.Click(x, y);
        platform.Click(x, y);
    }

    [Fact]
    public void Double_click_starts_editing_with_the_text_selected()
    {
        var (label, host, platform) = Show();

        platform.Click(10, 5);
        Assert.False(label.IsEditing);
        platform.Click(10, 5);

        Assert.True(label.IsEditing);
        Assert.IsType<TextInput>(host.FocusedWidget);
        platform.Type("Sky");
        platform.Key(Key.Enter);
        Assert.Equal("Sky", label.Text);
    }

    [Fact]
    public void Enter_commits_and_reports_the_new_text()
    {
        var (label, _, platform) = Show();
        var commits = new List<string>();
        label.TextCommitted += commits.Add;

        DoubleClick(platform, 10, 5);
        platform.Type("Ground");
        platform.Key(Key.Enter);

        Assert.False(label.IsEditing);
        Assert.Equal("Ground", label.Text);
        Assert.Equal(["Ground"], commits);
    }

    [Fact]
    public void Escape_cancels()
    {
        var (label, _, platform) = Show();
        int commits = 0;
        label.TextCommitted += _ => commits++;

        DoubleClick(platform, 10, 5);
        platform.Type("Ground");
        platform.Key(Key.Escape);

        Assert.False(label.IsEditing);
        Assert.Equal("Layer 1", label.Text);
        Assert.Equal(0, commits);
    }

    [Fact]
    public void Losing_focus_commits()
    {
        var (label, host, platform) = Show();

        label.BeginEdit();
        platform.Type("Walls");
        host.ClearFocus();

        Assert.False(label.IsEditing);
        Assert.Equal("Walls", label.Text);
    }

    [Fact]
    public void Blank_text_is_not_committed()
    {
        var (label, _, platform) = Show();
        int commits = 0;
        label.TextCommitted += _ => commits++;

        label.BeginEdit();
        platform.Type("   ");
        platform.Key(Key.Enter);

        Assert.Equal("Layer 1", label.Text);
        Assert.Equal(0, commits);
    }
}

public class NumberFieldTests
{
    private static (NumberField Field, TestPlatform Platform) Show(float value = 10f)
    {
        var field = new NumberField("W", value) { Width = 200 };
        var (_, platform) = TestPlatform.Show(new Column(field, new Button("Other")));
        platform.Render().Dispose();
        return (field, platform);
    }

    private static void Scrub(TestPlatform platform, float dx, KeyModifiers modifiers = KeyModifiers.None)
    {
        platform.Listener.PointerMove(5, 10, modifiers);
        platform.Listener.PointerDown(5, 10, PointerButton.Left, modifiers);
        platform.Listener.PointerMove(5 + dx / 2, 10, modifiers);
        platform.Listener.PointerMove(5 + dx, 10, modifiers);
        platform.Listener.PointerUp(5 + dx, 10, PointerButton.Left, modifiers);
    }

    [Fact]
    public void Dragging_the_caption_scrubs_by_step_per_pixel()
    {
        var (field, platform) = Show();
        field.Step = 0.5f;
        var completed = new List<float>();
        field.EditCompleted += completed.Add;

        Scrub(platform, 20);

        Assert.Equal(20f, field.Value);
        Assert.Equal([20f], completed);
        Assert.False(field.IsScrubbing);
    }

    [Fact]
    public void Shift_scrubs_ten_times_faster_and_the_range_clamps()
    {
        var (field, platform) = Show();
        field.WithRange(0, 50);

        Scrub(platform, 3, KeyModifiers.Shift);
        Assert.Equal(40f, field.Value);

        Scrub(platform, -100);
        Assert.Equal(0f, field.Value);
    }

    [Fact]
    public void The_caption_shows_a_resize_cursor()
    {
        var (_, platform) = Show();

        platform.Move(5, 10);

        Assert.Equal(CursorShape.ResizeHorizontal, platform.Cursor);
    }

    [Fact]
    public void Typing_commits_on_enter_and_invalid_text_reverts()
    {
        var field = new NumberField("W", 10f) { Width = 200 };
        var (host, platform) = TestPlatform.Show(new Column(field));
        platform.Render().Dispose();
        var completed = new List<float>();
        field.EditCompleted += completed.Add;
        var input = field.Children[0].Children[1];

        input.Focus();
        platform.Key(Key.A, KeyModifiers.Control);
        platform.Type("42.5");
        Assert.Equal(10f, field.Value);
        platform.Key(Key.Enter);
        Assert.Equal(42.5f, field.Value);

        platform.Key(Key.A, KeyModifiers.Control);
        platform.Type("-");
        host.ClearFocus();
        Assert.Equal(42.5f, field.Value);
        Assert.Equal("42.5", ((TextInput)input).Value);
        Assert.Equal([42.5f], completed);
    }

    private static (NumberField Field, TextInput Input, UiHost Host, TestPlatform Platform) ShowWithUnits(float value = 10f)
    {
        var field = new NumberField("W", value) { Width = 200 }.WithUnits("px", "%");
        var (host, platform) = TestPlatform.Show(new Column(field, new Button("Other")));
        platform.Render().Dispose();
        return (field, (TextInput)field.Children[0].Children[1], host, platform);
    }

    private static void TypeAndSubmit(TestPlatform platform, TextInput input, string text)
    {
        input.Focus();
        platform.Key(Key.A, KeyModifiers.Control);
        platform.Type(text);
        platform.Key(Key.Enter);
    }

    [Fact]
    public void Typing_a_unit_suffix_switches_the_unit_before_the_value()
    {
        var (field, input, _, platform) = ShowWithUnits();
        var events = new List<string>();
        field.UnitChanged += unit => events.Add(unit);
        field.ValueChanged += value => events.Add(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        field.EditCompleted += _ => events.Add("done");

        TypeAndSubmit(platform, input, "50 %");

        Assert.Equal(["%", "50", "done"], events);
        Assert.Equal("%", field.Unit);
        Assert.Equal("50", input.Value);
    }

    [Fact]
    public void Typing_the_same_number_in_another_unit_still_completes_an_edit()
    {
        var (field, input, _, platform) = ShowWithUnits(50f);
        int completed = 0;
        field.EditCompleted += _ => completed++;

        TypeAndSubmit(platform, input, "50%");

        Assert.Equal(1, completed);
    }

    [Fact]
    public void An_unknown_suffix_keeps_the_value_and_unit()
    {
        var (field, input, _, platform) = ShowWithUnits();

        TypeAndSubmit(platform, input, "30em");

        Assert.Equal(10f, field.Value);
        Assert.Equal("px", field.Unit);
        Assert.Equal("10", input.Value);
    }

    [Fact]
    public void Clicking_the_unit_label_cycles_units_and_converts_the_value()
    {
        var (field, _, _, platform) = ShowWithUnits(200f);
        field.ConvertUnit = (value, from, to) => from == "px" && to == "%" ? value / 4f : value * 4f;
        var completed = new List<float>();
        field.EditCompleted += completed.Add;
        Rect unit = field.Children[0].Children[2].LayoutBounds;

        platform.Click(unit.CenterX, unit.CenterY);
        Assert.Equal("%", field.Unit);
        Assert.Equal(50f, field.Value);

        platform.Click(unit.CenterX, unit.CenterY);
        Assert.Equal("px", field.Unit);
        Assert.Equal(200f, field.Value);
        Assert.Equal([50f, 200f], completed);
    }

    [Fact]
    public void A_mixed_field_shows_no_number_until_an_edit_clears_it()
    {
        var (field, input, _, platform) = ShowWithUnits(10f);
        int completed = 0;
        field.EditCompleted += _ => completed++;

        field.IsMixed = true;
        Assert.Equal("", input.Value);

        TypeAndSubmit(platform, input, "10");
        Assert.False(field.IsMixed);
        Assert.Equal("10", input.Value);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void Edits_clear_mixed_before_the_value_changes()
    {
        var (field, input, _, platform) = ShowWithUnits(10f);
        var mixedAtChange = new List<bool>();
        field.ValueChanged += _ => mixedAtChange.Add(field.IsMixed);

        field.IsMixed = true;
        Scrub(platform, 4);
        field.IsMixed = true;
        TypeAndSubmit(platform, input, "3");

        Assert.NotEmpty(mixedAtChange);
        Assert.All(mixedAtChange, Assert.False);
    }

    [Fact]
    public void Leaving_a_mixed_field_untouched_completes_nothing()
    {
        var (field, input, host, _) = ShowWithUnits(10f);
        int completed = 0;
        field.EditCompleted += _ => completed++;
        field.IsMixed = true;

        input.Focus();
        host.ClearFocus();

        Assert.True(field.IsMixed);
        Assert.Equal(0, completed);
    }
}

public class ColorPickerTests
{
    [Theory]
    [InlineData("#FF8000", 255, 128, 0, 255)]
    [InlineData("ff800040", 255, 128, 0, 64)]
    [InlineData(" #00000000 ", 0, 0, 0, 0)]
    public void Hex_parses_rgb_and_rgba(string text, byte r, byte g, byte b, byte a)
    {
        Assert.True(ColorPicker.TryParseHex(text, out var color));
        Assert.Equal(new SKColor(r, g, b, a), color);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#FFF")]
    [InlineData("#GG0000")]
    public void Bad_hex_is_rejected(string text) => Assert.False(ColorPicker.TryParseHex(text, out _));

    [Fact]
    public void Hex_text_omits_alpha_when_opaque()
    {
        Assert.Equal("#5B9CFF", ColorPicker.ToHex(new SKColor(0x5B, 0x9C, 0xFF)));
        Assert.Equal("#5B9CFF80", ColorPicker.ToHex(new SKColor(0x5B, 0x9C, 0xFF, 0x80)));
    }

    [Fact]
    public void A_color_set_from_code_reads_back_exactly()
    {
        var picker = new ColorPicker();
        var color = new SKColor(0x12, 0x34, 0x56, 0x78);

        picker.Color = color;

        Assert.Equal(color, picker.Color);
    }

    [Fact]
    public void Dragging_in_the_square_sets_saturation_and_brightness_and_completes_once()
    {
        var picker = new ColorPicker(new SKColor(255, 0, 0));
        var (_, platform) = TestPlatform.Show(new Column(picker));
        platform.Render().Dispose();
        var changes = new List<SKColor>();
        var completed = new List<SKColor>();
        picker.ColorChanged += changes.Add;
        picker.EditCompleted += completed.Add;
        float side = Theme.Light.ColorAreaSize;

        platform.Move(side - 1, 1);
        platform.Press(side - 1, 1);
        platform.Move(side + 50, side + 50);
        platform.Release(side + 50, side + 50);

        Assert.NotEmpty(changes);
        Assert.Equal(SKColors.Black, picker.Color);
        Assert.Single(completed);
    }

    [Fact]
    public void Hue_survives_a_trip_through_gray()
    {
        var picker = new ColorPicker(new SKColor(0, 0, 255));
        var (_, platform) = TestPlatform.Show(new Column(picker));
        platform.Render().Dispose();
        float side = Theme.Light.ColorAreaSize;

        platform.Move(0, side / 2);
        platform.Press(0, side / 2);
        platform.Move(side + 50, -50);
        platform.Release(side + 50, -50);

        var c = picker.Color;
        Assert.True(c.Blue > 240 && c.Red < 10 && c.Green < 10, $"got {c}");
    }

    [Fact]
    public void Swatch_opens_a_picker_whose_changes_reach_the_swatch()
    {
        var swatch = new ColorSwatch(SKColors.Red);
        var (host, platform) = TestPlatform.Show(new Column(swatch));
        platform.Render().Dispose();
        var changes = new List<SKColor>();
        swatch.ColorChanged += changes.Add;

        platform.Click(5, 5);
        Assert.True(swatch.IsOpen);
        swatch.Color = SKColors.Blue;
        Assert.Equal([SKColors.Blue], changes);

        platform.Render().Dispose();
        platform.Click(700, 500);
        Assert.False(swatch.IsOpen);
    }

    [Fact]
    public void Read_only_swatch_does_not_open()
    {
        var swatch = new ColorSwatch(SKColors.Red).AsReadOnly();
        var (_, platform) = TestPlatform.Show(new Column(swatch));
        platform.Render().Dispose();

        platform.Click(5, 5);

        Assert.False(swatch.IsOpen);
        Assert.False(swatch.Focusable);
    }
}

public class ToastTests
{
    [Fact]
    public void Shows_at_the_bottom_center_and_hides_after_its_duration()
    {
        var toast = new Toast { Duration = TimeSpan.FromSeconds(2) };
        var (host, platform) = TestPlatform.Show(new Column(new Button("Under")));

        toast.Show(host, "Saved");
        platform.Render().Dispose();

        var panel = toast.Content!.LayoutBounds;
        Assert.True(toast.IsVisible);
        Assert.Equal(400f, panel.CenterX, 1);
        Assert.True(panel.Bottom < 600f && panel.Bottom > 500f);

        platform.Advance(TimeSpan.FromSeconds(1.9));
        Assert.True(toast.IsVisible);
        platform.Advance(TimeSpan.FromSeconds(0.2));
        Assert.False(toast.IsVisible);
        Assert.Equal(0, platform.ActiveTimers);
    }

    [Fact]
    public void A_new_message_replaces_the_old_and_restarts_the_timer()
    {
        var toast = new Toast { Duration = TimeSpan.FromSeconds(2) };
        var (host, platform) = TestPlatform.Show(new Column());

        toast.Show(host, "One");
        platform.Advance(TimeSpan.FromSeconds(1.5));
        toast.Show(host, "Two");
        platform.Advance(TimeSpan.FromSeconds(1.5));

        Assert.True(toast.IsVisible);
        Assert.Equal("Two", toast.Message);
    }

    [Fact]
    public void Top_placement_sits_the_edge_offset_below_the_top()
    {
        var toast = new Toast { Placement = ToastPlacement.Top, EdgeOffset = 48 };
        var (host, platform) = TestPlatform.Show(new Column());

        toast.Show(host, "Saved");
        platform.Render().Dispose();

        var panel = toast.Content!.LayoutBounds;
        Assert.Equal(400f, panel.CenterX, 1);
        Assert.Equal(48f, panel.Y, 3);
    }

    [Fact]
    public void Edge_offset_moves_a_bottom_toast_too()
    {
        var toast = new Toast { EdgeOffset = 100 };
        var (host, platform) = TestPlatform.Show(new Column());

        toast.Show(host, "Saved");
        platform.Render().Dispose();

        Assert.Equal(500f, toast.Content!.LayoutBounds.Bottom, 3);
    }

    [Fact]
    public void Never_takes_pointer_input()
    {
        var button = new Button("Under") { Width = 800, Height = 600 };
        int clicks = 0;
        button.Pressed += () => clicks++;
        var toast = new Toast();
        var (host, platform) = TestPlatform.Show(button);
        toast.Show(host, "Saved");
        platform.Render().Dispose();
        var panel = toast.Content!.LayoutBounds;

        platform.Click(panel.CenterX, panel.CenterY);

        Assert.Equal(1, clicks);
        Assert.True(toast.IsVisible);
    }
}

public class SpacingSourceTests
{
    [Fact]
    public void Spacing_source_follows_the_theme_until_spacing_is_set()
    {
        var row = new Row(new FixedBox(10, 10), new FixedBox(10, 10)) { SpacingSource = t => t.SpacingLg };
        var (host, platform) = TestPlatform.Show(new Column(row));
        platform.Render().Dispose();
        Assert.Equal(20 + Theme.Light.SpacingLg, row.DesiredSize.Width);

        host.Theme = Theme.Light with { SpacingLg = 40 };
        platform.Render().Dispose();
        Assert.Equal(60, row.DesiredSize.Width);

        row.Spacing = 2;
        platform.Render().Dispose();
        Assert.Equal(22, row.DesiredSize.Width);
    }
}

public class FieldWidthTests
{
    [Fact]
    public void Text_input_offered_unlimited_width_asks_for_its_min_width()
    {
        var input = new TextInput { MinWidth = 60 };

        input.Measure(new Size(float.PositiveInfinity, 100));

        Assert.Equal(60, input.DesiredSize.Width, 3);
    }

    [Fact]
    public void Two_growing_number_fields_share_a_row()
    {
        var a = new NumberField("A") { Grow = 1 };
        var b = new NumberField("B") { Grow = 1 };
        var row = new Row(a, b);

        row.Measure(new Size(400, 100));
        row.Arrange(new Rect(0, 0, 400, 100));

        Assert.InRange(a.LayoutBounds.Width, 190, 210);
        Assert.Equal(a.LayoutBounds.Right, b.LayoutBounds.X, 3);
        Assert.Equal(400, b.LayoutBounds.Right, 3);
    }
}

public class ToastThemeTests
{
    [Fact]
    public void The_gap_below_the_toast_follows_the_theme()
    {
        var toast = new Toast();
        var (host, platform) = TestPlatform.Show(new Column());
        toast.Show(host, "Saved");
        platform.Render().Dispose();
        Assert.Equal(600 - Theme.Light.SpacingLg, toast.Content!.LayoutBounds.Bottom, 3);

        host.Theme = Theme.Light with { SpacingLg = 50 };
        platform.Render().Dispose();

        Assert.Equal(550, toast.Content!.LayoutBounds.Bottom, 3);
    }
}
