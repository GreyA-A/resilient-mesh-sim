using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class WorldBoundsTests
{
    private static MeshNode Node(string id, double x, double y)
        => new(id, new Position(x, y), 100.0);

    [Fact]
    public void DefaultIsHundredMetreSquare()
    {
        Assert.Equal(new WorldBounds(0.0, 0.0, 100.0, 100.0), WorldBounds.Default);
    }

    [Fact]
    public void DerivedValuesFollowCorners()
    {
        var bounds = new WorldBounds(10.0, 20.0, 30.0, 60.0);

        Assert.Equal(20.0, bounds.Width);
        Assert.Equal(40.0, bounds.Height);
        Assert.Equal(20.0, bounds.CenterX);
        Assert.Equal(40.0, bounds.CenterY);
    }

    [Theory]
    [InlineData(0.0, 0.0, 1.0, 1.0, true)]
    [InlineData(double.NaN, 0.0, 1.0, 1.0, false)]
    [InlineData(0.0, double.NaN, 1.0, 1.0, false)]
    [InlineData(0.0, 0.0, double.PositiveInfinity, 1.0, false)]
    [InlineData(0.0, 0.0, 1.0, double.NegativeInfinity, false)]
    public void IsFiniteRequiresAllCornersToBeFinite(double minX, double minY, double maxX, double maxY, bool expected)
    {
        Assert.Equal(expected, new WorldBounds(minX, minY, maxX, maxY).IsFinite);
    }

    [Fact]
    public void FromTopologyReturnsDefaultForEmptyTopology()
    {
        Assert.Equal(WorldBounds.Default, WorldBounds.FromTopology(new NetworkTopology()));
    }

    [Fact]
    public void FromTopologyCoversNodePositions()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0.0, 0.0));
        topology.AddNode(Node("B", 80.0, -20.0));

        Assert.Equal(new WorldBounds(0.0, -20.0, 80.0, 0.0), WorldBounds.FromTopology(topology));
    }

    [Fact]
    public void FromTopologyCoversZonesWithTheirRadius()
    {
        var topology = new NetworkTopology();
        topology.AddJammingZone(new JammingZone("z", new Position(10.0, 10.0), 5.0, 0.5));

        Assert.Equal(new WorldBounds(5.0, 5.0, 15.0, 15.0), WorldBounds.FromTopology(topology));
    }

    [Fact]
    public void FromTopologyCombinesNodesAndZones()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0.0, 0.0));
        topology.AddJammingZone(new JammingZone("z", new Position(100.0, 0.0), 20.0, 0.5));

        Assert.Equal(new WorldBounds(0.0, -20.0, 120.0, 20.0), WorldBounds.FromTopology(topology));
    }

    [Fact]
    public void FromTopologyThrowsArgumentNullExceptionForNull()
    {
        Assert.Throws<ArgumentNullException>(() => WorldBounds.FromTopology(null!));
    }
}