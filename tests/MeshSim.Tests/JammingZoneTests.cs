using MeshSim.Core;
using MeshSim.Engine;
using Xunit;

namespace MeshSim.Tests;

public class JammingZoneTests
{
    [Fact]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var zone = new JammingZone("jam-1", new Position(10, 10), 50.0, 0.8);

        Assert.Equal("jam-1", zone.Id);
        Assert.Equal(new Position(10, 10), zone.Center);
        Assert.Equal(50.0, zone.RadiusMeters);
        Assert.Equal(0.8, zone.JammingIntensity);
        Assert.True(zone.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorThrowsArgumentExceptionWhenIdIsInvalid(string invalidId)
    {
        Assert.Throws<ArgumentException>(() =>
            new JammingZone(invalidId, new Position(0, 0), 50.0, 0.5));
    }

    [Theory]
    [InlineData(0.0, 0.5)]
    [InlineData(-10.0, 0.5)]
    [InlineData(double.NaN, 0.5)]
    [InlineData(double.PositiveInfinity, 0.5)]
    [InlineData(50.0, -0.1)]
    [InlineData(50.0, 1.5)]
    [InlineData(50.0, double.NaN)]
    public void ConstructorThrowsArgumentOutOfRangeExceptionWhenNumericParametersAreInvalid(double radius, double intensity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new JammingZone("jam-1", new Position(0, 0), radius, intensity));
    }

    [Fact]
    public void ContainsReturnsTrueWhenPositionIsInsideOrOnBoundary()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 50.0, 1.0);
        var posOnBoundary = new Position(30, 40);

        Assert.True(zone.Contains(posOnBoundary));
    }

    [Fact]
    public void ContainsReturnsFalseWhenPositionIsOutsideOrZoneIsInactive()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 50.0, 1.0);
        var posOutside = new Position(60, 0);

        Assert.False(zone.Contains(posOutside));

        zone.IsActive = false;
        Assert.False(zone.Contains(new Position(0, 0)));
    }

    [Fact]
    public void IntersectsLinkDetectsPassThroughAndEndpointsAndRespectsActiveState()
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0);

        // Відрізок проходить наскрізь через зону (обидва кінці поза зоною)
        Assert.True(zone.IntersectsLink(new Position(0, 0), new Position(100, 0)));

        // Одна з точок вже всередині зони
        Assert.True(zone.IntersectsLink(new Position(50, 0), new Position(100, 0)));

        // Обидві точки збігаються поза межами зони (lengthSquared == 0)
        Assert.False(zone.IntersectsLink(new Position(0, 50), new Position(0, 50)));

        // Відрізок проходить повз зону
        Assert.False(zone.IntersectsLink(new Position(0, 50), new Position(100, 50)));

        // Вимкнена зона не блокує лінк
        zone.IsActive = false;
        Assert.False(zone.IntersectsLink(new Position(0, 0), new Position(100, 0)));
    }

    [Fact]
    public void MoveToUpdatesCenterCoordinate()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 25.0, 0.5);

        zone.MoveTo(new Position(100, 200));

        Assert.Equal(new Position(100, 200), zone.Center);
    }
}