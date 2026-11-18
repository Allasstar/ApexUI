namespace ApexUI.Tests;

public class ToolbarBlockTests
{
    [Fact]
    public void Button_group_lays_children_in_a_row_inside_its_padding()
    {
        var a = new FixedBox(20, 10);
        var b = new FixedBox(30, 10);
        var group = new ButtonGroup(a, b);
        var t = Theme.Light;
        float pad = t.SpacingXs * 0.5f, gap = t.SpacingXs * 0.5f;

        group.Measure(Size.Infinite);
        group.Arrange(new Rect(0, 0, group.DesiredSize.Width, group.DesiredSize.Height));

        Assert.Equal(new Size(20 + gap + 30 + 2 * pad, 10 + 2 * pad), group.DesiredSize);
        Assert.Equal(new Rect(pad, pad, 20, 10), a.LayoutBounds);
        Assert.Equal(pad + 20 + gap, b.LayoutBounds.X);
        Assert.Equal(t.CornerRadiusControl, group.CornerRadius);
    }

    [Fact]
    public void Vertical_button_group_stacks_children()
    {
        var a = new FixedBox(20, 10);
        var b = new FixedBox(20, 12);
        var group = new ButtonGroup(Orientation.Vertical, a, b);

        group.Measure(Size.Infinite);
        group.Arrange(new Rect(0, 0, group.DesiredSize.Width, group.DesiredSize.Height));

        Assert.True(b.LayoutBounds.Y >= a.LayoutBounds.Bottom);
        Assert.Equal(a.LayoutBounds.X, b.LayoutBounds.X);
    }

    [Fact]
    public void Button_group_children_stay_clickable()
    {
        int presses = 0;
        var button = new IconButton("M4 4H20V20H4Z", () => presses++);
        var (_, platform) = TestPlatform.Show(new Row(new ButtonGroup(button)));
        platform.Render().Dispose();

        var bounds = button.WindowBounds;
        platform.Click(bounds.CenterX, bounds.CenterY);

        Assert.Equal(1, presses);
    }

    [Fact]
    public void Button_group_draws_in_both_themes()
    {
        var group = new ButtonGroup(new IconButton("M4 4H20V20H4Z"), new Label { Text = "100%" }).WithChildrenCentered();
        TestCanvas.Draw(group, 200, 40, Theme.Light);
        TestCanvas.Draw(group, 200, 40, Theme.Dark);
        Assert.Equal(VAlign.Center, group.Children[1].VAlign);
    }

    [Fact]
    public void Key_hint_puts_the_chip_before_the_text()
    {
        var hint = new KeyHint("Shift", "Add");
        hint.Measure(Size.Infinite);
        hint.Arrange(new Rect(0, 0, hint.DesiredSize.Width, hint.DesiredSize.Height));

        Widget chip = hint.Children[0], label = hint.Children[1];
        Assert.True(chip.LayoutBounds.Width > 0);
        Assert.Equal(chip.LayoutBounds.Right + Theme.Light.SpacingXs, label.LayoutBounds.X, 3);
        Assert.Equal(chip.LayoutBounds.Width + Theme.Light.SpacingXs + label.DesiredSize.Width, hint.DesiredSize.Width, 3);
    }

    [Fact]
    public void Key_hint_text_change_relays_out_but_highlight_only_redraws()
    {
        var hint = new KeyHint("Shift", "Add");
        var (_, platform) = TestPlatform.Show(new Row(hint));
        platform.Render().Dispose();

        hint.IsHighlighted = true;
        Assert.False(hint.IsLayoutDirty);

        float before = hint.DesiredSize.Width;
        hint.Set("Ctrl", "Deepest of all", KeyHintKind.Key, highlighted: true);
        Assert.True(hint.IsLayoutDirty);
        platform.Render().Dispose();
        Assert.True(hint.DesiredSize.Width > before);
        Assert.Equal("Ctrl", hint.Key);
        Assert.Equal("Deepest of all", hint.Text);
    }

    [Fact]
    public void Highlighting_a_key_hint_asks_for_a_frame()
    {
        var hint = new KeyHint("Alt", "Measure");
        var (_, platform) = TestPlatform.Show(new Row(hint));
        platform.Render().Dispose();
        int before = platform.FramesRequested;

        hint.IsHighlighted = true;

        Assert.True(platform.FramesRequested > before);
    }

    [Fact]
    public void Key_hint_draws_key_and_mouse_chips_plain_and_highlighted()
    {
        foreach (var kind in (KeyHintKind[])[KeyHintKind.Key, KeyHintKind.Mouse])
        {
            foreach (bool highlighted in (bool[])[false, true])
            {
                var hint = new KeyHint("LMB", "Select", kind) { IsHighlighted = highlighted };
                TestCanvas.Draw(hint, 160, 30, Theme.Dark);
            }
        }
    }

    [Fact]
    public void Key_hint_is_not_hit_testable()
    {
        var hint = new KeyHint("Drag", "Pan", KeyHintKind.Mouse);
        hint.Measure(Size.Infinite);
        hint.Arrange(new Rect(0, 0, hint.DesiredSize.Width, hint.DesiredSize.Height));

        Assert.Null(hint.HitTest(2, 2));
    }

    [Fact]
    public void Modifier_changes_are_reported_once_each_way()
    {
        var (host, platform) = TestPlatform.Show(new Row(new FixedBox(10, 10)));
        var seen = new List<KeyModifiers>();
        host.ModifiersChanged += seen.Add;

        platform.Listener.KeyDown(Key.ShiftLeft, KeyModifiers.Shift);
        platform.Listener.KeyDown(Key.ShiftLeft, KeyModifiers.Shift);
        platform.Listener.PointerMove(5, 5, KeyModifiers.Shift);
        platform.Listener.KeyUp(Key.ShiftLeft, KeyModifiers.None);

        Assert.Equal([KeyModifiers.Shift, KeyModifiers.None], seen);
        Assert.Equal(KeyModifiers.None, host.Modifiers);
    }

    [Fact]
    public void Modifier_change_is_reported_while_a_text_field_has_focus()
    {
        var input = new TextInput();
        var (host, platform) = TestPlatform.Show(new Row(input));
        platform.Render().Dispose();
        input.Focus();
        KeyModifiers? seen = null;
        host.ModifiersChanged += m => seen = m;

        platform.Listener.KeyDown(Key.AltLeft, KeyModifiers.Alt);

        Assert.Equal(KeyModifiers.Alt, seen);
        Assert.Equal(KeyModifiers.Alt, host.Modifiers);
    }
}
