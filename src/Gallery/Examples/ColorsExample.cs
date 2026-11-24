namespace ApexUI.Gallery.Examples;

/// ColorSwatch with its popup picker, an inline ColorPicker with swatch rows and the eyedropper, a GradientBar, and
/// Toast messages.
public class ColorsExample : Widget
{
    public ColorsExample()
    {
        var preview = new Stack { Width = 120f, Height = 60f, Background = SKColor.FromHex("#5B9CFF"), CornerRadius = 8f };
        var hex = new Label { Text = ColorPicker.ToHex(preview.Background) };

        var fill = new ColorSwatch(preview.Background)
            .OnChange(c =>
            {
                preview.Background = c;
                hex.Text = ColorPicker.ToHex(c);
                preview.Invalidate();
            });
        var opaque = new ColorSwatch(SKColor.FromHex("#FF6B5B")).WithAlpha(false);
        var readOnly = new ColorSwatch(SKColor.FromHex("#3DDC8480")).AsReadOnly();

        // Recent colors: every finished edit moves its color to the front, at most 8.
        var recent = new ObservableList<SKColor>();
        var palette = new ObservableList<SKColor>([
            SKColor.FromHex("#5B9CFF"), SKColor.FromHex("#FF6B5B"), SKColor.FromHex("#4CC38A"),
            SKColor.FromHex("#F5B83D"), SKColor.FromHex("#B07CFF"), SKColor.FromHex("#2EC4C980"),
        ]);
        var inline = new ColorPicker(SKColor.FromHex("#FFC14D"))
            .AddSwatches("Recent", recent)
            .AddSwatches("Palette", palette);
        var inlineHex = new Label { Text = ColorPicker.ToHex(inline.Color) };
        inline.ColorChanged += c => inlineHex.Text = ColorPicker.ToHex(c);
        inline.EditCompleted += c =>
        {
            recent.Remove(c);
            recent.Insert(0, c);
            if (recent.Count > 8) recent.RemoveAt(recent.Count - 1);
        };
        fill.Picker.AddSwatches("Palette", palette);

        var gradient = new GradientBar(new ColorStop(0f, SKColor.FromHex("#5B9CFF")), new ColorStop(1f, SKColor.FromHex("#FF6B5B00")));
        var stopPicker = new ColorPicker(gradient.SelectedStop.Color);
        var stopLabel = new Label();
        void ShowStop() => stopLabel.Text = $"Stop {gradient.SelectedIndex + 1} of {gradient.Stops.Count} at {gradient.SelectedStop.Offset:0.00}";
        gradient.OnSelectionChange(_ => { stopPicker.Color = gradient.SelectedStop.Color; ShowStop(); });
        gradient.OnChange(ShowStop);
        stopPicker.ColorChanged += c => gradient.SetStopColor(gradient.SelectedIndex, c);
        ShowStop();

        var toast = new Toast();
        var topToast = new Toast { Placement = ToastPlacement.Top, EdgeOffset = 8f };
        int count = 0;
        var showToast = new Button("Show at the bottom", () => toast.Show(Host!, $"Message {++count}"));
        var showTopToast = new Button("Show at the top", () => topToast.Show(Host!, $"Message {++count}"));

        AddChild(new PaddingBox(
            new Column(
                new Label { Text = "ColorSwatch", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Click a swatch to pick a color. Transparent colors show over a checkerboard.", WordWrap = true },
                new Row(
                    Captioned("Fill", fill),
                    Captioned("No alpha", opaque),
                    Captioned("Read only", readOnly),
                    preview,
                    hex
                ).WithSpacing(16f),
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "ColorPicker", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Click the eyedropper, then anywhere in the window; Escape cancels. Finished edits fill the Recent row.", WordWrap = true },
                new Row(inline, inlineHex).WithSpacing(16f),
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "GradientBar", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Click the strip to add a stop, drag a handle to move it, drag it off or press Delete to remove it (never below two). The picker edits the selected stop.", WordWrap = true },
                new Row(new Column(gradient, stopLabel, stopPicker).WithSpacing(8f)).WithSpacing(16f),
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "Toast", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Appears at the bottom or the top of the window and hides itself; a new message replaces the old. The top one sits 8 px below the edge.", WordWrap = true },
                new Row(showToast, showTopToast).WithSpacing(8f)
            ).WithSpacing(12f),
            new Thickness(24f)));

        static Widget Captioned(string caption, Widget swatch)
        {
            swatch.VAlign = VAlign.Center;
            return new Row(swatch, new Label { Text = caption, VAlign = VAlign.Center }).WithSpacing(8f);
        }
    }
}
