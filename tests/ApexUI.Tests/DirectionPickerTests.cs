namespace ApexUI.Tests;

public class DirectionPickerTests
{
    private static (DirectionPicker Picker, TestPlatform Platform) Show()
    {
        var picker = new DirectionPicker { Width = 100f };
        var (_, platform) = TestPlatform.Show(new Column(picker), 300, 300);
        platform.Render().Dispose();
        return (picker, platform);
    }

    [Fact]
    public void Measures_as_a_square_of_the_theme_size_or_its_width()
    {
        var picker = new DirectionPicker();
        picker.Measure(new Size(500f, 500f));
        Assert.Equal(new Size(Theme.Light.DirectionPickerSize, Theme.Light.DirectionPickerSize), picker.DesiredSize);

        var wide = new DirectionPicker { Width = 100f };
        wide.Measure(new Size(500f, 500f));
        Assert.Equal(new Size(100f, 100f), wide.DesiredSize);
    }

    [Fact]
    public void The_centre_points_at_the_viewer_and_the_rim_lies_flat()
    {
        var (picker, _) = Show();
        Rect b = picker.LayoutBounds;

        Assert.Equal(90f, picker.DirectionAt(b.CenterX, b.CenterY).Elevation);
        var (az, el) = picker.DirectionAt(b.Right, b.CenterY);
        Assert.Equal(0f, az, 2);
        Assert.Equal(0f, el);
        Assert.Equal(90f, picker.DirectionAt(b.CenterX, b.Bottom - 10f).Azimuth, 2);
        Assert.Equal(180f, picker.DirectionAt(b.X + 10f, b.CenterY).Azimuth, 2);
    }

    [Fact]
    public void Dragging_reports_every_change_and_commits_once_on_release()
    {
        var (picker, platform) = Show();
        var changes = new List<(float, float)>();
        var commits = new List<(float, float)>();
        picker.DirectionChanged += (az, el) => changes.Add((az, el));
        picker.EditCompleted += (az, el) => commits.Add((az, el));
        Rect b = picker.LayoutBounds;

        platform.Press(b.CenterX + 20f, b.CenterY);
        platform.Move(b.CenterX, b.CenterY + 20f);
        platform.Release(b.CenterX, b.CenterY + 20f);

        Assert.Equal(2, changes.Count);
        Assert.Single(commits);
        Assert.Equal(90f, picker.Azimuth, 2);
        Assert.True(picker.Elevation is > 40f and < 60f);
        Assert.Equal((picker.Azimuth, picker.Elevation), commits[0]);
    }

    [Fact]
    public void A_click_on_the_current_direction_commits_nothing()
    {
        var (picker, platform) = Show();
        Rect b = picker.LayoutBounds;
        var (az, el) = picker.DirectionAt(b.CenterX - 20f, b.CenterY - 20f);
        picker.SetDirection(az, el);
        int commits = 0;
        picker.EditCompleted += (_, _) => commits++;

        platform.Click(b.CenterX - 20f, b.CenterY - 20f);

        Assert.Equal(0, commits);
    }

    [Fact]
    public void Arrow_keys_step_azimuth_and_elevation_and_commit_each_step()
    {
        var (picker, platform) = Show();
        picker.SetDirection(0f, 88f);
        picker.Focus();
        int commits = 0;
        picker.EditCompleted += (_, _) => commits++;

        platform.Key(Key.Left);
        platform.Key(Key.Up);
        platform.Key(Key.Up);

        Assert.Equal(355f, picker.Azimuth);
        Assert.Equal(90f, picker.Elevation);
        Assert.Equal(2, commits);
    }

    [Fact]
    public void Setting_from_code_raises_nothing_and_keeps_values_in_range()
    {
        var picker = new DirectionPicker();
        int events = 0;
        picker.DirectionChanged += (_, _) => events++;

        picker.SetDirection(-90f, 120f);

        Assert.Equal(270f, picker.Azimuth);
        Assert.Equal(90f, picker.Elevation);
        Assert.Equal(0, events);
    }

    [Fact]
    public void Drawing_again_unchanged_allocates_nothing()
    {
        var picker = new DirectionPicker { Width = 80f };
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var ctx = new DrawContext(surface.Canvas, Theme.Light, 1f);
        picker.Measure(new Size(100f, 100f));
        picker.Arrange(new Rect(0f, 0f, 100f, 100f));

        Assert.Equal(0, TestCanvas.MinAllocatedBytes(() => picker.Draw(ctx)));
    }
}
