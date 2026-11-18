namespace ApexUI.Gallery.Examples;

/// ColorSwatch with its popup picker, an inline ColorPicker, and Toast messages.
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

        var inline = new ColorPicker(SKColor.FromHex("#FFC14D"));
        var inlineHex = new Label { Text = ColorPicker.ToHex(inline.Color) };
        inline.ColorChanged += c => inlineHex.Text = ColorPicker.ToHex(c);

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
                new Row(inline, inlineHex).WithSpacing(16f),
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
