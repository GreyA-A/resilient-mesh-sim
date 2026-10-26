using MeshSim.Core;
using MeshSim.Desktop.Rendering;

namespace MeshSim.Tests;

public class ViewportTransformTests
{
    private static readonly WorldBounds Square = new(0.0, 0.0, 100.0, 100.0);

    [Fact]
    public void FitScalesByLimitingAxisAndCentersContent()
    {
        ViewportTransform transform = ViewportTransform.Fit(Square, 300.0, 200.0, 0.0);

        Assert.Equal(2.0, transform.Scale);
        Assert.Equal(new ScreenPoint(50.0, 200.0), transform.ToScreen(new Position(0.0, 0.0)));
        Assert.Equal(new ScreenPoint(250.0, 0.0), transform.ToScreen(new Position(100.0, 100.0)));
        Assert.Equal(300.0, transform.ViewportWidth);
        Assert.Equal(200.0, transform.ViewportHeight);
    }

    [Fact]
    public void FitAppliesPaddingOnEverySide()
    {
        ViewportTransform transform = ViewportTransform.Fit(Square, 300.0, 200.0, 20.0);

        Assert.Equal(1.6, transform.Scale, precision: 9);
        ScreenPoint center = transform.ToScreen(new Position(50.0, 50.0));
        Assert.Equal(150.0, center.X, precision: 9);
        Assert.Equal(100.0, center.Y, precision: 9);
    }

    [Fact]
    public void ToScreenFlipsVerticalAxis()
    {
        ViewportTransform transform = ViewportTransform.Fit(Square, 200.0, 200.0, 0.0);

        double lowerY = transform.ToScreen(new Position(0.0, 0.0)).Y;
        double upperY = transform.ToScreen(new Position(0.0, 10.0)).Y;

        Assert.True(upperY < lowerY);
    }

    [Fact]
    public void FitMapsDegenerateBoundsCentreToViewportCentre()
    {
        var point = new WorldBounds(10.0, 10.0, 10.0, 10.0);

        ViewportTransform transform = ViewportTransform.Fit(point, 100.0, 100.0, 0.0);

        Assert.Equal(100.0, transform.Scale);
        ScreenPoint centre = transform.ToScreen(new Position(10.0, 10.0));
        Assert.Equal(50.0, centre.X, precision: 9);
        Assert.Equal(50.0, centre.Y, precision: 9);
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(10.0, 10.0, 20.0)]
    public void FitKeepsPositiveScaleForTinyOrZeroViewport(double width, double height, double padding)
    {
        ViewportTransform transform = ViewportTransform.Fit(Square, width, height, padding);

        Assert.True(transform.Scale > 0.0);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(100.0, 100.0)]
    [InlineData(-37.5, 12.25)]
    [InlineData(55.5, -80.0)]
    public void ToWorldInvertsToScreen(double x, double y)
    {
        ViewportTransform transform = ViewportTransform.Fit(Square, 640.0, 480.0, 30.0);

        Position restored = transform.ToWorld(transform.ToScreen(new Position(x, y)));

        Assert.Equal(x, restored.X, precision: 9);
        Assert.Equal(y, restored.Y, precision: 9);
    }

    [Fact]
    public void ScaleLengthMultipliesByScale()
    {
        ViewportTransform transform = ViewportTransform.Fit(Square, 300.0, 200.0, 0.0);

        Assert.Equal(50.0, transform.ScaleLength(25.0));
    }

    [Theory]
    [InlineData(double.NaN, 100.0, 0.0)]
    [InlineData(100.0, double.NaN, 0.0)]
    [InlineData(-1.0, 100.0, 0.0)]
    [InlineData(100.0, -1.0, 0.0)]
    [InlineData(double.PositiveInfinity, 100.0, 0.0)]
    [InlineData(100.0, 100.0, -1.0)]
    [InlineData(100.0, 100.0, double.NaN)]
    public void FitThrowsForInvalidViewportOrPadding(double width, double height, double padding)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ViewportTransform.Fit(Square, width, height, padding));
    }

    [Fact]
    public void FitThrowsArgumentExceptionForNonFiniteBounds()
    {
        var bounds = new WorldBounds(double.NaN, 0.0, 1.0, 1.0);

        Assert.Throws<ArgumentException>(() => ViewportTransform.Fit(bounds, 100.0, 100.0, 0.0));
    }
}