using MeshSim.Core;
using MeshSim.Engine;
using Xunit;

namespace MeshSim.Tests;

public class NetworkTopologyTests
{
    private static MeshNode Node(string id, double x, double y, double range = 100.0, double battery = 100.0)
        => new(id, new Position(x, y), range, battery);

    // ---------- Константи та колекції ----------

    [Fact]
    public void JammingBlockThresholdIsPinnedToExpectedValue()
    {
        Assert.Equal(0.85, NetworkTopology.JammingBlockThreshold);
    }

    [Fact]
    public void NewTopologyIsEmpty()
    {
        var topology = new NetworkTopology();

        Assert.Empty(topology.Nodes);
        Assert.Empty(topology.JammingZones);
    }

    [Fact]
    public void AddNodeStoresNodeById()
    {
        var topology = new NetworkTopology();
        MeshNode node = Node("A", 0, 0);

        topology.AddNode(node);

        Assert.Single(topology.Nodes);
        Assert.Same(node, topology.Nodes["A"]);
    }

    [Fact]
    public void AddNodeReplacesExistingNodeWithSameId()
    {
        var topology = new NetworkTopology();
        MeshNode original = Node("A", 0, 0);
        MeshNode replacement = Node("A", 10, 10);

        topology.AddNode(original);
        topology.AddNode(replacement);

        Assert.Single(topology.Nodes);
        Assert.Same(replacement, topology.Nodes["A"]);
    }

    [Fact]
    public void AddNodeTreatsIdsAsCaseSensitive()
    {
        var topology = new NetworkTopology();

        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("a", 10, 0));

