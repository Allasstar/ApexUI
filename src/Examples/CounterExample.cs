namespace ApexUI.App.Examples;

public class CounterExample : Widget
{
    public CounterExample()
    {
        int count = 0;

        var counterLabel = new Label()
            .WithText("Count: 0")
            .WithSize(32)
            .AsBold();

        var statusLabel = new Label()
            .WithText("Press a button")
            .WithColor(SKColor.FromHex("#888888"));

        var nameInput = new TextInput("Enter your name…");
        var greetLabel = new Label().WithText("");

        AddChild(new PaddingBox(
            new Column(
                counterLabel,
                statusLabel,
                new Row(
                    new Button("+  Increment").OnPress(() =>
                    {
                        count++;
                        counterLabel.WithText($"Count: {count}");
                        statusLabel.WithText("Incremented").WithColor(SKColor.FromHex("#1D9E75"));
                    }),
                    new Button("-  Decrement").WithVariant(ButtonVariant.Secondary).OnPress(() =>
                    {
                        count--;
                        counterLabel.WithText($"Count: {count}");
                        statusLabel.WithText("Decremented").WithColor(SKColor.FromHex("#E24B4A"));
                    }),
                    new Button("Reset").WithVariant(ButtonVariant.Ghost).OnPress(() =>
                    {
                        count = 0;
                        counterLabel.WithText("Count: 0");
                        statusLabel.WithText("Reset").WithColor(SKColor.FromHex("#888888"));
                    })
                ).WithSpacing(8),
                nameInput.OnChange(v => greetLabel.WithText(
                    string.IsNullOrEmpty(v) ? "" : $"Hello, {v}!")),
                greetLabel
            ).WithSpacing(16),
            all: 32
        ));
    }
}
