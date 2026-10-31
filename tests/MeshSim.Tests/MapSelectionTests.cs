using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class MapSelectionTests
{
    /// <summary>
    /// Перетворення: екран = (2x, 200 - 2y).
    /// </summary>
    private static ViewportTransform Transform()
        => ViewportTransform.Fit(new WorldBounds(0.0, 0.0, 100.0, 100.0), 200.0, 200.0, 0.0);

    private static NetworkTopology Topology()
    {
        var topology = new NetworkTopology();
        topology.AddNode(new MeshNode("A", new Position(0, 0), 100.0));
        topology.AddJammingZone(new JammingZone("Z", new Position(10, 10), 5.0, 0.5));
        return topology;
    }

    [Fact]
    public void SelectionVisualStoresCenterAndRadius()
    {
        var visual = new SelectionVisual(new ScreenPoint(1, 2), 3.0);

        Assert.Equal(new ScreenPoint(1, 2), visual.Center);
        Assert.Equal(3.0, visual.RadiusPx);
    }

    [Fact]
    public void SceneStoresSelection()
    {
        var selection = new SelectionVisual(new ScreenPoint(1, 2), 3.0);

        var scene = new MapScene([], [], [], null, null, selection);

        Assert.Equal(selection, scene.Selection);
        Assert.Null(MapScene.Empty.Selection);
    }

    [Fact]
    public void BuildWithoutSelectionHasNoRing()
    {
        Assert.Null(MapSceneBuilder.Build(Topology(), Transform()).Selection);
    }

    [Fact]
    public void BuildCreatesRingAroundSelectedNode()
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), null, new MapTarget(MapTargetKind.Node, "A"));

        SelectionVisual ring = scene.Selection.GetValueOrDefault();
        Assert.True(scene.Selection.HasValue);
        Assert.Equal(new ScreenPoint(0.0, 200.0), ring.Center);
        Assert.Equal(MapSceneBuilder.NodeRadiusPx + MapSceneBuilder.SelectionMarginPx, ring.RadiusPx);
    }

    [Fact]
    public void BuildCreatesRingAroundSelectedZone()
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), null, new MapTarget(MapTargetKind.Zone, "Z"));

        SelectionVisual ring = scene.Selection.GetValueOrDefault();
        Assert.True(scene.Selection.HasValue);
        Assert.Equal(new ScreenPoint(20.0, 180.0), ring.Center);
        Assert.Equal(10.0 + MapSceneBuilder.SelectionMarginPx, ring.RadiusPx);
    }

    [Theory]
    [InlineData(MapTargetKind.Node, "GHOST")]
    [InlineData(MapTargetKind.Zone, "GHOST")]
    public void BuildOmitsRingForMissingTarget(MapTargetKind kind, string id)
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), null, new MapTarget(kind, id));

        Assert.Null(scene.Selection);
    }
}