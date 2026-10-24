using MeshSim.Core;

namespace MeshSim.Engine.IO;

/// <summary>
/// Будує свіжу топологію та новий рушій зі сценарію. Кожен виклик повертає незалежні об'єкти,
/// тому перезапуск симуляції це просто повторна побудова (Reset).
/// </summary>
public static class ScenarioFactory
{
    public static ScenarioRuntime Build(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ScenarioValidator.EnsureValid(scenario);

        var topology = new NetworkTopology();

        foreach (NodeDefinition node in scenario.Nodes)
        {
            topology.AddNode(new MeshNode(
                node.Id,
                new Position(node.X, node.Y),
                node.TransmissionRangeMeters,
                node.BatteryLevelPercent));
        }

        foreach (JammingZoneDefinition zone in scenario.JammingZones)
        {
            topology.AddJammingZone(new JammingZone(
                zone.Id,
                new Position(zone.X, zone.Y),
                zone.RadiusMeters,
                zone.JammingIntensity,
                zone.IsActive));
        }

        var engine = new SimulationEngine(
            topology,
            RoutingStrategyRegistry.Create(scenario.RoutingStrategy),
            scenario.Engine.HopDelayMs,
            scenario.Engine.EnergyPerTransmissionPercent);

        foreach (PacketDefinition packet in scenario.Packets)
        {
            engine.SchedulePacket(
                new NetworkPacket(packet.Id, packet.SourceNodeId, packet.DestinationNodeId, packet.Ttl, packet.PayloadBytes),
                packet.ScheduledTimeMs);
        }

        return new ScenarioRuntime(topology, engine);
    }
}