namespace ApexUI.App.Examples;

/// Demonstrates ProgressBar, Separator, and Overlay.
public class PrimitivesExample : Widget
{
    public PrimitivesExample()
    {
        var fill = new Bindable<float>(0.6f);

        // ── Modal dialog overlay ───────────────────────────────────────────────
        var dialog = new Overlay
        {
            IsModal               = true,
            DismissOnClickOutside = false,
            ContentHAlign         = HAlign.Center,
            ContentVAlign         = VAlign.Center,
        };

        var dialogContent = new PaddingBox(
            new Column(
                new Label { Text = "Overlay Dialog", Bold = true, FontSize = 18f },
                new Separator(),
                new Label { Text = "A modal Overlay renders above the entire widget tree,\nregardless of how deeply nested the caller is." },
                new Row(
                    new Button("Cancel").WithVariant(ButtonVariant.Ghost).OnPress(() => dialog.Close()),
                    new Button("OK").OnPress(() => dialog.Close())
                ) { Spacing = 8f, HAlign = HAlign.Right }
            ).WithSpacing(12f),
            20f
        ) { BackgroundSource = t => t.Surface, CornerRadius = 12f, Width = 380f };

        dialog.Content = dialogContent;

        // ── Anchored (non-modal) popup overlay ────────────────────────────────
        var popup = new Overlay { DismissOnClickOutside = true };

        var popupContent = new PaddingBox(
            new Column(
                new Label { Text = "Anchored Overlay", Bold = true },
                new Label { Text = "Positioned below the anchor button.\nClick anywhere outside to dismiss." }
            ).WithSpacing(4f),
            new Thickness(12f, 8f)
        ) { BackgroundSource = t => t.Surface, CornerRadius = 8f };

        popup.Content = popupContent;

        Button? popupAnchor = null;
        popupAnchor = new Button("Anchored Popup")
            .WithVariant(ButtonVariant.Secondary)
            .OnPress(() => popup.Open(popupAnchor!, OverlayAnchor.BelowAnchor));

        // ── Main layout ───────────────────────────────────────────────────────
        AddChild(new PaddingBox(
            new Column(

                // Progress bars
                new Label { Text = "ProgressBar", Bold = true, FontSize = 18f },

                MakeBarRow("Primary",  new ProgressBar().Bind(fill)),
                MakeBarRow("Success",  new ProgressBar { Variant = ProgressBarVariant.Success }.Bind(fill)),
                MakeBarRow("Warning",  new ProgressBar { Variant = ProgressBarVariant.Warning }.Bind(fill)),
                MakeBarRow("Danger",   new ProgressBar { Variant = ProgressBarVariant.Danger  }.Bind(fill)),

                new Slider().WithMin(0f).WithMax(1f).Bind(fill),

                new Separator { Margin = new Thickness(0, 4f) },

                // Separators
                new Label { Text = "Separator", Bold = true, FontSize = 18f },

                new Column(
                    new Label { Text = "Horizontal (default)" },
                    new Separator(),
                    new Label { Text = "Colored (Primary accent)" },
                    new Separator { Color = new SKColor(0x37, 0x8A, 0xDD) },
                    new Label { Text = "Vertical separators in a Row" },
                    new Row(
                        new Label { Text = "Left"   },
                        new Separator { Height = 22f }.AsVertical(),
                        new Label { Text = "Middle" },
                        new Separator { Height = 22f }.AsVertical(),
                        new Label { Text = "Right"  }
                    ).WithSpacing(12f)
                ).WithSpacing(8f),

                new Separator { Margin = new Thickness(0, 4f) },

                // Overlay
                new Label { Text = "Overlay", Bold = true, FontSize = 18f },

                new Row(
                    new Button("Open Modal Dialog").OnPress(() => dialog.Open()),
                    popupAnchor
                ).WithSpacing(12f)

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }

    private static Row MakeBarRow(string label, ProgressBar bar)
        => new Row(
            new Label { Text = label, Width = 60f },
            bar
        ).WithSpacing(12f);
}
