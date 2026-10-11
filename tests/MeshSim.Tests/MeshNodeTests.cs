using MeshSim.Core;
using Xunit;

namespace MeshSim.Tests;

public class MeshNodeTests
{
    [Fact]
    public void CanReachReturnsTrueWhenNodeIsWithinRange()
    {
        var nodeA = new MeshNode("node-a", new Position(0, 0), 100.0);
        var nodeB = new MeshNode("node-b", new Position(50, 0), 100.0);

        Assert.True(nodeA.CanReach(nodeB));
    }

    [Fact]
    public void CanReachReturnsFalseWhenNodeIsOutOfRange()
    {
        var nodeA = new MeshNode("node-a", new Position(0, 0), 50.0);
        var nodeB = new MeshNode("node-b", new Position(100, 0), 50.0);

        Assert.False(nodeA.CanReach(nodeB));
    }

    [Fact]
    public void CanReachReturnsFalseWhenNodeHasDrainedBattery()
    {
        var nodeA = new MeshNode("node-a", new Position(0, 0), 100.0);
        var nodeB = new MeshNode("node-b", new Position(50, 0), 100.0, initialBatteryLevelPercent: 0.0);

        Assert.False(nodeA.CanReach(nodeB));
    }

    [Fact]
    public void ConsumeEnergyReducesBatteryAndClampsAtZero()
    {
        var node = new MeshNode("node-a", new Position(0, 0), transmissionRangeMeters: 100.0, initialBatteryLevelPercent: 50.0);

        node.ConsumeEnergy(20.0);
        Assert.Equal(30.0, node.BatteryLevelPercent);

        node.ConsumeEnergy(40.0);
        Assert.Equal(0.0, node.BatteryLevelPercent);
        Assert.False(node.isActive);
    }

    [Theory]
    [InlineData(-10.0)]
    [InlineData(double.NaN)]
    public void ConsumeEnergyThrowsArgumentOutOfRangeExceptionWhenAmountIsInvalid(double amount)
    {
        var node = new MeshNode("node-1", new Position(0, 0), transmissionRangeMeters: 100.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => node.ConsumeEnergy(amount));
    }

    [Theory]
    [InlineData(-5.0)]
    [InlineData(110.0)]
    [InlineData(double.NaN)]
    public void ConstructorThrowsArgumentOutOfRangeExceptionWhenInitialBatteryIsInvalid(double battery)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MeshNode("node-1", new Position(0, 0), transmissionRangeMeters: 100.0, initialBatteryLevelPercent: battery));
    }
}
