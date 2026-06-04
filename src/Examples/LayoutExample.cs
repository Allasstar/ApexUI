namespace ApexUI.App.Examples;

/// Demonstrates Spacer, Grid (inventory view), and Wrap layout widgets.
public class LayoutExample : Widget
{
    public LayoutExample()
    {
        AddChild(new PaddingBox(
            new Column(

                // ── Spacer ────────────────────────────────────────────────────
                new Label { Text = "Spacer", Bold = true, FontSize = 18f },

                MakeCard(new Column(
                    new Label { Text = "Pushes siblings to opposite edges:" },
                    new Row(
                        new Label { Text = "Left" },
                        new Spacer(),
                        new Button("Right").WithVariant(ButtonVariant.Secondary)
                    ) { Spacing = 8f },
                    new Label { Text = "Multiple spacers split remaining space equally:" },
                    new Row(
                        new Button("Start").WithVariant(ButtonVariant.Ghost),
                        new Spacer(),
                        new Button("Middle").WithVariant(ButtonVariant.Ghost),
                        new Spacer(),
                        new Button("End").WithVariant(ButtonVariant.Ghost)
                    ) { Spacing = 8f }
                ).WithSpacing(8f)),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── Grid — Inventory ──────────────────────────────────────────
                new Label { Text = "Grid — Inventory", Bold = true, FontSize = 18f },

                MakeCard(BuildInventoryGrid()),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── Wrap ──────────────────────────────────────────────────────
                new Label { Text = "Wrap", Bold = true, FontSize = 18f },

                MakeCard(new Column(
                    new Label { Text = "Children reflow to the next line when they overflow the container width:" },
                    new Wrap(
                        MakeTag("C#"), MakeTag(".NET 9"), MakeTag("SkiaSharp"),
                        MakeTag("Silk.NET"), MakeTag("OpenGL"), MakeTag("GPU Rendering"),
                        MakeTag("UI Framework"), MakeTag("Layout"), MakeTag("2D Graphics"),
                        MakeTag("Vector"), MakeTag("Cross-Platform"), MakeTag("Zero-GC")
                    ).WithSpacing(6f, 6f)
                ).WithSpacing(8f)),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── Grid — Square Tiles (1:1 aspect ratio) ───────────────────
                new Label { Text = "Grid — Aspect Ratio (1:1 Square Tiles)", Bold = true, FontSize = 18f },

                MakeCard(new Column(
                    new Label { Text = "WithColumns(4): four equal Star columns. AspectRatio=1 makes every cell square — height tracks width as the window scales." },
                    new Scroll(BuildSquareTileGrid()) { Height = 300f }
                ).WithSpacing(8f)),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── Grid — 16:9 Cards ─────────────────────────────────────────
                new Label { Text = "Grid — Aspect Ratio (16:9 Cards)", Bold = true, FontSize = 18f },

                MakeCard(new Column(
                    new Label { Text = "WithColumns(2): two equal Star columns. AspectRatio=16/9 keeps cards widescreen at any window width." },
                    new Scroll(BuildWidescreenCardGrid()) { Height = 400f }
                ).WithSpacing(8f))

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }

    // ── Inventory grid ────────────────────────────────────────────────────────

    private static Widget BuildInventoryGrid()
    {
        var items = new (SKColor color, string name, int qty)[]
        {
            (new SKColor(0x33, 0x6B, 0xE6), "Sword",   1),
            (new SKColor(0x60, 0x7D, 0x8B), "Shield",  1),
            (new SKColor(0x4C, 0xAF, 0x50), "Potion",  5),
            (new SKColor(0xFF, 0xB3, 0x00), "Gold",  120),
            (new SKColor(0x9C, 0x27, 0xB0), "Staff",   1),
            (new SKColor(0xF4, 0x43, 0x36), "Dagger",  2),
            (new SKColor(0x00, 0x96, 0x88), "Gem",     7),
            (new SKColor(0x79, 0x55, 0x48), "Arrow",  24),
            (new SKColor(0x45, 0x6A, 0xBC), "Ring",    1),
            (new SKColor(0x00, 0x7A, 0xC1), "Key",     3),
            (new SKColor(0x2E, 0x7D, 0x32), "Herb",    8),
            (new SKColor(0xC6, 0x28, 0x28), "Bomb",    3),
        };

        const int Cols = 4;
        var grid = new Grid()
            .DefineColumns(GridLength.Star(), GridLength.Star(), GridLength.Star(), GridLength.Star())
            .WithSpacing(8f, 8f);

        for (int i = 0; i < items.Length; i++)
        {
            var (color, name, qty) = items[i];
            grid.Add(MakeItem(color, name, qty), i % Cols, i / Cols);
        }

        return grid;
    }

    private static Widget MakeItem(SKColor iconColor, string name, int qty)
        => new PaddingBox(
            new Column(
                // Icon area: colored rectangle with qty badge in bottom-right corner
                new PaddingBox(
                    new Label
                    {
                        Text     = $"×{qty}",
                        FontSize = 11f,
                        HAlign   = HAlign.Right,
                        VAlign   = VAlign.Bottom,
                        Color    = new SKColor(255, 255, 255),
                    },
                    new Thickness(4f, 28f, 4f, 4f)
                ) { Background = iconColor, CornerRadius = 4f },
                // Item name
                new Label { Text = name, FontSize = 12f, HAlign = HAlign.Center }
            ).WithSpacing(4f),
            6f
        ) { BackgroundSource = t => t.SurfaceHover, CornerRadius = 6f };

    // ── Square tile grid (1:1 AR) ─────────────────────────────────────────────

    private static Widget BuildSquareTileGrid()
    {
        var palette = new SKColor[]
        {
            new(0x33, 0x6B, 0xE6), new(0x60, 0x7D, 0x8B), new(0x4C, 0xAF, 0x50),
            new(0xFF, 0xB3, 0x00), new(0x9C, 0x27, 0xB0), new(0xF4, 0x43, 0x36),
            new(0x00, 0x96, 0x88), new(0x79, 0x55, 0x48),
        };
        const int Cols = 4;
        var grid = new Grid().WithColumns(Cols).WithSpacing(6f, 6f);
        for (int i = 0; i < 20; i++)
        {
            grid.Add(new PaddingBox(
                new Label
                {
                    Text     = $"{i + 1}",
                    FontSize = 22f,
                    Bold     = true,
                    HAlign   = HAlign.Center,
                    VAlign   = VAlign.Center,
                    Color    = SKColors.White,
                },
                4f
            )
            {
                Background   = palette[i % palette.Length],
                CornerRadius = 6f,
                AspectRatio  = 1f,
            }, i % Cols, i / Cols);
        }
        return grid;
    }

    // ── 16:9 card grid ────────────────────────────────────────────────────────

    private static Widget BuildWidescreenCardGrid()
    {
        var palette = new SKColor[]
        {
            new(0x1A, 0x23, 0x7E), new(0x1B, 0x5E, 0x20), new(0xB7, 0x1C, 0x1C),
            new(0x33, 0x69, 0x1E), new(0x01, 0x57, 0x9B), new(0x4A, 0x14, 0x8C),
        };
        const int Cols = 2;
        var grid = new Grid().WithColumns(Cols).WithSpacing(8f, 8f);
        for (int i = 0; i < 10; i++)
        {
            grid.Add(new PaddingBox(
                new Column(
                    new Label { Text = $"Card {i + 1}", FontSize = 16f, Bold = true, Color = SKColors.White },
                    new Label { Text = "16 : 9 — width controls height", FontSize = 12f, Color = new SKColor(255, 255, 255, 160) }
                ).WithSpacing(4f),
                12f
            )
            {
                Background   = palette[i % palette.Length],
                CornerRadius = 8f,
                AspectRatio  = 16f / 9f,
            }, i % Cols, i / Cols);
        }
        return grid;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static Widget MakeCard(Widget content)
        => new PaddingBox(content, 16f)
        {
            BackgroundSource = t => t.Surface,
            CornerRadius = 8f,
        };

    private static Widget MakeTag(string text)
        => new PaddingBox(
            new Label { Text = text, FontSize = 13f },
            new Thickness(10f, 4f)
        )
        {
            BackgroundSource = t => t.SurfaceHover,
            CornerRadius = 12f,
        };
}
