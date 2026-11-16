namespace ApexUI.Tests;

public class InputRoutingTests
{
    [Fact]
    public void Click_bubbles_until_a_widget_handles_it()
    {
        var box    = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var button = new Button("OK") { HAlign = HAlign.Left, VAlign = VAlign.Top, Margin = new Thickness(0, 100, 0, 0) };
        var root   = new TestPanel(box, button);
        var sources = new List<Widget>();
        root.Click += e => sources.Add(e.Source);
        var (_, platform) = TestPlatform.Show(root);

        platform.Click(10, 10);
        platform.Click(10, 110);

        Assert.Equal([box], sources);
    }

    [Fact]
    public void Subscribing_to_a_widget_adds_to_its_behavior_instead_of_replacing_it()
    {
        var toggle = new Toggle { HAlign = HAlign.Left, VAlign = VAlign.Top };
        int clicks = 0;
        toggle.Click += _ => clicks++;
        var (_, platform) = TestPlatform.Show(toggle);

        platform.Click(5, 5);

        Assert.True(toggle.IsChecked);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Pressed_widget_keeps_the_pointer_outside_its_bounds_until_release()
    {
        var box = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var moves = new List<float>();
        int captureLost = 0;
        box.PointerMoved       += e => moves.Add(e.X);
        box.PointerCaptureLost += () => captureLost++;
        var (host, platform) = TestPlatform.Show(new TestPanel(box));

        platform.Move(10, 10);
        platform.Press(10, 10);
        platform.Move(300, 300);
        Assert.Same(box, host.PointerCapture);

        platform.Release(300, 300);
        platform.Move(310, 310);

        Assert.Equal([10f, 300f], moves);
        Assert.Equal(1, captureLost);
        Assert.Null(host.PointerCapture);
    }

    [Fact]
    public void An_ancestor_can_take_capture_from_the_pressed_child()
    {
        var child = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var panel = new TestPanel(child);
        Widget? moveSource = null;
        int childLost = 0;
        panel.PointerPressed     += _ => panel.CapturePointer();
        panel.PointerMoved       += e => moveSource = e.Source;
        child.PointerCaptureLost += () => childLost++;
        var (_, platform) = TestPlatform.Show(panel);

        platform.Press(10, 10);
        platform.Move(20, 20);

        Assert.Same(panel, moveSource);
        Assert.Equal(1, childLost);
    }

    [Fact]
    public void Hover_covers_the_whole_chain_with_enter_and_exit_per_widget()
    {
        var child = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var panel = new TestPanel(child) { Width = 200, Height = 200, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var log = new List<string>();
        panel.PointerEntered += _ => log.Add("panel+");
        panel.PointerExited  += _ => log.Add("panel-");
        child.PointerEntered += _ => log.Add("child+");
        child.PointerExited  += _ => log.Add("child-");
        var (_, platform) = TestPlatform.Show(new TestPanel(panel));

        platform.Move(10, 10);
        Assert.True(panel.IsHovered && child.IsHovered);
        platform.Move(100, 100);
        Assert.True(panel.IsHovered && !child.IsHovered);
        platform.Move(500, 500);

        Assert.Equal(["panel+", "child+", "child-", "panel-"], log);
    }

    [Fact]
    public void Presses_close_in_time_and_place_count_as_double_clicks()
    {
        var box = new FixedBox(100, 100);
        var counts = new List<int>();
        box.PointerPressed += e => counts.Add(e.ClickCount);
        var (_, platform) = TestPlatform.Show(box);

        platform.Click(10, 10);
        platform.Advance(TimeSpan.FromMilliseconds(200));
        platform.Click(11, 11);
        platform.Advance(TimeSpan.FromSeconds(1));
        platform.Click(11, 11);
        platform.Click(60, 60);

        Assert.Equal([1, 2, 1, 1], counts);
    }

    [Fact]
    public void Pointer_and_wheel_events_carry_modifiers()
    {
        var box = new FixedBox(100, 100);
        KeyModifiers pressed = KeyModifiers.None, wheel = KeyModifiers.None;
        box.PointerPressed += e => pressed = e.Modifiers;
        box.Wheel          += e => wheel = e.Modifiers;
        var (_, platform) = TestPlatform.Show(box);

        platform.Click(10, 10, modifiers: KeyModifiers.Shift);
        platform.Listener.Wheel(0, 1, KeyModifiers.Control);

        Assert.Equal(KeyModifiers.Shift, pressed);
        Assert.Equal(KeyModifiers.Control, wheel);
    }

    [Fact]
    public void Wheel_bubbles_from_the_widget_under_the_pointer_to_the_scroll()
    {
        var scroll = new Scroll(new FixedBox(100, 1000));
        var (_, platform) = TestPlatform.Show(scroll, 200, 200);
        platform.Render().Dispose();
        var row = scroll.Children[0];
        float before = row.WindowBounds.Y;

        platform.Move(50, 50);
        platform.Listener.Wheel(0, -1, KeyModifiers.None);

        Assert.True(row.WindowBounds.Y < before);
    }

    [Fact]
    public void Pointer_moves_while_dragging_a_slider_allocate_nothing()
    {
        var slider = new Slider { Width = 400, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (_, platform) = TestPlatform.Show(new Column(new Label { Text = "x" }, slider));
        platform.Render().Dispose();
        platform.Move(50, 40);
        platform.Press(50, 40);
        platform.Move(60, 40);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            platform.Move(60 + i % 300, 40 + i % 7);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void Cursor_follows_the_widget_under_the_pointer()
    {
        var input = new TextInput { Width = 200, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (_, platform) = TestPlatform.Show(new TestPanel(input));

        platform.Move(10, 10);
        Assert.Equal(CursorShape.Text, platform.Cursor);

        platform.Move(500, 500);
        Assert.Equal(CursorShape.Arrow, platform.Cursor);
    }
}

public class OverlayInputTests
{
    [Fact]
    public void Clicking_inside_content_keeps_the_overlay_open_and_pressing_the_backdrop_closes_it()
    {
        var content = new FixedBox(100, 100);
        var overlay = new Overlay { Content = content, ContentHAlign = HAlign.Center, ContentVAlign = VAlign.Center };
        var (host, platform) = TestPlatform.Show(new FixedBox(10, 10));
        overlay.Open(host);
        platform.Render().Dispose();

        platform.Click(400, 300);
        Assert.Equal(1, host.OpenOverlayCount);

        platform.Press(10, 10);
        Assert.Equal(0, host.OpenOverlayCount);
    }

    [Fact]
    public void Context_menu_opens_for_children_added_after_attaching()
    {
        var panel = new TestPanel();
        ContextMenu.Attach(panel, new ContextMenu().AddItem("Copy", () => { }));
        var (host, platform) = TestPlatform.Show(panel);
        panel.Add(new FixedBox(100, 100) { HAlign = HAlign.Left, VAlign = VAlign.Top });
        platform.Render().Dispose();

        platform.Click(10, 10, PointerButton.Right);

        Assert.Equal(1, host.OpenOverlayCount);
    }
}

public class FocusTests
{
    [Fact]
    public void Click_focuses_the_nearest_focusable_widget_and_clicking_elsewhere_clears_it()
    {
        var input = new TextInput { Width = 200, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (host, platform) = TestPlatform.Show(new TestPanel(input));

        platform.Click(10, 10);
        Assert.True(input.IsFocused);
        Assert.Same(input, host.FocusedWidget);
        Assert.False(host.IsFocusVisible);

        platform.Click(500, 500);
        Assert.False(input.IsFocused);
        Assert.Null(host.FocusedWidget);
    }

    [Fact]
    public void Tab_moves_focus_in_tree_order_and_wraps()
    {
        var a = new Button("A");
        var b = new Checkbox(label: "B");
        var c = new TextInput();
        var (host, platform) = TestPlatform.Show(new Column(a, new Label { Text = "not focusable" }, b, c));

        platform.Key(Key.Tab);
        Assert.Same(a, host.FocusedWidget);
        Assert.True(host.IsFocusVisible);
        platform.Key(Key.Tab);
        Assert.Same(b, host.FocusedWidget);
        platform.Key(Key.Tab);
        Assert.Same(c, host.FocusedWidget);
        platform.Key(Key.Tab);
        Assert.Same(a, host.FocusedWidget);
        platform.Key(Key.Tab, KeyModifiers.Shift);
        Assert.Same(c, host.FocusedWidget);
    }

    [Fact]
    public void Tab_stays_inside_an_open_modal_overlay()
    {
        var outside = new Button("Outside");
        var inside  = new Button("Inside");
        var overlay = new Overlay { Content = new Column(inside), IsModal = true };
        var (host, platform) = TestPlatform.Show(new Column(outside));
        overlay.Open(host);

        platform.Key(Key.Tab);
        platform.Key(Key.Tab);

        Assert.Same(inside, host.FocusedWidget);
    }

    [Fact]
    public void Keys_bubble_from_the_focused_widget_and_unhandled_ones_reach_the_host()
    {
        var input = new TextInput { Width = 200, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var panel = new TestPanel(input);
        var bubbled   = new List<Key>();
        var unhandled = new List<Key>();
        panel.KeyDown += e => bubbled.Add(e.Key);
        var (host, platform) = TestPlatform.Show(panel);
        host.UnhandledKeyDown += e => unhandled.Add(e.Key);
        platform.Click(10, 10);

        platform.Key(Key.A, KeyModifiers.Control);
        platform.Key(Key.S, KeyModifiers.Control);

        Assert.Equal([Key.S], bubbled);
        Assert.Equal([Key.S], unhandled);
    }

    [Fact]
    public void Held_keys_are_marked_as_repeats()
    {
        var box = new FixedBox(100, 100) { Focusable = true };
        var repeats = new List<bool>();
        box.KeyDown += e => repeats.Add(e.IsRepeat);
        var (_, platform) = TestPlatform.Show(box);
        platform.Click(10, 10);

        platform.Listener.KeyDown(Key.Right, KeyModifiers.None);
        platform.Listener.KeyDown(Key.Right, KeyModifiers.None);
        platform.Listener.KeyUp(Key.Right, KeyModifiers.None);
        platform.Listener.KeyDown(Key.Right, KeyModifiers.None);

        Assert.Equal([false, true, false], repeats);
    }

    [Fact]
    public void Detaching_the_focused_widget_clears_focus()
    {
        var input = new TextInput { Width = 200, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var root  = new TestPanel(input);
        var (host, platform) = TestPlatform.Show(root);
        platform.Click(10, 10);

        root.Remove(input);

        Assert.Null(host.FocusedWidget);
        Assert.False(input.IsFocused);
        Assert.Equal(0, platform.ActiveTimers);
    }

    [Fact]
    public void Space_presses_a_focused_button_and_arrows_move_a_focused_slider()
    {
        int pressed = 0;
        var button = new Button("Go", () => pressed++);
        var slider = new Slider().WithStep(0.1f);
        var (_, platform) = TestPlatform.Show(new Column(button, slider));

        platform.Key(Key.Tab);
        platform.Key(Key.Space);
        platform.Key(Key.Tab);
        platform.Key(Key.Right);
        platform.Key(Key.Right);

        Assert.Equal(1, pressed);
        Assert.Equal(0.2f, slider.Value, 4);
    }
}

public class TextInputEditingTests
{
    [Fact]
    public void Copy_and_paste_go_through_the_platform_clipboard()
    {
        var (input, platform) = FocusedInput();
        platform.Type("hello");
        platform.Key(Key.A, KeyModifiers.Control);
        platform.Key(Key.C, KeyModifiers.Control);
        platform.Key(Key.End);

        platform.Key(Key.V, KeyModifiers.Control);

        Assert.Equal("hellohello", input.Value);
        Assert.Equal("hello", platform.GetClipboardText());
    }

    [Fact]
    public void Cut_removes_the_selection_and_paste_turns_line_breaks_into_spaces()
    {
        var (input, platform) = FocusedInput();
        platform.SetClipboardText("a\r\nb");
        platform.Type("xyz");
        platform.Key(Key.Left, KeyModifiers.Shift);

        platform.Key(Key.X, KeyModifiers.Control);
        Assert.Equal("xy", input.Value);
        Assert.Equal("z", platform.GetClipboardText());

        platform.SetClipboardText("a\r\nb");
        platform.Key(Key.V, KeyModifiers.Control);
        Assert.Equal("xya b", input.Value);
    }

    [Fact]
    public void Password_fields_do_not_copy()
    {
        var (input, platform) = FocusedInput();
        input.IsPassword = true;
        platform.Type("secret");
        platform.Key(Key.A, KeyModifiers.Control);

        platform.Key(Key.C, KeyModifiers.Control);

        Assert.Null(platform.GetClipboardText());
    }

    [Fact]
    public void Ctrl_arrows_and_ctrl_backspace_work_by_word()
    {
        var (input, platform) = FocusedInput();
        platform.Type("one two three");

        platform.Key(Key.Backspace, KeyModifiers.Control);
        Assert.Equal("one two ", input.Value);

        platform.Key(Key.Left, KeyModifiers.Control);
        platform.Type("X");
        Assert.Equal("one Xtwo ", input.Value);

        platform.Key(Key.Right, KeyModifiers.Control);
        platform.Type("Y");
        Assert.Equal("one Xtwo Y", input.Value);
    }

    [Fact]
    public void Escape_restores_the_text_from_when_the_field_was_focused_and_leaves_it()
    {
        var (input, platform) = FocusedInput();
        input.Value = "keep";
        platform.Click(500, 500);
        platform.Click(10, 10);
        platform.Type(" changed");

        platform.Key(Key.Escape);

        Assert.Equal("keep", input.Value);
        Assert.False(input.IsFocused);
    }

    [Fact]
    public void Double_click_selects_a_word()
    {
        var (input, platform) = FocusedInput();
        input.Value = "alpha beta";
        platform.Advance(TimeSpan.FromSeconds(1));

        platform.Click(15, 10);
        platform.Click(15, 10);
        platform.Type("A");

        Assert.Equal("A beta", input.Value);
    }

    [Fact]
    public void Long_text_scrolls_so_the_caret_stays_visible()
    {
        var (input, platform) = FocusedInput();
        platform.Type(new string('m', 80));
        platform.Render().Dispose();
        platform.Advance(TimeSpan.FromSeconds(1));

        // The end of the text is now at the right edge, so a click there lands at the end.
        platform.Click(195, 10);
        platform.Type("!");

        Assert.EndsWith("m!", input.Value);
    }

    [Fact]
    public void Up_and_down_in_a_number_input_step_its_value()
    {
        var number = new NumberInput(5) { Width = 200, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (_, platform) = TestPlatform.Show(number);

        platform.Key(Key.Tab);
        platform.Key(Key.Up);
        platform.Key(Key.Up);
        platform.Key(Key.Down);

        Assert.Equal(6f, number.Value);
    }

    private static (TextInput, TestPlatform) FocusedInput()
    {
        var input = new TextInput { Width = 200, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (_, platform) = TestPlatform.Show(new TestPanel(input));
        platform.Click(10, 10);
        return (input, platform);
    }
}
