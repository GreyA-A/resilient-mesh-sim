using MeshSim.Core;
using MeshSim.Engine;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class ScenarioFactoryTests
{
    [Fact]
    public void BuildCreatesNodesWithConfiguredProperties()
    {
        ScenarioRuntime runtime = ScenarioFactory.Build(
            Scenario(nodes: new[] { Node("A", 1.0, 2.0, range: 75.0, battery: 40.0), Node("B", 50.0) }));

        Assert.Equal(2, runtime.Topology.Nodes.Count);
        MeshNode node = runtime.Topology.Nodes["A"];
        Assert.Equal(new Position(1.0, 2.0), node.Position);
        Assert.Equal(75.0, node.TransmissionRangeMeters);
        Assert.Equal(40.0, node.BatteryLevelPercent);
    }

    [Fact]
    public void BuildCreatesJammingZonesIncludingInactiveOnes()
    {
        ScenarioRuntime runtime = ScenarioFactory.Build(Scenario(zones: new[]
        {
            Zone("on", 10.0, 20.0, 30.0, 0.5),
            Zone("off", isActive: false)
        }));

        Assert.Equal(2, runtime.Topology.JammingZones.Count);
        JammingZone zone = runtime.Topology.JammingZones[0];
        Assert.Equal("on", zone.Id);
        Assert.Equal(new Position(10.0, 20.0), zone.Center);
        Assert.Equal(30.0, zone.RadiusMeters);
        Assert.Equal(0.5, zone.JammingIntensity);
        Assert.True(zone.IsActive);
        Assert.False(runtime.Topology.JammingZones[1].IsActive);
    }

    [Fact]
    public void BuildAppliesEngineSettingsAndSchedulesPackets()
    {
        ScenarioRuntime runtime = ScenarioFactory.Build(JammedChain());

        Assert.Equal(15.0, runtime.Simulation.HopDelayMs);
        Assert.Equal(2.0, runtime.Simulation.EnergyPerTransmissionPercent);
        Assert.True(runtime.Simulation.HasPendingEvents);
    }

    [Fact]
    public void BuiltRuntimeRoutesAroundJammingAndDeliversPacket()
    {
        ScenarioRuntime runtime = ScenarioFactory.Build(JammedChain());

        SimulationResult result = runtime.Simulation.RunToCompletion();

        Assert.Equal(1.0, result.PacketDeliveryRatio);
        Assert.Equal(2.0, result.AverageHopCount);
        Assert.Equal(30.0, runtime.Simulation.CurrentTimeMs);
        Assert.Equal(98.0, runtime.Topology.Nodes["A"].BatteryLevelPercent);
        Assert.Equal(98.0, runtime.Topology.Nodes["B"].BatteryLevelPercent);
    }

    [Fact]
    public void BuildHonoursScheduledTimeOfPackets()
    {
        ScenarioRuntime runtime = ScenarioFactory.Build(Scenario(packets: new[] { Packet("p1", "A", "B", scheduledTimeMs: 5.0) }));

        SimulationResult result = runtime.Simulation.RunToCompletion();

        Assert.Equal(5.0, result.EventLog[0].TimestampMs);
    }

    [Fact]
    public void DeactivatedZoneInScenarioDoesNotBlockDirectRoute()
    {
        ScenarioDefinition scenario = Scenario(
            nodes: new[] { Node("A"), Node("B", x: 50.0, y: 60.0), Node("C", x: 100.0) },
            zones: new[] { Zone("jam-1", isActive: false) },
            packets: new[] { Packet("p1", "A", "C") });

        SimulationResult result = ScenarioFactory.Build(scenario).Simulation.RunToCompletion();

        Assert.Equal(1.0, result.AverageHopCount);
    }

    [Fact]
    public void BuildProducesIndependentRuntimesSoResetIsJustRebuild()
    {
        ScenarioDefinition definition = JammedChain();
        ScenarioRuntime first = ScenarioFactory.Build(definition);
        first.Simulation.RunToCompletion();

        ScenarioRuntime second = ScenarioFactory.Build(definition);

        Assert.NotSame(first.Topology, second.Topology);
        Assert.Equal(98.0, first.Topology.Nodes["A"].BatteryLevelPercent);
        Assert.Equal(100.0, second.Topology.Nodes["A"].BatteryLevelPercent);
        Assert.True(second.Simulation.HasPendingEvents);
        Assert.Equal(1, second.Simulation.RunToCompletion().TotalPacketsDelivered);
    }

    [Fact]
    public void BuildAcceptsRoutingStrategyNameInAnyCase()
    {
        ScenarioRuntime runtime = ScenarioFactory.Build(Scenario(routingStrategy: "dijkstra"));

        Assert.False(runtime.Simulation.HasPendingEvents);
    }

    [Fact]
    public void BuildThrowsInvalidDataExceptionForInvalidScenario()
    {
        Assert.Throws<InvalidDataException>(() => ScenarioFactory.Build(Scenario(schemaVersion: 5)));
    }

    [Fact]
    public void BuildThrowsArgumentNullExceptionForNull()
    {
        Assert.Throws<ArgumentNullException>(() => ScenarioFactory.Build(null!));
    }
}