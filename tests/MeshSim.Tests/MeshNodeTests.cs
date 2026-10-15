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
        Assert.False(node.IsActive);
    }

    [Theory]
    [InlineData(-10.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConsumeEnergyThrowsArgumentOutOfRangeExceptionWhenAmountIsInvalid(double amount)
    {
        var node = new MeshNode("node-1", new Position(0, 0), transmissionRangeMeters: 100.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => node.ConsumeEnergy(amount));
    }

    [Theory]
    [InlineData(-5.0)]
    [InlineData(110.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConstructorThrowsArgumentOutOfRangeExceptionWhenInitialBatteryIsInvalid(double battery)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MeshNode("node-1", new Position(0, 0), transmissionRangeMeters: 100.0, initialBatteryLevelPercent: battery));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-50.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConstructorThrowsArgumentOutOfRangeExceptionWhenRangeIsInvalid(double range)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MeshNode("node-1", new Position(0, 0), transmissionRangeMeters: range));
    }

    // ---------- Constructor ----------

    [Fact]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var node = new MeshNode("node-1", new Position(5, 6), 75.0, 40.0);

        Assert.Equal("node-1", node.Id);
        Assert.Equal(new Position(5, 6), node.Position);
        Assert.Equal(75.0, node.TransmissionRangeMeters);
        Assert.Equal(40.0, node.BatteryLevelPercent);
        Assert.True(node.IsActive);
    }

    [Fact]
    public void ConstructorDefaultsBatteryToFull()
    {
        var node = new MeshNode("node-1", new Position(0, 0), 100.0);

        Assert.Equal(100.0, node.BatteryLevelPercent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorThrowsArgumentExceptionWhenIdIsEmpty(string id)
    {
        Assert.Throws<ArgumentException>(() => new MeshNode(id, new Position(0, 0), 100.0));
    }

    [Fact]
    public void ConstructorThrowsArgumentExceptionWhenIdIsNull()
    {
        Assert.Throws<ArgumentException>(() => new MeshNode(null!, new Position(0, 0), 100.0));
    }

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(100.0, true)]
    [InlineData(0.001, true)]
    public void ConstructorAcceptsBatteryBoundaryValuesAndSetsActivity(double battery, bool expectedActive)
    {
        var node = new MeshNode("node-1", new Position(0, 0), 100.0, battery);

        Assert.Equal(battery, node.BatteryLevelPercent);
        Assert.Equal(expectedActive, node.IsActive);
    }

    // ---------- CanReach ----------

    [Fact]
    public void CanReachReturnsTrueWhenDistanceEqualsRangeExactly()
    {
        var nodeA = new MeshNode("node-a", new Position(0, 0), 100.0);
        var nodeB = new MeshNode("node-b", new Position(100, 0), 100.0);

        Assert.True(nodeA.CanReach(nodeB));
    }

    [Fact]
    public void CanReachReturnsFalseWhenDistanceIsJustBeyondRange()
    {
        var nodeA = new MeshNode("node-a", new Position(0, 0), 100.0);
        var nodeB = new MeshNode("node-b", new Position(100.001, 0), 100.0);

        Assert.False(nodeA.CanReach(nodeB));
    }

    [Fact]
    public void CanReachReturnsTrueForActiveNodeTargetingItself()
    {
        var node = new MeshNode("node-a", new Position(0, 0), 10.0);

        Assert.True(node.CanReach(node));
    }

    [Fact]
    public void CanReachReturnsFalseForDrainedNodeTargetingItself()
    {
        var node = new MeshNode("node-a", new Position(0, 0), 10.0, initialBatteryLevelPercent: 0.0);

        Assert.False(node.CanReach(node));
    }

    [Fact]
    public void CanReachReturnsFalseWhenSenderHasDrainedBattery()
    {
        var sender = new MeshNode("node-a", new Position(0, 0), 100.0, initialBatteryLevelPercent: 0.0);
        var receiver = new MeshNode("node-b", new Position(50, 0), 100.0);

        Assert.False(sender.CanReach(receiver));
    }

    [Fact]
    public void CanReachIsDirectionalWhenTransmissionRangesDiffer()
    {
        var strong = new MeshNode("strong", new Position(0, 0), 100.0);
        var weak = new MeshNode("weak", new Position(80, 0), 50.0);

        Assert.True(strong.CanReach(weak));
        Assert.False(weak.CanReach(strong));
    }

    [Fact]
    public void CanReachThrowsArgumentNullExceptionWhenTargetIsNull()
    {
        var node = new MeshNode("node-a", new Position(0, 0), 100.0);

        Assert.Throws<ArgumentNullException>(() => node.CanReach(null!));
    }

    [Fact]
    public void CanReachReflectsPositionChanges()
    {
        var nodeA = new MeshNode("node-a", new Position(0, 0), 100.0);
        var nodeB = new MeshNode("node-b", new Position(50, 0), 100.0);
        Assert.True(nodeA.CanReach(nodeB));

        nodeB.Position = new Position(500, 0);

        Assert.False(nodeA.CanReach(nodeB));
    }

    [Fact]
    public void CanReachBecomesFalseAfterBatteryIsDepleted()
    {
        var nodeA = new MeshNode("node-a", new Position(0, 0), 100.0);
        var nodeB = new MeshNode("node-b", new Position(50, 0), 100.0, initialBatteryLevelPercent: 10.0);
        Assert.True(nodeA.CanReach(nodeB));

        nodeB.ConsumeEnergy(10.0);

        Assert.False(nodeA.CanReach(nodeB));
        Assert.False(nodeB.CanReach(nodeA));
    }

    // ---------- ConsumeEnergy ----------

    [Fact]
    public void ConsumeEnergyWithZeroLeavesBatteryUnchanged()
    {
        var node = new MeshNode("node-a", new Position(0, 0), 100.0, 60.0);

        node.ConsumeEnergy(0.0);

        Assert.Equal(60.0, node.BatteryLevelPercent);
        Assert.True(node.IsActive);
    }

    [Fact]
    public void ConsumeEnergyDeactivatesNodeWhenBatteryReachesExactlyZero()
    {
        var node = new MeshNode("node-a", new Position(0, 0), 100.0, 50.0);

        node.ConsumeEnergy(50.0);

        Assert.Equal(0.0, node.BatteryLevelPercent);
        Assert.False(node.IsActive);
    }

    [Fact]
    public void ConsumeEnergyKeepsNodeActiveWhileSomeChargeRemains()
    {
        var node = new MeshNode("node-a", new Position(0, 0), 100.0, 50.0);

        node.ConsumeEnergy(49.5);

        Assert.Equal(0.5, node.BatteryLevelPercent, precision: 9);
        Assert.True(node.IsActive);
    }

    [Fact]
    public void ConsumeEnergyOnDrainedNodeStaysAtZero()
    {
        var node = new MeshNode("node-a", new Position(0, 0), 100.0, 0.0);

        node.ConsumeEnergy(10.0);

        Assert.Equal(0.0, node.BatteryLevelPercent);
        Assert.False(node.IsActive);
    }
}