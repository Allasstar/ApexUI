using System.Globalization;

namespace ApexUI.Gallery.Examples;

public class SettingsExample : Widget
{
    public Bindable<float>  Scale      { get; } = new(1f);
    public Bindable<bool>   DarkMode   { get; } = new(false);
    public Bindable<string> FontFamily { get; } = new("Segoe UI");
    public Bindable<string> Preset     { get; }

    private static readonly string[] Fonts =
    [
        "Segoe UI", "Arial", "Calibri", "Verdana", "Tahoma",
        "Georgia", "Times New Roman", "Consolas", "Courier New",
    ];

    public SettingsExample(ThemeRegistry themes)
    {
        Preset = new(themes.DefaultName);
        AddChild(new PaddingBox(
            new Column(
                new Label { Text = "Settings", Bold = true, Role = TextRole.Title },

                Section("Appearance",
                    Row("Theme",         BuildThemeDropdown(themes)),
                    Row("Color mode",    new Toggle().WithLabel("Dark mode").Bind(DarkMode)),
                    Row("Font family",   BuildFontDropdown()),
                    Row("UI Scale  (0.5× – 2.0×)",
                        new Row(
                            new TextInput { Width = 72f }
                                .AsFloat()
                                .WithValidation(s => string.IsNullOrEmpty(s) || InRange(s, 0.5f, 2f))
                                .BindFloat(Scale, "F2"),
                            new Slider().WithMin(0.5f).WithMax(2f).WithStep(0.05f).Bind(Scale)
                        ).WithSpacing(8f))
                )
            ).WithSpacing(20f),
            new Thickness(24f)
        ));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Widget BuildThemeDropdown(ThemeRegistry themes)
    {
        var dd = new Dropdown<string>();
        // Lists what is registered now, so custom themes must be registered before this runs.
        foreach (var name in themes.Names)
            dd.AddItem(name, name);
        dd.Bind(Preset);
        return dd;
    }

    private Widget BuildFontDropdown()
    {
        var dd = new Dropdown<string>().WithPlaceholder("Select font…");
        foreach (var f in Fonts) dd.AddItem(f, f);
        dd.Bind(FontFamily);
        return dd;
    }

    private static Widget Section(string title, params Widget[] rows)
        => new Column([new Label { Text = title, Bold = true, Role = TextRole.Subheading }, .. rows]).WithSpacing(12f);

    private static Widget Row(string label, Widget control)
        => new Column(new Label { Text = label }, control).WithSpacing(4f);

    private static bool InRange(string s, float min, float max)
        => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
           && v >= min && v <= max;
}
