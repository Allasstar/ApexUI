namespace ApexUI.App.Examples;

public class ScaleExample : Widget
{
    /// Bind this to Application.UiScale from Program.cs.
    public Bindable<float> Scale { get; } = new(1f);

    /// Bind this to Application.Theme from Program.cs.
    public Bindable<bool> DarkMode { get; } = new(false);

    public ScaleExample()
    {
        var opacity = new Bindable<float>(0.75f);

        AddChild(new PaddingBox(
            new Column(

                new Label { Text = "UI Scale Demo", Bold = true, FontSize = 22f },

                // ── Scale control ─────────────────────────────────────────────
                MakeSliderRow("Scale  (0.5× – 2.0×)",
                    new Slider().WithMin(0.5f).WithMax(2f).WithStep(0.05f).Bind(Scale),
                    new TextInput { Width = 72f }
                        .AsFloat()
                        .WithValidation(s => string.IsNullOrEmpty(s) || InRange(s, 0.5f, 2f))
                        .BindFloat(Scale, "F2")),

                // ── Sample form ───────────────────────────────────────────────
                new Label { Text = "Sample form", Bold = true, FontSize = 16f },

                MakeFormRow("Username",
                    new TextInput { Width = 220f }.WithValue("JohnDoe")),
                MakeFormRow("Email",
                    new TextInput { Width = 220f }.WithValue("john@example.com")),
                MakeFormRow("Password",
                    new TextInput { Width = 220f }.AsPassword().WithValue("hunter2")),

                new Row(
                    new Toggle().WithLabel("Dark mode").Bind(DarkMode),
                    new Toggle(false, label: "Notifications")
                ).WithSpacing(24f),

                MakeSliderRow("Opacity  (0 – 1.0)",
                    new Slider().WithMin(0f).WithMax(1f).WithStep(0.05f).Bind(opacity),
                    new TextInput { Width = 72f }
                        .AsFloat()
                        .WithValidation(s => string.IsNullOrEmpty(s) || InRange(s, 0f, 1f))
                        .BindFloat(opacity, "F2")),

                new Row(
                    new Button("Save"),
                    new Button("Cancel").WithVariant(ButtonVariant.Secondary),
                    new Button("Reset").WithVariant(ButtonVariant.Ghost)
                ).WithSpacing(8f),

                // ── Sample card ───────────────────────────────────────────────
                MakeCard()

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    // TextInput first so Row measures its fixed Width before the slider takes the rest.
    private static Widget MakeSliderRow(string label, Slider slider, TextInput input)
        => new Column(
            new Label { Text = label },
            new Row(input, slider).WithSpacing(8f)
        ).WithSpacing(4f);

    private static Widget MakeFormRow(string label, Widget input)
        => new Row(
            new Label { Text = label, Width = 80f, VAlign = VAlign.Center },
            input
        ).WithSpacing(12f);

    private static Widget MakeCard()
        => new PaddingBox(
            new Column(
                new Label { Text = "About ApexUI", Bold = true },
                new Label { Text = "Every widget scales uniformly via a single canvas transform." },
                new Row(
                    new Button("Learn more"),
                    new Button("Dismiss").WithVariant(ButtonVariant.Ghost)
                ).WithSpacing(8f)
            ).WithSpacing(8f),
            new Thickness(16f)
        )
        { BackgroundSource = t => t.Surface, CornerRadius = 12f };

    // ── Validation ────────────────────────────────────────────────────────────

    private static bool InRange(string s, float min, float max)
        => float.TryParse(s, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out var v)
           && v >= min && v <= max;
}
