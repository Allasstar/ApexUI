namespace ApexUI.Tests;

public class FileDropTests
{
    [Fact]
    public void Dropped_files_reach_the_widget_under_the_drop_point_in_its_own_space()
    {
        var target = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top, Margin = new Thickness(100, 40, 0, 0) };
        var other  = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var root   = new TestPanel(other, target);
        var received = new List<string>();
        float x = -1, y = -1;
        target.FilesDropped += e =>
        {
            received.AddRange(e.Paths);
            (x, y) = (e.X, e.Y);
        };
        var (_, platform) = TestPlatform.Show(root);

        platform.Drop(110, 60, "a.png", "b.jpg");

        Assert.Equal(["a.png", "b.jpg"], received);
        Assert.Equal(110, x);
        Assert.Equal(60, y);
    }

    [Fact]
    public void A_drop_bubbles_until_a_widget_handles_it()
    {
        var inner = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var middle = new TestPanel(inner);
        var root = new TestPanel(middle);
        int middleDrops = 0, rootDrops = 0;
        middle.FilesDropped += e =>
        {
            middleDrops++;
            e.Handled = true;
        };
        root.FilesDropped += _ => rootDrops++;
        var (_, platform) = TestPlatform.Show(root);

        platform.Drop(10, 10, "a.png");

        Assert.Equal(1, middleDrops);
        Assert.Equal(0, rootDrops);
    }

    [Fact]
    public void A_drop_position_follows_the_display_scale_and_moves_the_hover()
    {
        var box = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top, Margin = new Thickness(100, 0, 0, 0) };
        float x = -1;
        box.FilesDropped += e => x = e.X;
        var (_, platform) = TestPlatform.Show(new TestPanel(box), dpiScale: 2f);

        platform.Drop(220, 20, "a.png");

        Assert.Equal(110, x);
        Assert.True(box.IsHovered);
    }

    [Fact]
    public void An_empty_drop_or_one_outside_every_widget_reaches_nobody()
    {
        var box = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        int drops = 0;
        box.FilesDropped += _ => drops++;
        var (_, platform) = TestPlatform.Show(box);

        platform.Drop(10, 10);
        platform.Drop(300, 300, "a.png");

        Assert.Equal(0, drops);
    }
}
