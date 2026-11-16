namespace ApexUI.Tests;

public class DrawAllocationTests
{
    [Fact]
    public void Drawing_shapes_backgrounds_opacity_and_containers_allocates_nothing()
    {
        using var surface = SKSurface.Create(new SKImageInfo(300, 200));
        using var ctx     = new DrawContext(surface.Canvas, Theme.Light, 1f);
        var tree = new Column(
            new FixedBox(50, 20) { Background = SKColors.Red, CornerRadius = 4 },
            new FixedBox(50, 20) { Background = SKColors.Blue, Opacity = 0.5f },
            new ProgressBar { Value = 0.4f },
            new Separator(),
            new Slider { Value = 0.3f });
        tree.Measure(new Size(300, 200));
        tree.Arrange(new Rect(0, 0, 300, 200));

        long bytes = TestCanvas.MinAllocatedBytes(() =>
        {
            tree.Draw(ctx);
            ctx.StrokeRoundRect(new Rect(0, 0, 40, 40), 6, SKColors.Gray);
            ctx.DrawShadow(new Rect(0, 0, 40, 40), 6, 10, SKColors.Black);
            ctx.DrawChevron(20, 20, 4, pointUp: true, SKColors.Gray);
            ctx.DrawCheckmark(20, 20, 6, SKColors.Gray);
            ctx.FillTriangle(0, 0, 10, 0, 5, 8, SKColors.Gray);
        });

        Assert.Equal(0, bytes);
    }

    [Fact]
    public void Drawing_unchanged_labels_allocates_nothing()
    {
        using var surface = SKSurface.Create(new SKImageInfo(300, 200));
        using var ctx     = new DrawContext(surface.Canvas, Theme.Light, 1f);
        var tree = new Column(
            new Label { Text = "Hello world" },
            new Label { Text = "A longer sentence that wraps over several lines", WordWrap = true, Width = 120 });
        tree.Measure(new Size(300, 200));
        tree.Arrange(new Rect(0, 0, 300, 200));

        Assert.Equal(0, TestCanvas.MinAllocatedBytes(() => tree.Draw(ctx)));
    }

    [Fact]
    public void Walking_children_allocates_nothing()
    {
        var tree = new Column(new FixedBox(1, 1), new Row(new FixedBox(1, 1), new FixedBox(1, 1)));

        static int Count(Widget w)
        {
            int n = 1;
            foreach (var child in w.Children) n += Count(child);
            return n;
        }

        Assert.Equal(0, TestCanvas.MinAllocatedBytes(() => Count(tree)));
    }

    [Fact]
    public void Drawing_an_empty_text_input_allocates_nothing()
    {
        using var surface = SKSurface.Create(new SKImageInfo(300, 40));
        using var ctx     = new DrawContext(surface.Canvas, Theme.Light, 1f);
        var input = new TextInput();
        input.Measure(new Size(300, 40));
        input.Arrange(new Rect(0, 0, 300, 40));

        Assert.Equal(0, TestCanvas.MinAllocatedBytes(() => input.Draw(ctx)));
    }

    [Fact]
    public void Remeasuring_unchanged_labels_allocates_nothing()
    {
        var single  = new Label { Text = "Hello world" };
        var wrapped = new Label { Text = "A longer sentence that wraps over several lines", WordWrap = true };

        long bytes = TestCanvas.MinAllocatedBytes(() =>
        {
            single.Measure(new Size(500, 100));
            wrapped.Measure(new Size(80, 500));
        });

        Assert.Equal(0, bytes);
    }
}

public class TextCacheTests
{
    [Fact]
    public void Wrapped_label_height_follows_text_changes()
    {
        var label = new Label { Text = "one two three four five six seven eight", WordWrap = true };
        label.Measure(new Size(60, 1000));
        float manyLines = label.DesiredSize.Height;

        label.Text = "one";
        label.Measure(new Size(60, 1000));

        Assert.True(label.DesiredSize.Height < manyLines);
    }

    [Fact]
    public void Wrapped_label_height_follows_width_changes()
    {
        var label = new Label { Text = "one two three four five six seven eight", WordWrap = true };
        label.Measure(new Size(60, 1000));
        float narrow = label.DesiredSize.Height;

        label.Measure(new Size(1000, 1000));

        Assert.True(label.DesiredSize.Height < narrow);
    }

    [Fact]
    public void Label_width_follows_text_changes()
    {
        var label = new Label { Text = "short" };
        label.Measure(new Size(1000, 100));
        float shortWidth = label.DesiredSize.Width;

        label.Text = "a much longer piece of text";
        label.Measure(new Size(1000, 100));

        Assert.True(label.DesiredSize.Width > shortWidth);
    }

    [Fact]
    public void Clicking_places_the_caret_using_the_current_text()
    {
        var input = new TextInput();
        input.Measure(new Size(400, 40));
        input.Arrange(new Rect(0, 0, 400, 40));

        input.Value = "abc";
        Click(input, 399);
        Type(input, "d");
        Assert.Equal("abcd", input.Value);

        Click(input, 0);
        Type(input, "x");
        Assert.Equal("xabcd", input.Value);

        input.Value = "hello world";
        Click(input, 399);
        Type(input, "!");
        Assert.Equal("hello world!", input.Value);
    }

    [Fact]
    public void Password_input_caret_follows_masked_text()
    {
        var input = new TextInput().AsPassword();
        input.Measure(new Size(400, 40));
        input.Arrange(new Rect(0, 0, 400, 40));

        input.Value = "secret";
        Click(input, 399);
        Type(input, "!");

        Assert.Equal("secret!", input.Value);
    }

    private static void Click(TextInput input, float x)
    {
        input.OnPointerDown!(new PointerEvent(x, 20, PointerButton.Left, true));
        input.OnPointerUp!(new PointerEvent(x, 20, PointerButton.Left, false));
    }

    private static void Type(TextInput input, string text)
    {
        foreach (var c in text)
            input.OnTextInput!(new TextInputEvent(c.ToString()));
    }
}

public class FontCacheTests
{
    [Fact]
    public void Same_request_returns_the_same_font()
    {
        using var cache = new FontCache();

        var first  = cache.Get(FontCache.DefaultFamily, 14, bold: false);
        var second = cache.Get(FontCache.DefaultFamily, 14, bold: false);
        var bold   = cache.Get(FontCache.DefaultFamily, 14, bold: true);

        Assert.Same(first, second);
        Assert.NotSame(first, bold);
    }

    [Fact]
    public void Context_with_shared_cache_does_not_dispose_it()
    {
        using var surface = SKSurface.Create(new SKImageInfo(10, 10));
        using var cache   = new FontCache();
        var font = cache.Get(FontCache.DefaultFamily, 14);

        new DrawContext(surface.Canvas, Theme.Light, 1f, fonts: cache).Dispose();

        Assert.Same(font, cache.Get(FontCache.DefaultFamily, 14));
        Assert.True(font.MeasureText("abc") > 0);
    }
}
