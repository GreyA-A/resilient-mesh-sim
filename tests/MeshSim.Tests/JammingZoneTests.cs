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
    [InlineData(double.NegativeInfinity, 0.5)]
    [InlineData(50.0, -0.1)]
    [InlineData(50.0, 1.5)]
    [InlineData(50.0, double.NaN)]
    [InlineData(50.0, double.PositiveInfinity)]
    [InlineData(50.0, double.NegativeInfinity)]
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

    // ---------- Constructor ----------

    [Fact]
    public void ConstructorThrowsArgumentExceptionWhenIdIsNull()
    {
        Assert.Throws<ArgumentException>(() =>
            new JammingZone(null!, new Position(0, 0), 50.0, 0.5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void ConstructorAcceptsIntensityBoundaryValues(double intensity)
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 10.0, intensity);

        Assert.Equal(intensity, zone.JammingIntensity);
    }

    [Fact]
    public void ConstructorCanCreateInactiveZone()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 10.0, 0.5, isActive: false);

        Assert.False(zone.IsActive);
        Assert.False(zone.Contains(new Position(0, 0)));
    }

    // ---------- Contains ----------

    [Theory]
    [InlineData(0.0, 0.0)]    // центр
    [InlineData(10.0, 10.0)]  // всередині
    [InlineData(-30.0, -40.0)] // на межі в іншому квадранті
    public void ContainsReturnsTrueForInteriorCenterAndBoundaryPositions(double x, double y)
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 50.0, 1.0);

        Assert.True(zone.Contains(new Position(x, y)));
    }

    [Fact]
    public void ContainsReturnsFalseJustBeyondBoundary()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 50.0, 1.0);

        Assert.False(zone.Contains(new Position(50.001, 0)));
    }

    [Fact]
    public void ContainsFollowsZoneAfterMoveTo()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 10.0, 1.0);
        var target = new Position(100, 100);
        Assert.False(zone.Contains(target));

        zone.MoveTo(new Position(100, 100));

        Assert.True(zone.Contains(target));
        Assert.False(zone.Contains(new Position(0, 0)));
    }

    [Fact]
    public void ContainsReflectsReactivation()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 10.0, 1.0);
        var inside = new Position(1, 1);

        zone.IsActive = false;
        Assert.False(zone.Contains(inside));

        zone.IsActive = true;
        Assert.True(zone.Contains(inside));
    }

    // ---------- IntersectsLink ----------

    [Theory]
    [InlineData(0.0, 0.0, 20.0, 0.0)]    // відрізок закінчується до зони (clamp t > 1)
    [InlineData(100.0, 0.0, 80.0, 0.0)]  // те саме у зворотному напрямку
    [InlineData(80.0, 0.0, 100.0, 0.0)]  // відрізок починається після зони (clamp t < 0)
    public void IntersectsLinkReturnsFalseWhenInfiniteLineCrossesZoneButSegmentDoesNot(
        double x1, double y1, double x2, double y2)
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0);

        Assert.False(zone.IntersectsLink(new Position(x1, y1), new Position(x2, y2)));
    }

    [Fact]
    public void IntersectsLinkReturnsTrueWhenSegmentIsTangentToZone()
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0);

        // Найближча точка (50, 20) лежить рівно на відстані радіуса.
        Assert.True(zone.IntersectsLink(new Position(0, 20), new Position(100, 20)));
    }

    [Fact]
    public void IntersectsLinkReturnsFalseWhenSegmentPassesJustOutsideZone()
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0);

        Assert.False(zone.IntersectsLink(new Position(0, 20.001), new Position(100, 20.001)));
    }

    [Fact]
    public void IntersectsLinkReturnsTrueWhenOnlyEndpointTouchesBoundary()
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0);

        // Кінець (30, 0) лежить на межі зони.
        Assert.True(zone.IntersectsLink(new Position(0, 0), new Position(30, 0)));
    }

    [Fact]
    public void IntersectsLinkReturnsTrueForDegenerateSegmentInsideZone()
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0);

        Assert.True(zone.IntersectsLink(new Position(50, 0), new Position(50, 0)));
    }

    [Fact]
    public void IntersectsLinkReturnsTrueWhenSegmentCrossesDiagonallyNearCenter()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 10.0, 1.0);

        // Найближча точка (0, 5) — всередині радіуса 10.
        Assert.True(zone.IntersectsLink(new Position(-20, 5), new Position(20, 5)));
    }

    [Fact]
    public void IntersectsLinkReturnsTrueWhenSegmentCompletelyInsideZone()
    {
        var zone = new JammingZone("jam-1", new Position(0, 0), 50.0, 1.0);

        Assert.True(zone.IntersectsLink(new Position(-5, 0), new Position(5, 0)));
    }

    [Theory]
    [InlineData(0.0, 0.0, 100.0, 0.0)]
    [InlineData(0.0, 20.0, 100.0, 20.0)]
    [InlineData(0.0, 0.0, 20.0, 0.0)]
    [InlineData(0.0, 50.0, 100.0, 50.0)]
    [InlineData(80.0, 0.0, 100.0, 0.0)]
    public void IntersectsLinkIsSymmetricRegardlessOfSegmentDirection(double x1, double y1, double x2, double y2)
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0);
        var start = new Position(x1, y1);
        var end = new Position(x2, y2);

        Assert.Equal(zone.IntersectsLink(start, end), zone.IntersectsLink(end, start));
    }

    [Fact]
    public void IntersectsLinkFollowsZoneAfterMoveTo()
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0);
        var start = new Position(0, 0);
        var end = new Position(100, 0);
        Assert.True(zone.IntersectsLink(start, end));

        zone.MoveTo(new Position(50, 500));

        Assert.False(zone.IntersectsLink(start, end));
    }

    [Fact]
    public void IntersectsLinkReflectsReactivation()
    {
        var zone = new JammingZone("jam-1", new Position(50, 0), 20.0, 1.0, isActive: false);
        var start = new Position(0, 0);
        var end = new Position(100, 0);
        Assert.False(zone.IntersectsLink(start, end));

        zone.IsActive = true;

        Assert.True(zone.IntersectsLink(start, end));
    }
}