using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class PacketMarkerTests
{
    private static NetworkTopology Topology()
    {
        var topology = new NetworkTopology();
        topology.AddNode(new MeshNode("A", new Position(0, 0), 100.0));
        topology.AddNode(new MeshNode("B", new Position(80, 0), 100.0));
        return topology;
    }

    /// <summary>
    /// Перетворення: екран = (2x, 200 - 2y).
    /// </summary>
    private static ViewportTransform Transform()
        => ViewportTransform.Fit(new WorldBounds(0.0, 0.0, 100.0, 100.0), 200.0, 200.0, 0.0);

    private static PacketActivity Activity(
        string node = "B",
        string? previous = null,
        SimulationEventType type = SimulationEventType.PacketForwarded,
        PacketDropReason reason = PacketDropReason.None)
        => new("p1", node, previous, type, reason);

    // ---------- PacketActivity ----------

    [Fact]
    public void PacketActivityStoresAllValues()
    {
        var activity = new PacketActivity("p9", "B", "A", SimulationEventType.PacketDropped, PacketDropReason.TtlExpired);

        Assert.Equal("p9", activity.PacketId);
        Assert.Equal("B", activity.NodeId);
        Assert.Equal("A", activity.PreviousNodeId);
        Assert.Equal(SimulationEventType.PacketDropped, activity.EventType);
        Assert.Equal(PacketDropReason.TtlExpired, activity.DropReason);
    }

    // ---------- MarkerStyleCalculator ----------

    [Fact]
    public void MarkerStyleMapsEveryEventTypeToItsColour()
    {
        Assert.Equal(MapPalette.MarkerQueued, MarkerStyleCalculator.ForEvent(SimulationEventType.PacketEnqueued));
        Assert.Equal(MapPalette.MarkerForwarded, MarkerStyleCalculator.ForEvent(SimulationEventType.PacketForwarded));
        Assert.Equal(MapPalette.MarkerDelivered, MarkerStyleCalculator.ForEvent(SimulationEventType.PacketDelivered));
        Assert.Equal(MapPalette.MarkerDropped, MarkerStyleCalculator.ForEvent(SimulationEventType.PacketDropped));
    }

    [Fact]
    public void MarkerStyleThrowsForUnknownEventType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkerStyleCalculator.ForEvent((SimulationEventType)99));
    }

    // ---------- MapScene ----------

    [Fact]
    public void SceneWithoutActivityHasNoMarkerOrSegment()
    {
        Assert.Null(MapScene.Empty.Marker);
        Assert.Null(MapScene.Empty.Segment);
    }

    [Fact]
    public void SceneStoresMarkerAndSegment()
    {
        var marker = new PacketMarkerVisual("p1", "p1", new ScreenPoint(1, 2), 15.0, SimulationEventType.PacketEnqueued, MapPalette.MarkerQueued);
        var segment = new ActiveSegmentVisual(new ScreenPoint(0, 0), new ScreenPoint(5, 5), MapPalette.MarkerQueued, 5.0);

        var scene = new MapScene([], [], [], marker, segment);

        Assert.Equal(marker, scene.Marker);
        Assert.Equal(segment, scene.Segment);
    }

    // ---------- MapSceneBuilder ----------

    [Fact]
    public void BuildWithoutActivityProducesNoMarkerOrSegment()
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform());

        Assert.Null(scene.Marker);
        Assert.Null(scene.Segment);
    }

    [Fact]
    public void BuildPlacesMarkerOnActivityNode()
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), Activity());

        Assert.True(scene.Marker.HasValue);
        PacketMarkerVisual marker = scene.Marker.GetValueOrDefault();
        Assert.Equal("p1", marker.PacketId);
        Assert.Equal("p1", marker.Caption);
        Assert.Equal(new ScreenPoint(160.0, 200.0), marker.Position);
        Assert.Equal(MapSceneBuilder.MarkerRadiusPx, marker.RadiusPx);
        Assert.Equal(SimulationEventType.PacketForwarded, marker.EventType);
        Assert.Equal(MapPalette.MarkerForwarded, marker.Color);
        Assert.Null(scene.Segment);
    }

    [Fact]
    public void BuildCreatesSegmentFromPreviousNodeToActivityNode()
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), Activity(previous: "A"));

        Assert.True(scene.Segment.HasValue);
        ActiveSegmentVisual segment = scene.Segment.GetValueOrDefault();
        Assert.Equal(new ScreenPoint(0.0, 200.0), segment.From);
        Assert.Equal(new ScreenPoint(160.0, 200.0), segment.To);
        Assert.Equal(MapPalette.MarkerForwarded, segment.Color);
        Assert.Equal(MapSceneBuilder.SegmentThicknessPx, segment.Thickness);
    }

    [Fact]
    public void BuildOmitsSegmentWhenPreviousNodeEqualsActivityNode()
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), Activity(previous: "B"));

        Assert.True(scene.Marker.HasValue);
        Assert.Null(scene.Segment);
    }

    [Fact]
    public void BuildOmitsSegmentWhenPreviousNodeIsUnknown()
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), Activity(previous: "GHOST"));

        Assert.True(scene.Marker.HasValue);
        Assert.Null(scene.Segment);
    }

    [Fact]
    public void BuildOmitsMarkerAndSegmentWhenActivityNodeIsUnknown()
    {
        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), Activity(node: "GHOST", previous: "A"));

        Assert.Null(scene.Marker);
        Assert.Null(scene.Segment);
    }

    [Fact]
    public void BuildCaptionIncludesDropReasonForDroppedPacket()
    {
        PacketActivity dropped = Activity(type: SimulationEventType.PacketDropped, reason: PacketDropReason.NoRouteFound);

        MapScene scene = MapSceneBuilder.Build(Topology(), Transform(), dropped);

        PacketMarkerVisual marker = scene.Marker.GetValueOrDefault();
        Assert.Equal("p1: немає маршруту", marker.Caption);
        Assert.Equal(MapPalette.MarkerDropped, marker.Color);
    }
}