namespace ApexUI.Tests;

public class InvalidationTests
{
    [Fact]
    public void Layout_change_marks_the_widget_and_its_ancestors_dirty_but_not_siblings()
    {
        var box   = new CountingBox(10, 10);
        var inner = new Column(box);
        var other = new Column(new FixedBox(10, 10));
        var root  = new Column(inner, other);
        var (_, platform) = TestPlatform.Show(root);
        platform.Render().Dispose();

        box.Resize(20, 20);

        Assert.True(box.IsLayoutDirty);
        Assert.True(inner.IsLayoutDirty);
        Assert.True(root.IsLayoutDirty);
        Assert.False(other.IsLayoutDirty);
    }

    [Fact]
    public void A_frame_clears_every_dirty_flag()
    {
        var box  = new CountingBox(10, 10);
        var root = new Column(new Column(box));
        var (_, platform) = TestPlatform.Show(root);
        platform.Render().Dispose();

        box.Resize(20, 20);
        platform.Render().Dispose();

        Assert.False(box.IsLayoutDirty);
        Assert.False(root.IsLayoutDirty);
        Assert.Equal(20, box.LayoutBounds.Height);
    }

    [Fact]
    public void Render_only_changes_request_a_frame_without_layout()
    {
        var box  = new CountingBox(10, 10);
        var root = new Column(box);
        var (_, platform) = TestPlatform.Show(root);
        platform.Render().Dispose();
        int frames = platform.FramesRequested, measures = box.Measures, draws = box.Draws;

        root.Background = SKColors.Red;
        Assert.Equal(frames + 1, platform.FramesRequested);
        Assert.False(root.IsLayoutDirty);
        platform.Render().Dispose();

        Assert.Equal(measures, box.Measures);
        Assert.Equal(draws + 1, box.Draws);
    }

    [Fact]
    public void Layout_changes_request_a_frame()
    {
        var box = new CountingBox(10, 10);
        var (_, platform) = TestPlatform.Show(new Column(box));
        platform.Render().Dispose();
        int frames = platform.FramesRequested;

        box.Resize(30, 30);

        Assert.Equal(frames + 1, platform.FramesRequested);
    }

    [Fact]
    public void Detached_widgets_record_changes_without_a_host()
    {
        var box    = new CountingBox(10, 10);
        var column = new Column(box);
        column.Measure(new Size(100, 100));
        column.Arrange(new Rect(0, 0, 100, 100));

        box.Resize(20, 20);
        box.Invalidate();

        Assert.True(column.IsLayoutDirty);
        column.Measure(new Size(100, 100));
        column.Arrange(new Rect(0, 0, 100, 100));
        Assert.Equal(20, box.LayoutBounds.Height);
    }

    [Fact]
    public void Showing_a_widget_lays_out_its_siblings_again_and_hiding_it_frees_the_space()
    {
        var toggled = new FixedBox(10, 40) { IsVisible = false };
        var below   = new FixedBox(10, 10);
        var (_, platform) = TestPlatform.Show(new Column(toggled, below));
        platform.Render().Dispose();
        Assert.Equal(0, below.LayoutBounds.Y);

        toggled.IsVisible = true;
        platform.Render().Dispose();
        Assert.Equal(40, below.LayoutBounds.Y);

        toggled.IsVisible = false;
        platform.Render().Dispose();
        Assert.Equal(0, below.LayoutBounds.Y);
    }
}
