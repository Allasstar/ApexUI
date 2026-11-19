namespace ApexUI.Tests;

public class TextAreaTests
{
    private static (TextArea Area, UiHost Host, TestPlatform Platform) Show(string value = "")
    {
        var area = new TextArea { HAlign = HAlign.Left, VAlign = VAlign.Top, Width = 300f }.WithValue(value);
        var other = new Button("Other") { HAlign = HAlign.Left, VAlign = VAlign.Bottom };
        var (host, platform) = TestPlatform.Show(new TestPanel(area, other));
        platform.Render().Dispose();
        area.Focus();
        return (area, host, platform);
    }

    [Fact]
    public void Enter_breaks_the_line_and_typing_goes_on_the_new_line()
    {
        var (area, _, platform) = Show("ab");

        platform.Key(Key.Enter);
        platform.Type("cd");

        Assert.Equal("ab\ncd", area.Value);
        Assert.Equal(5, area.CaretIndex);
    }

    [Fact]
    public void Up_and_down_keep_the_column_through_a_shorter_line()
    {
        var (area, _, platform) = Show("abcdef\nab\nabcdef");
        area.Select(5, 5);

        platform.Key(Key.Down);
        Assert.Equal(9, area.CaretIndex);
        platform.Key(Key.Down);
        Assert.Equal(15, area.CaretIndex);
        platform.Key(Key.Up);
        platform.Key(Key.Up);
        Assert.Equal(5, area.CaretIndex);
    }

    [Fact]
    public void Home_and_end_go_to_the_line_ends_and_ctrl_to_the_whole_text()
    {
        var (area, _, platform) = Show("one\ntwo\nthree");
        area.Select(5, 5);

        platform.Key(Key.Home);
        Assert.Equal(4, area.CaretIndex);
        platform.Key(Key.End);
        Assert.Equal(7, area.CaretIndex);
        platform.Key(Key.End, KeyModifiers.Control);
        Assert.Equal(13, area.CaretIndex);
        platform.Key(Key.Home, KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal((0, 13), (area.SelectionStart, area.SelectionEnd));
    }

    [Fact]
    public void Typing_over_a_selection_spanning_lines_replaces_it()
    {
        var (area, _, platform) = Show("one\ntwo\nthree");
        area.Select(2, 10);

        platform.Type("X");

        Assert.Equal("onXree", area.Value);
    }

    [Fact]
    public void Edit_completes_once_on_ctrl_enter_or_when_focus_leaves_and_only_after_a_change()
    {
        var (area, host, platform) = Show("Text");
        var completed = new List<string>();
        area.EditCompleted += completed.Add;

        host.ClearFocus();
        Assert.Empty(completed);

        area.Focus();
        platform.Type("!");
        platform.Key(Key.Enter, KeyModifiers.Control);
        host.ClearFocus();

        Assert.Equal(["Text!"], completed);
        Assert.Equal("Text!", area.Value);
    }

    [Fact]
    public void Escape_restores_the_text_it_had_when_focused_and_leaves()
    {
        var (area, host, platform) = Show("keep");
        int completed = 0;
        area.EditCompleted += _ => completed++;

        platform.Type(" this");
        platform.Key(Key.Enter);
        platform.Key(Key.Escape);

        Assert.Equal("keep", area.Value);
        Assert.Null(host.FocusedWidget);
        Assert.Equal(0, completed);
    }

    [Fact]
    public void The_field_grows_with_its_lines_between_min_and_max()
    {
        var (area, _, platform) = Show();
        area.WithLines(2, 4);
        platform.Render().Dispose();
        float two = area.LayoutBounds.Height;

        area.Value = "1\n2\n3";
        platform.Render().Dispose();
        float three = area.LayoutBounds.Height;
        area.Value = "1\n2\n3\n4\n5\n6";
        platform.Render().Dispose();
        float many = area.LayoutBounds.Height;

        Assert.True(three > two);
        Assert.Equal(two * 2 - area.Padding.Vertical, many, 0.01f);
    }

    [Fact]
    public void Paste_keeps_line_breaks_as_newlines_and_drops_other_control_characters()
    {
        var (area, host, platform) = Show();
        host.Platform.SetClipboardText("a\r\nb\rc\td\u0001");

        platform.Key(Key.V, KeyModifiers.Control);

        Assert.Equal("a\nb\nc d", area.Value);
    }

    [Fact]
    public void Clicking_on_the_second_line_puts_the_caret_there()
    {
        var (area, _, platform) = Show("one\ntwo");
        var inner = area.ContentBounds;
        float lineHeight = area.ContentBounds.Height / 3;

        platform.Click(inner.X + 1, inner.Y + lineHeight * 1.5f);

        Assert.Equal(4, area.CaretIndex);
    }
}
