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
}