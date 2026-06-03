using System.Globalization;

namespace ApexUI.App.Examples;

public class SliderExample : Widget
{
    public SliderExample()
    {
        var volume = new Bindable<float>(0.5f);
        var age    = new Bindable<int>(25);
        var temp   = new Bindable<float>(20f);

        AddChild(new PaddingBox(
            new Column(

                // ── Slider binding ────────────────────────────────────────────
                new Label { Text = "Sliders — two-way binding", Bold = true, FontSize = 18f },

                MakeSliderRow(
                    "Volume  (0 – 1.0,  continuous)",
                    new Slider().WithMin(0f).WithMax(1f).Bind(volume),
                    new TextInput { Width = 72f }
                        .AsFloat()
                        .WithValidation(s => string.IsNullOrEmpty(s) || InRange(s, 0f, 1f))
                        .BindFloat(volume, "F2")),

                MakeSliderRow(
                    "Age  (0 – 120,  integer steps)",
                    new Slider().WithMin(0f).WithMax(120f).WithStep(1f).BindInt(age),
                    new TextInput { Width = 72f }
                        .AsInteger()
                        .WithValidation(s => string.IsNullOrEmpty(s) || InRange(s, 0, 120))
                        .BindInt(age)),

                MakeSliderRow(
                    "Temperature  (–20 – 50 °C,  step 0.5)",
                    new Slider().WithMin(-20f).WithMax(50f).WithStep(0.5f).Bind(temp),
                    new TextInput { Width = 72f }
                        .AsFloat()
                        .WithValidation(s => string.IsNullOrEmpty(s) || InRange(s, -20f, 50f))
                        .BindFloat(temp, "F1")),

                // ── Input modes & validation ──────────────────────────────────
                new Label { Text = "Input modes & validation", Bold = true, FontSize = 18f },

                MakeInputRow("Password",
                    new TextInput { Width = 260f }
                        .AsPassword()
                        .WithPlaceholder("Enter password")),

                MakeInputRow("Integer only  (negative allowed)",
                    new TextInput { Width = 260f }
                        .AsInteger()
                        .WithPlaceholder("-99 to 99")
                        .WithValidation(s => string.IsNullOrEmpty(s) || InRange(s, -99, 99))),

                MakeInputRow("Float only",
                    new TextInput { Width = 260f }
                        .AsFloat()
                        .WithPlaceholder("e.g. 3.14")
                        .WithValidation(s => string.IsNullOrEmpty(s) ||
                            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _))),

                MakeInputRow("Hex only  (0-9, a-f)  — max 8 chars",
                    new TextInput { Width = 260f }
                        .WithAllowedChars("0123456789abcdefABCDEF")
                        .WithValidation(s => s.Length <= 8)
                        .WithPlaceholder("e.g. 1A2B3C")),

                MakeInputRow("Email  (spaces blocked)",
                    new TextInput { Width = 260f }
                        .WithBlockedChars(" \t")
                        .WithValidation(s => string.IsNullOrEmpty(s) || s.Contains('@'))
                        .WithPlaceholder("user@example.com"))

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }

    // TextInput first in Row so it claims its fixed Width before the slider measures.
    private static Widget MakeSliderRow(string label, Slider slider, TextInput input)
        => new Column(
            new Label { Text = label },
            new Row(input, slider).WithSpacing(8f)
        ).WithSpacing(4f);

    private static Widget MakeInputRow(string label, TextInput input)
        => new Column(
            new Label { Text = label },
            input
        ).WithSpacing(4f);

    // ── Validation helpers ────────────────────────────────────────────────────

    private static bool InRange(string s, float min, float max)
        => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
           && v >= min && v <= max;

    private static bool InRange(string s, int min, int max)
        => int.TryParse(s, out var v) && v >= min && v <= max;
}
