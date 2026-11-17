namespace ApexUI.Tests;

public class DialogTests
{
    [Fact]
    public void Enter_runs_the_primary_button_and_Escape_the_cancel_button()
    {
        var (host, platform) = TestPlatform.Show(new FixedBox(50, 50));
        string result = "";
        var confirm = Dialog.Confirm("Delete", "Sure?", onConfirm: () => result += "ok", onCancel: () => result += "cancel");

        confirm.Show(host);
        platform.Key(Key.Enter);
        confirm.Show(host);
        platform.Key(Key.Escape);

        Assert.Equal("okcancel", result);
        Assert.False(confirm.IsOpen);
    }

    [Fact]
    public void Showing_focuses_the_primary_button()
    {
        var (host, _) = TestPlatform.Show(new FixedBox(50, 50));
        var alert = Dialog.Alert("Note", "Hello", btnText: "Got it");

        alert.Show(host);

        Assert.Equal("Got it", Assert.IsType<Button>(host.FocusedWidget).Text);
    }

    [Fact]
    public void Initial_focus_wins_and_Escape_without_a_cancel_button_closes()
    {
        var (host, platform) = TestPlatform.Show(new FixedBox(50, 50));
        var name = new TextInput();
        var dialog = new Dialog("Rename", name) { InitialFocus = name };
        int closed = 0;
        dialog.Closed += () => closed++;
        dialog.AddPrimaryButton("OK", dialog.Close);

        dialog.Show(host);
        bool fieldFocused = host.FocusedWidget == name;
        host.ClearFocus();
        platform.Key(Key.Escape);

        Assert.True(fieldFocused);
        Assert.Equal(1, closed);
        Assert.False(dialog.IsOpen);
    }

    [Fact]
    public void Closing_twice_raises_Closed_once()
    {
        var (host, _) = TestPlatform.Show(new FixedBox(50, 50));
        var alert = Dialog.Alert("Note", "Hello");
        int closed = 0;
        alert.Closed += () => closed++;

        alert.Show(host);
        alert.Close();
        alert.Close();

        Assert.Equal(1, closed);
    }
}
