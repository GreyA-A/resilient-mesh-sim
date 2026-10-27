using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class MapSceneTests
{
    private static MeshNode Node(string id, double x, double y, double range = 100.0, double battery = 100.0)
        => new(id, new Position(x, y), range, battery);

    /// <summary>
    /// Перетворення: екран = (2x, 200 - 2y).
    /// </summary>
    private static ViewportTransform Transform()
        => ViewportTransform.Fit(new WorldBounds(0.0, 0.0, 100.0, 100.0), 200.0, 200.0, 0.0);

    // ---------- MapScene ----------

    [Fact]
    public void EmptySceneHasNoPrimitives()
    {
        Assert.Empty(MapScene.Empty.Links);
        Assert.Empty(MapScene.Empty.Nodes);
        Assert.Empty(MapScene.Empty.Zones);
    }

    [Fact]
    public void SceneConstructorRejectsNullCollections()
    {
        Assert.Throws<ArgumentNullException>(() => new MapScene(null!, [], []));
        Assert.Throws<ArgumentNullException>(() => new MapScene([], null!, []));
        Assert.Throws<ArgumentNullException>(() => new MapScene([], [], null!));
    }

    // ---------- Побудова ----------

    [Fact]
    public void BuildCreatesVisualsForNodesZonesAndInRangeLinks()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 80, 0));
        topology.AddNode(Node("D", 1000, 0));
        topology.AddJammingZone(new JammingZone("far", new Position(500, 500), 10.0, 0.5));

        MapScene scene = MapSceneBuilder.Build(topology, Transform());

        Assert.Equal(3, scene.Nodes.Count);
        Assert.Single(scene.Zones);
        LinkVisual link = Assert.Single(scene.Links);
        Assert.Equal("A", link.FromId);
        Assert.Equal("B", link.ToId);
        Assert.Equal(new ScreenPoint(0.0, 200.0), link.From);
        Assert.Equal(new ScreenPoint(160.0, 200.0), link.To);
        Assert.Equal(1.0, link.Quality);
        Assert.False(link.Style.IsDashed);
    }

    [Fact]
    public void BuildMapsNodeGeometryAndBatteryText()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 10, 20, range: 50.0, battery: 35.0));

        NodeVisual node = Assert.Single(MapSceneBuilder.Build(topology, Transform()).Nodes);

        Assert.Equal("A", node.Id);
        Assert.Equal(new ScreenPoint(20.0, 160.0), node.Center);
        Assert.Equal(MapSceneBuilder.NodeRadiusPx, node.RadiusPx);
        Assert.Equal(100.0, node.RangeRadiusPx);
        Assert.Equal("35%", node.BatteryText);
        Assert.True(node.Style.IsActive);
    }

    [Fact]
    public void BuildOrdersNodesAndLinksById()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("C", 0, 40));
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 0, 20));

        MapScene scene = MapSceneBuilder.Build(topology, Transform());

        string[] expectedNodeOrder = ["A", "B", "C"];
        Assert.Equal(expectedNodeOrder, scene.Nodes.Select(node => node.Id));
        (string, string)[] expectedLinkOrder = [("A", "B"), ("A", "C"), ("B", "C")];
        Assert.Equal(expectedLinkOrder, scene.Links.Select(link => (link.FromId, link.ToId)));
    }

    [Fact]
    public void BuildMapsZoneGeometryAndStyle()
    {
        var topology = new NetworkTopology();
        topology.AddJammingZone(new JammingZone("on", new Position(10, 10), 5.0, 0.5));
        topology.AddJammingZone(new JammingZone("off", new Position(20, 20), 5.0, 1.0, isActive: false));

        MapScene scene = MapSceneBuilder.Build(topology, Transform());

        Assert.Equal("on", scene.Zones[0].Id);
        Assert.Equal(new ScreenPoint(20.0, 180.0), scene.Zones[0].Center);
        Assert.Equal(10.0, scene.Zones[0].RadiusPx);
        Assert.True(scene.Zones[0].Style.IsActive);
        Assert.False(scene.Zones[1].Style.IsActive);
    }

    // ---------- Якість і видимість лінків ----------

    [Fact]
    public void BuildKeepsFullyJammedInRangeLinkAsDashedBlockedLink()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 80, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(40, 0), 10.0, 1.0));

        LinkVisual link = Assert.Single(MapSceneBuilder.Build(topology, Transform()).Links);

        Assert.Equal(0.0, link.Quality);
        Assert.True(link.Style.IsDashed);
        Assert.Equal(MapPalette.LinkBlocked, link.Style.Color);
    }

    [Fact]
    public void BuildReducesLinkQualityForPartialJamming()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 80, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(40, 0), 10.0, 0.5));

        LinkVisual link = Assert.Single(MapSceneBuilder.Build(topology, Transform()).Links);

        Assert.Equal(0.5, link.Quality, precision: 9);
        Assert.False(link.Style.IsDashed);
    }

    [Fact]
    public void BuildOmitsLinkWhenNeitherNodeReachesTheOther()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 50.0));
        topology.AddNode(Node("B", 80, 0, range: 50.0));

        Assert.Empty(MapSceneBuilder.Build(topology, Transform()).Links);
    }

    [Fact]
    public void BuildShowsLinkWhenOnlyTheSecondNodeReachesTheFirst()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 50.0));
        topology.AddNode(Node("B", 80, 0, range: 200.0));

        LinkVisual link = Assert.Single(MapSceneBuilder.Build(topology, Transform()).Links);

        Assert.Equal(1.0, link.Quality);
    }

    [Fact]
    public void BuildOmitsLinksOfDepletedNodesAndMarksThemInactive()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, battery: 0.0));
        topology.AddNode(Node("B", 80, 0));

        MapScene scene = MapSceneBuilder.Build(topology, Transform());

        Assert.Empty(scene.Links);
        Assert.False(scene.Nodes[0].Style.IsActive);
        Assert.True(scene.Nodes[1].Style.IsActive);
    }

    // ---------- Граничні випадки ----------

    [Fact]
    public void BuildReturnsEmptyPrimitivesForEmptyTopology()
    {
        MapScene scene = MapSceneBuilder.Build(new NetworkTopology(), Transform());

        Assert.Empty(scene.Nodes);
        Assert.Empty(scene.Links);
        Assert.Empty(scene.Zones);
    }

    [Fact]
    public void BuildThrowsArgumentNullExceptionForNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => MapSceneBuilder.Build(null!, Transform()));
        Assert.Throws<ArgumentNullException>(() => MapSceneBuilder.Build(new NetworkTopology(), null!));
    }
}