        Assert.Equal(2, topology.Nodes.Count);
    }

    [Fact]
    public void AddJammingZoneStoresZonesInInsertionOrder()
    {
        var topology = new NetworkTopology();
        var first = new JammingZone("z1", new Position(0, 0), 10.0, 0.5);
        var second = new JammingZone("z2", new Position(50, 0), 10.0, 0.5);

        topology.AddJammingZone(first);
        topology.AddJammingZone(second);

        Assert.Equal(2, topology.JammingZones.Count);
        Assert.Same(first, topology.JammingZones[0]);
        Assert.Same(second, topology.JammingZones[1]);
    }

    [Fact]
    public void AddNodeAndAddJammingZoneThrowArgumentNullExceptionForNull()
    {
        var topology = new NetworkTopology();

        Assert.Throws<ArgumentNullException>(() => topology.AddNode(null!));
        Assert.Throws<ArgumentNullException>(() => topology.AddJammingZone(null!));
    }

    // ---------- GetLinkJammingIntensity ----------

    [Fact]
    public void GetLinkJammingIntensityReturnsZeroWhenNoZonesExist()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);

        Assert.Equal(0.0, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityReturnsZeroWhenZoneDoesNotTouchLink()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 100, 0);
        topology.AddJammingZone(new JammingZone("far", new Position(50, 100), 10.0, 0.9));

        Assert.Equal(0.0, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityReturnsIntensityOfSingleCrossingZone()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 100, 0);
        topology.AddJammingZone(new JammingZone("z", new Position(50, 0), 10.0, 0.4));

        Assert.Equal(0.4, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityReturnsMaximumOfOverlappingZones()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 100, 0, range: 150.0);
        topology.AddJammingZone(new JammingZone("z1", new Position(30, 0), 10.0, 0.3));
        topology.AddJammingZone(new JammingZone("z2", new Position(70, 0), 10.0, 0.6));

        Assert.Equal(0.6, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityMaximumDoesNotDependOnZoneOrder()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 100, 0);
        topology.AddJammingZone(new JammingZone("strong", new Position(30, 0), 10.0, 0.6));
        topology.AddJammingZone(new JammingZone("weak", new Position(70, 0), 10.0, 0.3));

        Assert.Equal(0.6, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityIgnoresInactiveZones()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 100, 0);
        topology.AddJammingZone(new JammingZone("off", new Position(50, 0), 10.0, 1.0, isActive: false));

        Assert.Equal(0.0, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityIgnoresInactiveZoneButUsesActiveOne()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 100, 0);
        topology.AddJammingZone(new JammingZone("off", new Position(30, 0), 10.0, 1.0, isActive: false));
        topology.AddJammingZone(new JammingZone("on", new Position(70, 0), 10.0, 0.2));

        Assert.Equal(0.2, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityReturnsZeroForActiveZoneWithZeroIntensity()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 100, 0);
        topology.AddJammingZone(new JammingZone("harmless", new Position(50, 0), 10.0, 0.0));

        Assert.Equal(0.0, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityReactsToZoneToggleAndMovement()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 100, 0);
        var zone = new JammingZone("z", new Position(50, 0), 10.0, 0.7);
        topology.AddJammingZone(zone);
        Assert.Equal(0.7, topology.GetLinkJammingIntensity(a, b));

        zone.IsActive = false;
        Assert.Equal(0.0, topology.GetLinkJammingIntensity(a, b));

        zone.IsActive = true;
        zone.MoveTo(new Position(50, 500));
        Assert.Equal(0.0, topology.GetLinkJammingIntensity(a, b));

        zone.MoveTo(new Position(50, 0));
        Assert.Equal(0.7, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityCountsZoneThatCoversOnlyOneEndpoint()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);
        topology.AddJammingZone(new JammingZone("on-sender", new Position(0, 0), 10.0, 0.5));

        Assert.Equal(0.5, topology.GetLinkJammingIntensity(a, b));
    }

    [Fact]
    public void GetLinkJammingIntensityThrowsArgumentNullExceptionForNullNodes()
    {
        var topology = new NetworkTopology();
        MeshNode node = Node("A", 0, 0);

        Assert.Throws<ArgumentNullException>(() => topology.GetLinkJammingIntensity(null!, node));
        Assert.Throws<ArgumentNullException>(() => topology.GetLinkJammingIntensity(node, null!));
    }

    // ---------- CalculateLinkQuality ----------

    [Fact]
    public void CalculateLinkQualityReturnsOneWhenLinkIsInRangeAndClean()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);

        Assert.Equal(1.0, topology.CalculateLinkQuality(a, b));
    }

    [Fact]
    public void CalculateLinkQualityReturnsZeroWhenOutOfRange()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0, range: 50.0);
        MeshNode b = Node("B", 100, 0, range: 50.0);

        Assert.Equal(0.0, topology.CalculateLinkQuality(a, b));
    }

    [Fact]
    public void CalculateLinkQualityReturnsZeroWhenReceiverBatteryIsDepleted()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0, battery: 0.0);

        Assert.Equal(0.0, topology.CalculateLinkQuality(a, b));
    }

    [Fact]
    public void CalculateLinkQualityReturnsZeroWhenSenderBatteryIsDepleted()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0, battery: 0.0);
        MeshNode b = Node("B", 50, 0);

        Assert.Equal(0.0, topology.CalculateLinkQuality(a, b));
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.25, 0.75)]
    [InlineData(0.5, 0.5)]
    [InlineData(0.75, 0.25)]
    [InlineData(0.84, 0.16)]
    public void CalculateLinkQualityEqualsOneMinusIntensityBelowThreshold(double intensity, double expectedQuality)
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);
        topology.AddJammingZone(new JammingZone("z", new Position(25, 0), 10.0, intensity));

        Assert.Equal(expectedQuality, topology.CalculateLinkQuality(a, b), precision: 9);
    }

    [Theory]
    [InlineData(0.85)]
    [InlineData(0.9)]
    [InlineData(1.0)]
    public void CalculateLinkQualityReturnsZeroAtOrAboveThreshold(double intensity)
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);
        topology.AddJammingZone(new JammingZone("z", new Position(25, 0), 10.0, intensity));

        Assert.Equal(0.0, topology.CalculateLinkQuality(a, b));
    }

    [Fact]
    public void CalculateLinkQualityAffectsOnlyLinksCrossingTheZone()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);
        MeshNode c = Node("C", 0, 50);
        topology.AddJammingZone(new JammingZone("z", new Position(25, 0), 5.0, 0.6));

        Assert.Equal(0.4, topology.CalculateLinkQuality(a, b), precision: 9);
        Assert.Equal(1.0, topology.CalculateLinkQuality(a, c), precision: 9);
    }

    [Fact]
    public void CalculateLinkQualityRecoversWhenZoneMovesAway()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);
        var zone = new JammingZone("z", new Position(25, 0), 10.0, 1.0);
        topology.AddJammingZone(zone);
        Assert.Equal(0.0, topology.CalculateLinkQuality(a, b));

        zone.MoveTo(new Position(25, 500));

        Assert.Equal(1.0, topology.CalculateLinkQuality(a, b));
    }

    [Fact]
    public void CalculateLinkQualityIsDirectionalWhenRangesDiffer()
    {
        var topology = new NetworkTopology();
        MeshNode strong = Node("S", 0, 0, range: 100.0);
        MeshNode weak = Node("W", 80, 0, range: 50.0);

        Assert.Equal(1.0, topology.CalculateLinkQuality(strong, weak));
        Assert.Equal(0.0, topology.CalculateLinkQuality(weak, strong));
    }

    [Fact]
    public void CalculateLinkQualityThrowsArgumentNullExceptionForNullNodes()
    {
        var topology = new NetworkTopology();
        MeshNode node = Node("A", 0, 0);

        Assert.Throws<ArgumentNullException>(() => topology.CalculateLinkQuality(null!, node));
        Assert.Throws<ArgumentNullException>(() => topology.CalculateLinkQuality(node, null!));
    }

    // ---------- IsLinkAvailable ----------

    [Fact]
    public void IsLinkAvailableReturnsTrueForCleanInRangeLink()
    {
        var topology = new NetworkTopology();

        Assert.True(topology.IsLinkAvailable(Node("A", 0, 0), Node("B", 50, 0)));
    }

    [Fact]
    public void IsLinkAvailableReturnsTrueForPartiallyJammedLink()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);
        topology.AddJammingZone(new JammingZone("z", new Position(25, 0), 10.0, 0.5));

        Assert.True(topology.IsLinkAvailable(a, b));
    }

    [Theory]
    [InlineData(0.84, true)]
    [InlineData(0.85, false)]
    [InlineData(1.0, false)]
    public void IsLinkAvailableRespectsInclusiveBlockThreshold(double intensity, bool expectedAvailable)
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);
        topology.AddJammingZone(new JammingZone("z", new Position(25, 0), 10.0, intensity));

        Assert.Equal(expectedAvailable, topology.IsLinkAvailable(a, b));
    }

    [Fact]
    public void IsLinkAvailableBecomesTrueAfterBlockingZoneIsDeactivated()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0);
        MeshNode b = Node("B", 50, 0);
        var zone = new JammingZone("z", new Position(25, 0), 10.0, 1.0);
        topology.AddJammingZone(zone);
        Assert.False(topology.IsLinkAvailable(a, b));

        zone.IsActive = false;

        Assert.True(topology.IsLinkAvailable(a, b));
    }

    [Fact]
    public void IsLinkAvailableReturnsFalseForOutOfRangeNodes()
    {
        var topology = new NetworkTopology();

        Assert.False(topology.IsLinkAvailable(Node("A", 0, 0, range: 10.0), Node("B", 500, 0, range: 10.0)));
    }

    [Fact]
    public void IsLinkAvailableThrowsArgumentNullExceptionForNullNodes()
    {
        var topology = new NetworkTopology();
        MeshNode node = Node("A", 0, 0);

        Assert.Throws<ArgumentNullException>(() => topology.IsLinkAvailable(null!, node));
        Assert.Throws<ArgumentNullException>(() => topology.IsLinkAvailable(node, null!));
    }
}