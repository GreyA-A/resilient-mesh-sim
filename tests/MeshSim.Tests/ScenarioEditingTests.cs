using MeshSim.Desktop.Editing;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class ScenarioEditingTests
{
    // JammedChain: вузли A, B, C; зона jam-1; пакет p1.

    private static void AssertMetadataPreserved(ScenarioDefinition original, ScenarioDefinition edited)
    {
        Assert.Equal(original.SchemaVersion, edited.SchemaVersion);
        Assert.Equal(original.Name, edited.Name);
        Assert.Equal(original.RoutingStrategy, edited.RoutingStrategy);
        Assert.Same(original.Engine, edited.Engine);
    }

    // ---------- Вузли ----------

    [Fact]
    public void AddNodeAppendsNodeWithoutTouchingOriginalOrOtherSections()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition edited = ScenarioEditing.AddNode(original, Node("D", x: 10.0));

        Assert.Equal(3, original.Nodes.Count);
        Assert.Equal(4, edited.Nodes.Count);
        Assert.Equal("D", edited.Nodes[3].Id);
        Assert.Same(original.JammingZones, edited.JammingZones);
        Assert.Same(original.Packets, edited.Packets);
        AssertMetadataPreserved(original, edited);
    }

    [Fact]
    public void MoveNodeChangesOnlyPositionOfMatchingNode()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition edited = ScenarioEditing.MoveNode(original, "B", 5.5, 6.5);

        NodeDefinition moved = edited.Nodes[1];
        Assert.Equal("B", moved.Id);
        Assert.Equal(5.5, moved.X);
        Assert.Equal(6.5, moved.Y);
        Assert.Equal(120.0, moved.TransmissionRangeMeters);
        Assert.Equal(100.0, moved.BatteryLevelPercent);
        Assert.Equal(50.0, original.Nodes[1].X);
        Assert.Same(original.Nodes[0], edited.Nodes[0]);
        Assert.Same(original.Nodes[2], edited.Nodes[2]);
        Assert.Same(original.JammingZones, edited.JammingZones);
        Assert.Same(original.Packets, edited.Packets);
        AssertMetadataPreserved(original, edited);
    }

    [Fact]
    public void MoveNodeWithUnknownIdKeepsAllNodes()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition edited = ScenarioEditing.MoveNode(original, "GHOST", 1.0, 1.0);

        Assert.Equal(3, edited.Nodes.Count);
        Assert.Same(original.Nodes[1], edited.Nodes[1]);
    }

    [Fact]
    public void RemoveNodeDropsOnlyMatchingNode()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition edited = ScenarioEditing.RemoveNode(original, "B");

        Assert.Equal(3, original.Nodes.Count);
        string[] expectedIds = ["A", "C"];
        Assert.Equal(expectedIds, edited.Nodes.Select(node => node.Id));
        Assert.Same(original.Packets, edited.Packets);
        AssertMetadataPreserved(original, edited);
    }

    [Fact]
    public void RemoveNodeWithUnknownIdKeepsAllNodes()
    {
        Assert.Equal(3, ScenarioEditing.RemoveNode(JammedChain(), "GHOST").Nodes.Count);
    }

    // ---------- Зони ----------

    [Fact]
    public void AddZoneAppendsZoneWithoutTouchingOtherSections()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition edited = ScenarioEditing.AddZone(original, Zone("jam-2", x: 10.0));

        Assert.Single(original.JammingZones);
        Assert.Equal(2, edited.JammingZones.Count);
        Assert.Equal("jam-2", edited.JammingZones[1].Id);
        Assert.Same(original.Nodes, edited.Nodes);
        Assert.Same(original.Packets, edited.Packets);
        AssertMetadataPreserved(original, edited);
    }

    [Fact]
    public void MoveZoneChangesOnlyCenterOfMatchingZone()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition edited = ScenarioEditing.MoveZone(original, "jam-1", 7.0, 8.0);

        JammingZoneDefinition moved = edited.JammingZones[0];
        Assert.Equal(7.0, moved.X);
        Assert.Equal(8.0, moved.Y);
        Assert.Equal(25.0, moved.RadiusMeters);
        Assert.Equal(1.0, moved.JammingIntensity);
        Assert.True(moved.IsActive);
        Assert.Equal(50.0, original.JammingZones[0].X);
        Assert.Same(original.Nodes, edited.Nodes);
    }

    [Fact]
    public void MoveZoneWithUnknownIdKeepsZone()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition edited = ScenarioEditing.MoveZone(original, "GHOST", 1.0, 1.0);

        Assert.Same(original.JammingZones[0], edited.JammingZones[0]);
    }

    [Fact]
    public void RemoveZoneDropsMatchingZoneAndIgnoresUnknownId()
    {
        ScenarioDefinition original = JammedChain();

        Assert.Empty(ScenarioEditing.RemoveZone(original, "jam-1").JammingZones);
        Assert.Single(ScenarioEditing.RemoveZone(original, "GHOST").JammingZones);
    }

    [Fact]
    public void ToggleZoneFlipsActivityAndPreservesOtherProperties()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition off = ScenarioEditing.ToggleZone(original, "jam-1");
        ScenarioDefinition backOn = ScenarioEditing.ToggleZone(off, "jam-1");

        Assert.False(off.JammingZones[0].IsActive);
        Assert.Equal(50.0, off.JammingZones[0].X);
        Assert.Equal(0.0, off.JammingZones[0].Y);
        Assert.Equal(25.0, off.JammingZones[0].RadiusMeters);
        Assert.Equal(1.0, off.JammingZones[0].JammingIntensity);
        Assert.True(original.JammingZones[0].IsActive);
        Assert.True(backOn.JammingZones[0].IsActive);
    }

    [Fact]
    public void ToggleZoneWithUnknownIdKeepsZone()
    {
        ScenarioDefinition original = JammedChain();

        Assert.Same(original.JammingZones[0], ScenarioEditing.ToggleZone(original, "GHOST").JammingZones[0]);
    }

    // ---------- Пакети ----------

    [Fact]
    public void AddPacketAppendsPacketWithoutTouchingOtherSections()
    {
        ScenarioDefinition original = JammedChain();

        ScenarioDefinition edited = ScenarioEditing.AddPacket(original, Packet("p2", "A", "B"));

        Assert.Single(original.Packets);
        Assert.Equal(2, edited.Packets.Count);
        Assert.Equal("p2", edited.Packets[1].Id);
        Assert.Same(original.Nodes, edited.Nodes);
        Assert.Same(original.JammingZones, edited.JammingZones);
        AssertMetadataPreserved(original, edited);
    }

    [Fact]
    public void RemovePacketDropsMatchingPacketAndIgnoresUnknownId()
    {
        ScenarioDefinition original = JammedChain();

        Assert.Empty(ScenarioEditing.RemovePacket(original, "p1").Packets);
        Assert.Single(ScenarioEditing.RemovePacket(original, "GHOST").Packets);
    }

    // ---------- Аргументи ----------

    [Fact]
    public void OperationsThrowArgumentNullExceptionForNullArguments()
    {
        ScenarioDefinition scenario = JammedChain();

        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.AddNode(null!, Node("D")));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.AddNode(scenario, null!));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.MoveNode(null!, "A", 0, 0));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.RemoveNode(null!, "A"));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.AddZone(null!, Zone("z")));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.AddZone(scenario, null!));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.MoveZone(null!, "z", 0, 0));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.RemoveZone(null!, "z"));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.ToggleZone(null!, "z"));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.AddPacket(null!, Packet("p")));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.AddPacket(scenario, null!));
        Assert.Throws<ArgumentNullException>(() => ScenarioEditing.RemovePacket(null!, "p"));
    }
}