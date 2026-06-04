namespace ApexUI.App.Examples;

/// Demonstrates RadioGroup<T> (vertical + horizontal) and NumberInput (float + int, with binding).
public class InputsExample : Widget
{
    public InputsExample()
    {
        // ── Shared state ──────────────────────────────────────────────────────
        var theme     = new Bindable<string>("System");
        var direction = new Bindable<string>("North");
        var quantity  = new Bindable<int>(5);
        var opacity   = new Bindable<float>(1.0f);
        var low       = new Bindable<float>(0f);
        var high      = new Bindable<float>(100f);

        var themeStatus = new Label { Text = $"Selected: {theme.Value}" };
        theme.Changed += v => themeStatus.Text = $"Selected: {v}";

        var dirStatus = new Label { Text = $"Heading: {direction.Value}" };
        direction.Changed += v => dirStatus.Text = $"Heading: {v}";

        var qtyLabel = new Label { Text = $"= {quantity.Value}", VAlign = VAlign.Center };
        quantity.Changed += v => qtyLabel.Text = $"= {v}";

        // ── Layout ────────────────────────────────────────────────────────────
        AddChild(new PaddingBox(
            new Column(

                // ── RadioGroup — Vertical ──────────────────────────────────────
                new Label { Text = "RadioGroup — Vertical", Bold = true, FontSize = 18f },

                MakeCard(new Column(
                    new Label { Text = "Mutually-exclusive options sharing a Bindable<string>:" },
                    new RadioGroup<string>()
                        .AddOption("Light",  "Light")
                        .AddOption("Dark",   "Dark")
                        .AddOption("System", "System (default)")
                        .Bind(theme),
                    themeStatus
                ).WithSpacing(8f)),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── RadioGroup — Horizontal ────────────────────────────────────
                new Label { Text = "RadioGroup — Horizontal", Bold = true, FontSize = 18f },

                MakeCard(new Column(
                    new Label { Text = "Same widget, RadioOrientation.Horizontal:" },
                    new RadioGroup<string>(RadioOrientation.Horizontal, spacing: 20f)
                        .AddOption("North", "North")
                        .AddOption("East",  "East")
                        .AddOption("South", "South")
                        .AddOption("West",  "West")
                        .Bind(direction),
                    dirStatus
                ).WithSpacing(8f)),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── NumberInput ────────────────────────────────────────────────
                new Label { Text = "NumberInput", Bold = true, FontSize = 18f },

                MakeCard(new Column(

                    new Label { Text = "Integer — Step 1, range 0..20, bound to Bindable<int>:" },
                    new Row(
                        new NumberInput().WithMin(0).WithMax(20).BindInt(quantity),
                        qtyLabel
                    ).WithSpacing(12f),

                    new Label { Text = "Float — Step 0.05, range 0..1, F2 display, synced with Slider:" },
                    new Row(
                        new NumberInput(1.0f).WithMin(0f).WithMax(1f).WithStep(0.05f).WithFormat("F2").Bind(opacity),
                        new Slider().WithMin(0f).WithMax(1f).WithStep(0.05f).Bind(opacity)
                    ).WithSpacing(12f),

                    new Label { Text = "Unconstrained pair — step 5:" },
                    new Row(
                        new Label { Text = "Low:",  Width = 36f, VAlign = VAlign.Center },
                        new NumberInput(0f).WithStep(5f).Bind(low),
                        new Label { Text = "High:", Width = 40f, VAlign = VAlign.Center },
                        new NumberInput(100f).WithStep(5f).Bind(high)
                    ).WithSpacing(8f)

                ).WithSpacing(12f)),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── Combined form ──────────────────────────────────────────────
                new Label { Text = "Combined — RadioGroup + NumberInput", Bold = true, FontSize = 18f },

                MakeCard(BuildCombinedForm())

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }

    // ── Combined game-settings form ───────────────────────────────────────────

    private static Widget BuildCombinedForm()
    {
        var difficulty = new Bindable<string>("Normal");
        var lives      = new Bindable<int>(3);
        var timeLimit  = new Bindable<float>(60f);

        var summary = new Label { Text = Summary(difficulty.Value, lives.Value, timeLimit.Value) };
        void Refresh() => summary.Text = Summary(difficulty.Value, lives.Value, timeLimit.Value);

        difficulty.Changed += _ => Refresh();
        lives.Changed      += _ => Refresh();
        timeLimit.Changed  += _ => Refresh();

        return new Column(
            new Label { Text = "Game Settings", Bold = true },
            new Grid()
                .DefineColumns(GridLength.Auto, GridLength.Star())
                .WithSpacing(12f, 10f)
                .Add(new Label { Text = "Difficulty", VAlign = VAlign.Center }, 0, 0)
                .Add(new RadioGroup<string>(RadioOrientation.Horizontal, spacing: 16f)
                        .AddOption("Easy",   "Easy")
                        .AddOption("Normal", "Normal")
                        .AddOption("Hard",   "Hard")
                        .Bind(difficulty), 1, 0)
                .Add(new Label { Text = "Lives", VAlign = VAlign.Center }, 0, 1)
                .Add(new NumberInput().WithMin(1).WithMax(9).BindInt(lives), 1, 1)
                .Add(new Label { Text = "Time (s)", VAlign = VAlign.Center }, 0, 2)
                .Add(new NumberInput(60f).WithMin(10f).WithMax(300f).WithStep(10f).WithFormat("0").Bind(timeLimit), 1, 2),
            new Separator(),
            summary
        ).WithSpacing(10f);
    }

    private static string Summary(string diff, int lives, float time)
        => $"{diff} mode · {lives} lives · {time:0} s";

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Widget MakeCard(Widget content)
        => new PaddingBox(content, 16f)
        {
            BackgroundSource = t => t.Surface,
            CornerRadius = 8f,
        };
}
