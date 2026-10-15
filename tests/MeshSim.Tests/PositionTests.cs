using MeshSim.Core;
using Xunit;

namespace MeshSim.Tests;

public class PositionTests
{
    [Fact]
    public void DistanceToCalculatesEuclideanDistanceCorrectly()
    {
        var p1 = new Position(0, 0);
        var p2 = new Position(3, 4);

        double distance = p1.DistanceTo(p2);

        Assert.Equal(5.0, distance, precision: 6);
    }

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0.0)]
    [InlineData(0.0, double.NegativeInfinity)]
    [InlineData(double.NegativeInfinity, 0.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    [InlineData(double.NaN, double.NaN)]
    public void ConstructorThrowsArgumentOutOfRangeExceptionWhenCoordinatesAreInvalid(double x, double y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Position(x, y));
    }

    [Fact]
    public void EqualityComparesByValue()
    {
        var p1 = new Position(10.5, 20.25);
        var p2 = new Position(10.5, 20.25);

        Assert.Equal(p1, p2);
        Assert.True(p1 == p2);
    }

    [Fact]
    public void ConstructorStoresCoordinates()
    {
        var position = new Position(-12.5, 7.75);

        Assert.Equal(-12.5, position.X);
        Assert.Equal(7.75, position.Y);
    }

    [Theory]
    [InlineData(0.0, 0.0, 6.0, 8.0, 10.0)]
    [InlineData(-3.0, -4.0, 0.0, 0.0, 5.0)]
    [InlineData(-1.0, -1.0, 2.0, 3.0, 5.0)]
    [InlineData(0.0, 0.0, 0.0, 7.0, 7.0)]
    [InlineData(0.0, 0.0, -9.0, 0.0, 9.0)]
    [InlineData(5.0, 5.0, 5.0, 5.0, 0.0)]
    public void DistanceToHandlesNegativeCoordinatesAxisAlignedAndZeroDistance(
        double x1, double y1, double x2, double y2, double expected)
    {
        var p1 = new Position(x1, y1);
        var p2 = new Position(x2, y2);

        Assert.Equal(expected, p1.DistanceTo(p2), precision: 6);
    }

    [Fact]
    public void DistanceToIsSymmetric()
    {
        var p1 = new Position(1.5, -2.5);
        var p2 = new Position(-7.25, 11.0);

        Assert.Equal(p1.DistanceTo(p2), p2.DistanceTo(p1), precision: 9);
    }

    [Fact]
    public void InequalityDetectedWhenAnyCoordinateDiffers()
    {
        var baseline = new Position(1, 2);

        Assert.NotEqual(baseline, new Position(1, 3));
        Assert.NotEqual(baseline, new Position(2, 2));
        Assert.True(baseline != new Position(1, 3));
    }

    [Fact]
    public void EqualPositionsShareTheSameHashCode()
    {
        var p1 = new Position(4.5, -8.0);
        var p2 = new Position(4.5, -8.0);

        Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
    }

    [Fact]
    public void DefaultPositionEqualsOrigin()
    {
        // default(Position) обходить конструктор, але його значення (0, 0) валідне.
        Position defaultPosition = default;

        Assert.Equal(new Position(0, 0), defaultPosition);
        Assert.Equal(0.0, defaultPosition.X);
        Assert.Equal(0.0, defaultPosition.Y);
    }

    [Fact]
    public void ConstructorAcceptsLargeFiniteCoordinates()
    {
        var exception = Record.Exception(() => new Position(double.MaxValue, double.MinValue));

        Assert.Null(exception);
    }
}