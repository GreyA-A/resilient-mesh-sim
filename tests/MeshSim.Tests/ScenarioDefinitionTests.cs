using MeshSim.Engine;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class ScenarioDefinitionTests
{
    [Fact]
    public void NewScenarioHasDocumentedDefaults()
    {
        var scenario = new ScenarioDefinition();

        Assert.Equal(1, ScenarioDefinition.CurrentSchemaVersion);
        Assert.Equal(0, scenario.SchemaVersion);
        Assert.Equal(string.Empty, scenario.Name);
        Assert.Equal("Dijkstra", scenario.RoutingStrategy);
        Assert.Equal(SimulationEngine.DefaultHopDelayMs, scenario.Engine.HopDelayMs);
        Assert.Equal(SimulationEngine.DefaultEnergyPerTransmissionPercent, scenario.Engine.EnergyPerTransmissionPercent);
        Assert.Empty(scenario.Nodes);
        Assert.Empty(scenario.JammingZones);
        Assert.Empty(scenario.Packets);
    }

    [Fact]
    public void NodeDefaultsToFullBatteryAndZoneDefaultsToActive()
    {
        var node = new NodeDefinition();
        var zone = new JammingZoneDefinition();

        Assert.Equal(100.0, node.BatteryLevelPercent);
        Assert.True(zone.IsActive);
    }

    [Fact]
    public void InitializersStoreEveryValue()
    {
        ScenarioDefinition scenario = JammedChain();

        Assert.Equal("Jammed chain", scenario.Name);
        Assert.Equal(15.0, scenario.Engine.HopDelayMs);
        Assert.Equal(2.0, scenario.Engine.EnergyPerTransmissionPercent);

        NodeDefinition node = scenario.Nodes[1];
        Assert.Equal("B", node.Id);
        Assert.Equal(50.0, node.X);
        Assert.Equal(60.0, node.Y);
        Assert.Equal(120.0, node.TransmissionRangeMeters);
        Assert.Equal(100.0, node.BatteryLevelPercent);

        JammingZoneDefinition zone = scenario.JammingZones[0];
        Assert.Equal("jam-1", zone.Id);
        Assert.Equal(50.0, zone.X);
        Assert.Equal(0.0, zone.Y);
        Assert.Equal(25.0, zone.RadiusMeters);
        Assert.Equal(1.0, zone.JammingIntensity);
        Assert.True(zone.IsActive);

        PacketDefinition packet = scenario.Packets[0];
        Assert.Equal("p1", packet.Id);
        Assert.Equal("A", packet.SourceNodeId);
        Assert.Equal("C", packet.DestinationNodeId);
        Assert.Equal(5, packet.Ttl);
        Assert.Equal(64, packet.PayloadBytes);
        Assert.Equal(0.0, packet.ScheduledTimeMs);
    }
}