namespace ApexUI.Tests;

public class KeyRepeatTests
{
    private static readonly TimeSpan Delay = KeyRepeat.Default.Delay;
    private static readonly TimeSpan Interval = KeyRepeat.Default.Interval;

    [Fact]
    public void A_held_key_repeats_after_the_delay_at_the_interval_until_released()
    {
        var box = new FixedBox(100, 100) { Focusable = true };
        var repeats = new List<bool>();
        box.KeyDown += e => repeats.Add(e.IsRepeat);
        var (_, platform) = TestPlatform.Show(box);
        box.Focus();

        platform.Listener.KeyDown(Key.Right, KeyModifiers.None);
        platform.Advance(Delay - TimeSpan.FromMilliseconds(1));
        Assert.Equal([false], repeats);

        platform.Advance(TimeSpan.FromMilliseconds(1) + 3 * Interval);
        Assert.Equal([false, true, true, true, true], repeats);

        platform.Listener.KeyUp(Key.Right, KeyModifiers.None);
        platform.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(5, repeats.Count);
        Assert.Equal(0, platform.ActiveTimers);
    }

    [Fact]
    public void Holding_Backspace_in_a_text_input_deletes_repeatedly()
    {
        var input = new TextInput();
        var (_, platform) = TestPlatform.Show(input);
        input.Focus();
        platform.Type("abcdefgh");

        platform.Listener.KeyDown(Key.Backspace, KeyModifiers.None);
        platform.Advance(Delay + 3 * Interval);
        platform.Listener.KeyUp(Key.Backspace, KeyModifiers.None);

        Assert.Equal("abc", input.Value);
    }

    [Fact]
    public void Repeats_carry_the_modifiers_held_at_that_moment_and_modifiers_never_repeat()
    {
        var box = new FixedBox(100, 100) { Focusable = true };
        var keys = new List<(Key, KeyModifiers, bool)>();
        box.KeyDown += e => keys.Add((e.Key, e.Modifiers, e.IsRepeat));
        var (_, platform) = TestPlatform.Show(box);
        box.Focus();

        platform.Listener.KeyDown(Key.ShiftLeft, KeyModifiers.Shift);
        platform.Advance(Delay + Interval);
        platform.Listener.KeyDown(Key.Up, KeyModifiers.Shift);
        platform.Advance(Delay);

        Assert.Equal([(Key.ShiftLeft, KeyModifiers.Shift, false), (Key.Up, KeyModifiers.Shift, false), (Key.Up, KeyModifiers.Shift, true)], keys);
    }

    [Fact]
    public void Another_key_takes_over_the_repeat()
    {
        var box = new FixedBox(100, 100) { Focusable = true };
        var keys = new List<Key>();
        box.KeyDown += e => { if (e.IsRepeat) keys.Add(e.Key); };
        var (_, platform) = TestPlatform.Show(box);
        box.Focus();

        platform.Listener.KeyDown(Key.Left, KeyModifiers.None);
        platform.Advance(Delay);
        platform.Listener.KeyDown(Key.Right, KeyModifiers.None);
        platform.Advance(Delay);
        platform.Listener.KeyUp(Key.Left, KeyModifiers.None);
        platform.Advance(Interval);

        Assert.Equal([Key.Left, Key.Right, Key.Right], keys);
    }

    [Fact]
    public void A_platform_that_repeats_keys_itself_gets_no_extra_repeats()
    {
        var box = new FixedBox(100, 100) { Focusable = true };
        int downs = 0;
        box.KeyDown += _ => downs++;
        var (_, platform) = TestPlatform.Show(box);
        platform.RepeatsKeys = true;
        box.Focus();

        platform.Listener.KeyDown(Key.Right, KeyModifiers.None);
        platform.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(1, downs);
    }

    [Fact]
    public void The_systems_delay_and_rate_are_used_when_the_platform_reports_them()
    {
        var custom = new KeyRepeat(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(100));
        var platform = new TestPlatform { SystemKeyRepeat = custom };
        var host = new UiHost(platform);
        Assert.Equal(custom, host.KeyRepeat);
        Assert.Equal(KeyRepeat.Default, new UiHost(new TestPlatform()).KeyRepeat);

        var box = new FixedBox(100, 100) { Focusable = true };
        int repeats = 0;
        box.KeyDown += e => { if (e.IsRepeat) repeats++; };
        host.Run(box);
        box.Focus();
        platform.Listener.KeyDown(Key.Down, KeyModifiers.None);
        platform.Advance(TimeSpan.FromMilliseconds(450));

        Assert.Equal(3, repeats);
    }

    [Fact]
    public void A_held_shortcut_runs_once_unless_its_command_repeats()
    {
        var commands = new CommandManager();
        int saves = 0, nudges = 0;
        commands.Add("save", "Save", () => saves++, "Ctrl+S");
        commands.Add("nudge", "Nudge", () => nudges++, "Left").Repeats = true;
        var (host, platform) = TestPlatform.Show(new FixedBox(50, 50));
        commands.Attach(host);

        platform.Listener.KeyDown(Key.S, KeyModifiers.Control);
        platform.Advance(Delay + 2 * Interval);
        platform.Listener.KeyUp(Key.S, KeyModifiers.Control);
        platform.Listener.KeyDown(Key.Left, KeyModifiers.None);
        platform.Advance(Delay + 2 * Interval);
        platform.Listener.KeyUp(Key.Left, KeyModifiers.None);

        Assert.Equal(1, saves);
        Assert.Equal(4, nudges);
    }

    [Fact]
    public void A_held_arrow_steps_a_number_input()
    {
        var number = new NumberInput(0f);
        var (host, platform) = TestPlatform.Show(number);
        host.MoveFocus(forward: true);

        platform.Listener.KeyDown(Key.Up, KeyModifiers.None);
        platform.Advance(Delay + Interval);
        platform.Listener.KeyUp(Key.Up, KeyModifiers.None);

        Assert.Equal(3f, number.Value);
    }
}
