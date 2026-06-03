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
                new Label { Text = "Slider Demo", Bold = true, FontSize = 24f },

                MakeSection("Volume  (0 – 1.0,  continuous)",
                    new Slider().WithMin(0f).WithMax(1f).Bind(volume),
                    new TextInput { Width = 72f }.BindFloat(volume, "F2")),

                MakeSection("Age  (0 – 120,  integer steps)",
                    new Slider().WithMin(0f).WithMax(120f).WithStep(1f).BindInt(age),
                    new TextInput { Width = 72f }.BindInt(age)),

                MakeSection("Temperature  (–20 – 50 °C,  step 0.5)",
                    new Slider().WithMin(-20f).WithMax(50f).WithStep(0.5f).Bind(temp),
                    new TextInput { Width = 72f }.BindFloat(temp, "F1"))
            ).WithSpacing(20f),
            new Thickness(24f)
        ));
    }

    // Label above + Row(input | slider) — TextInput is first so Row measures it
    // at its fixed Width before giving the slider the remaining space.
    private static Widget MakeSection(string label, Slider slider, TextInput input)
        => new Column(
            new Label { Text = label },
            new Row(input, slider).WithSpacing(8f)
        ).WithSpacing(6f);
}
