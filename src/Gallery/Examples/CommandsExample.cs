namespace ApexUI.Gallery.Examples;

/// CommandManager shortcuts with bound buttons, native file dialogs and the window title.
public class CommandsExample : Widget
{
    private readonly CommandManager _commands = new();

    public CommandsExample()
    {
        int count = 0;
        var counter = new Label { Text = "Count: 0", Role = TextRole.Heading };
        var log = new Label { Text = "Press a shortcut…", ColorSource = t => t.OnSurfaceMuted };

        void Set(int value)
        {
            count = value;
            counter.Text = $"Count: {count}";
        }

        var increment = _commands.Add("count.up", "Increment", () => Set(count + 1), "Mod+Up", "Equal");
        var decrement = _commands.Add("count.down", "Decrement", () => Set(count - 1), "Mod+Down", "Minus");
        var reset = _commands.Add("count.reset", "Reset", () => Set(0), "R");
        _commands.Executed += c => log.Text = $"Ran \"{c.Label}\"";

        var enabled = new Checkbox(true, "Commands enabled", on =>
        {
            increment.IsEnabled = on;
            decrement.IsEnabled = on;
            reset.IsEnabled = on;
        });

        var path = new Label { Text = "No file chosen", WordWrap = true, ColorSource = t => t.OnSurfaceMuted };
        var filters = new[] { new FileFilter("Text files", ["txt", "md"]), new FileFilter("All files", ["*"]) };

        async void Open()
        {
            string? chosen = await Host!.Platform.OpenFileAsync(new FileDialogOptions("Open a file", filters));
            path.Text = chosen ?? "Cancelled";
        }

        async void Save()
        {
            string? chosen = await Host!.Platform.SaveFileAsync(new FileDialogOptions("Save a file", filters, "notes.txt"));
            path.Text = chosen ?? "Cancelled";
        }

        async void PickFolder()
        {
            string? chosen = await Host!.Platform.PickFolderAsync("Choose a folder");
            path.Text = chosen ?? "Cancelled";
        }

        var title = new TextInput().WithValue("ApexUI Demo");
        title.Grow = 1f;

        AddChild(new PaddingBox(
            new Column(
                new Label { Text = "Commands", Bold = true, Role = TextRole.Heading },
                new Label
                {
                    Text = "Shortcuts run when no focused widget uses the key. Single-key shortcuts (R, =, -) wait while a text field has focus; all of them wait while a dialog is open.",
                    WordWrap = true,
                },
                counter,
                new Row(
                    new Tooltip(new Button("−").WithCommand(decrement), decrement.Tooltip),
                    new Tooltip(new Button("+").WithCommand(increment), increment.Tooltip),
                    new Tooltip(new Button("Reset").WithVariant(ButtonVariant.Secondary).WithCommand(reset), reset.Tooltip)
                ).WithSpacing(8f),
                enabled,
                log,
                new TextInput("Type here: R stays a letter"),
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "File dialogs", Bold = true, Role = TextRole.Heading },
                new Row(
                    new Button("Open…", Open),
                    new Button("Save as…", Save).WithVariant(ButtonVariant.Secondary),
                    new Button("Folder…", PickFolder).WithVariant(ButtonVariant.Secondary)
                ).WithSpacing(8f),
                path,
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "Window title", Bold = true, Role = TextRole.Heading },
                new Row(title, new Button("Set", () => Host!.Platform.SetTitle(title.Value))).WithSpacing(8f)
            ).WithSpacing(12f),
            24f));
    }

    protected override void OnAttached(UiHost host) => _commands.Attach(host);

    protected override void OnDetached() => _commands.Detach();
}
