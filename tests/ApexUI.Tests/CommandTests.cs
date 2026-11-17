namespace ApexUI.Tests;

public class CommandTests
{
    [Theory]
    [InlineData("Ctrl+Shift+Z", Key.Z, KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData("F", Key.F, KeyModifiers.None)]
    [InlineData("alt+f4", Key.F4, KeyModifiers.Alt)]
    [InlineData("Ctrl+1", Key.D1, KeyModifiers.Control)]
    [InlineData("Equal", Key.Equal, KeyModifiers.None)]
    [InlineData("Cmd+S", Key.S, KeyModifiers.Super)]
    public void Gestures_parse_modifiers_and_key(string text, Key key, KeyModifiers modifiers)
    {
        Assert.Equal(new KeyGesture(key, modifiers), KeyGesture.Parse(text));
    }

    [Fact]
    public void Mod_means_the_platform_primary_modifier()
    {
        Assert.Equal(KeyGesture.PrimaryModifier, KeyGesture.Parse("Mod+S").Modifiers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Hyper+A")]
    [InlineData("Ctrl+Banana")]
    public void Invalid_gestures_do_not_parse(string text)
    {
        Assert.False(KeyGesture.TryParse(text, out _));
    }

    [Fact]
    public void Gestures_print_readably()
    {
        Assert.Equal("Ctrl+Shift+Z", new KeyGesture(Key.Z, KeyModifiers.Control | KeyModifiers.Shift).ToString());
        Assert.Equal("=", new KeyGesture(Key.Equal).ToString());
    }

    [Fact]
    public void An_unhandled_key_runs_the_command_with_that_exact_gesture()
    {
        var commands = new CommandManager();
        int undo = 0, redo = 0;
        commands.Add("undo", "Undo", () => undo++, "Ctrl+Z");
        commands.Add("redo", "Redo", () => redo++, "Ctrl+Shift+Z", "Ctrl+Y");
        var (host, platform) = TestPlatform.Show(new FixedBox(50, 50));
        commands.Attach(host);

        platform.Key(Key.Z, KeyModifiers.Control);
        platform.Key(Key.Z, KeyModifiers.Control | KeyModifiers.Shift);
        platform.Key(Key.Y, KeyModifiers.Control);
        platform.Key(Key.Z);

        Assert.Equal(1, undo);
        Assert.Equal(2, redo);
    }

    [Fact]
    public void A_disabled_command_ignores_its_shortcut_and_Execute()
    {
        var commands = new CommandManager();
        int runs = 0;
        var command = commands.Add("save", "Save", () => runs++, "Ctrl+S");
        var (host, platform) = TestPlatform.Show(new FixedBox(50, 50));
        commands.Attach(host);

        command.IsEnabled = false;
        platform.Key(Key.S, KeyModifiers.Control);

        Assert.False(command.Execute());
        Assert.Equal(0, runs);
    }

    [Fact]
    public void A_key_a_focused_widget_handles_does_not_run_a_command()
    {
        var box = new FixedBox(50, 50) { Focusable = true };
        box.KeyDown += e => e.Handled = true;
        var commands = new CommandManager();
        int runs = 0;
        commands.Add("fit", "Fit", () => runs++, "F");
        var (host, platform) = TestPlatform.Show(box);
        commands.Attach(host);
        box.Focus();

        platform.Key(Key.F);

        Assert.Equal(0, runs);
    }

    [Fact]
    public void Shortcuts_wait_while_a_text_field_has_focus()
    {
        var field = new TextInput();
        var commands = new CommandManager();
        int runs = 0;
        commands.Add("paint", "Paint", () => runs++, "B");
        var (host, platform) = TestPlatform.Show(field);
        commands.Attach(host);
        field.Focus();

        platform.Key(Key.B);
        commands.SuspendWhileEditingText = false;
        platform.Key(Key.B);

        Assert.Equal(1, runs);
    }

    [Fact]
    public void Shortcuts_wait_while_a_modal_dialog_is_open()
    {
        var commands = new CommandManager();
        int runs = 0;
        commands.Add("undo", "Undo", () => runs++, "Ctrl+Z");
        var (host, platform) = TestPlatform.Show(new FixedBox(50, 50));
        commands.Attach(host);
        var dialog = Dialog.Alert("Note", "Hello");

        dialog.Show(host);
        platform.Key(Key.Z, KeyModifiers.Control);
        dialog.Close();
        platform.Key(Key.Z, KeyModifiers.Control);

        Assert.Equal(1, runs);
    }

    [Fact]
    public void Detach_stops_listening()
    {
        var commands = new CommandManager();
        int runs = 0;
        commands.Add("fit", "Fit", () => runs++, "F");
        var (host, platform) = TestPlatform.Show(new FixedBox(50, 50));
        commands.Attach(host);

        commands.Detach();
        platform.Key(Key.F);

        Assert.Equal(0, runs);
    }

    [Fact]
    public void Names_are_unique_and_Execute_by_name_raises_Executed()
    {
        var commands = new CommandManager();
        commands.Add("a", "A", () => { });
        Command? executed = null;
        commands.Executed += c => executed = c;

        Assert.Throws<ArgumentException>(() => commands.Add("a", "Again", () => { }));
        Assert.True(commands.Execute("a"));
        Assert.Same(commands["a"], executed);
    }

    [Fact]
    public void Tooltip_shows_the_first_shortcut()
    {
        var commands = new CommandManager();
        var command = commands.Add("redo", "Redo", () => { }, "Ctrl+Shift+Z", "Ctrl+Y");

        Assert.Equal("Redo (Ctrl+Shift+Z)", command.Tooltip);
    }

    [Fact]
    public void A_button_with_a_command_runs_it_and_follows_its_enabled_state()
    {
        var commands = new CommandManager();
        int runs = 0;
        var command = commands.Add("go", "Go", () => runs++);
        var button = new Button("Go").WithCommand(command);
        var icon = new IconButton("M0 0h24v24H0z").WithCommand(command);
        var (_, platform) = TestPlatform.Show(new TestPanel(button));

        platform.Render().Dispose();
        platform.Click(10, 10);
        command.IsEnabled = false;

        Assert.Equal(1, runs);
        Assert.False(button.IsEnabled);
        Assert.False(icon.IsEnabled);
    }
}
