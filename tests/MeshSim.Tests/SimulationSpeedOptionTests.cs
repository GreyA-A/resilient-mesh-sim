using MeshSim.Desktop.ViewModels;

namespace MeshSim.Tests;

public class SimulationSpeedOptionTests
{
    [Fact]
    public void ConstructorStoresLabelAndMultiplier()
    {
        var option = new SimulationSpeedOption("2×", 2.0);

        Assert.Equal("2×", option.Label);
        Assert.Equal(2.0, option.Multiplier);
    }

    [Theory]
    [InlineData(0.25, 3200)]
    [InlineData(1.0, 800)]
    [InlineData(2.0, 400)]
    [InlineData(4.0, 200)]
    public void GetIntervalDividesBaseIntervalByMultiplier(double multiplier, int expectedMilliseconds)
    {
        var option = new SimulationSpeedOption("x", multiplier);

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), option.GetInterval(TimeSpan.FromMilliseconds(800)));
    }

    [Fact]
    public void GetIntervalNeverReturnsZero()
    {
        var option = new SimulationSpeedOption("huge", 1e12);

        Assert.Equal(TimeSpan.FromTicks(1), option.GetInterval(TimeSpan.FromTicks(1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsBlankLabel(string? label)
    {
        Assert.ThrowsAny<ArgumentException>(() => new SimulationSpeedOption(label!, 1.0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidMultiplier(double multiplier)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationSpeedOption("x", multiplier));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void GetIntervalRejectsNonPositiveBaseInterval(int milliseconds)
    {
        var option = new SimulationSpeedOption("x", 1.0);

        Assert.Throws<ArgumentOutOfRangeException>(() => option.GetInterval(TimeSpan.FromMilliseconds(milliseconds)));
    }
}