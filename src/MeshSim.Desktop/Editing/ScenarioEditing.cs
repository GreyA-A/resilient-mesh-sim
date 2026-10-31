using MeshSim.Engine.IO;

namespace MeshSim.Desktop.Editing;

/// <summary>
/// Чисті операції редагування незмінного сценарію: кожна повертає новий ScenarioDefinition,
/// не змінюючи вихідний. Незмінені розділи передаються за тим самим посиланням.
/// Операції не валідують результат: це робить ScenarioValidator у викликачі.
/// Невідомий Id не є помилкою, у такому разі склад списку не змінюється.
/// </summary>
public static class ScenarioEditing
{
    // ---------- Вузли ----------

    public static ScenarioDefinition AddNode(ScenarioDefinition scenario, NodeDefinition node)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(node);

        return Rebuild(scenario, [.. scenario.Nodes, node], scenario.JammingZones, scenario.Packets);
    }

    public static ScenarioDefinition MoveNode(ScenarioDefinition scenario, string nodeId, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        List<NodeDefinition> nodes = scenario.Nodes
            .Select(node => IsId(node.Id, nodeId)
                ? new NodeDefinition
                {
                    Id = node.Id,
                    X = x,
                    Y = y,
                    TransmissionRangeMeters = node.TransmissionRangeMeters,
                    BatteryLevelPercent = node.BatteryLevelPercent
                }
                : node)
            .ToList();

        return Rebuild(scenario, nodes, scenario.JammingZones, scenario.Packets);
    }

    public static ScenarioDefinition RemoveNode(ScenarioDefinition scenario, string nodeId)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        List<NodeDefinition> nodes = scenario.Nodes.Where(node => !IsId(node.Id, nodeId)).ToList();

        return Rebuild(scenario, nodes, scenario.JammingZones, scenario.Packets);
    }

    // ---------- Зони завад ----------

    public static ScenarioDefinition AddZone(ScenarioDefinition scenario, JammingZoneDefinition zone)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(zone);

        return Rebuild(scenario, scenario.Nodes, [.. scenario.JammingZones, zone], scenario.Packets);
    }

    public static ScenarioDefinition MoveZone(ScenarioDefinition scenario, string zoneId, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        List<JammingZoneDefinition> zones = scenario.JammingZones
            .Select(zone => IsId(zone.Id, zoneId)
                ? new JammingZoneDefinition
                {
                    Id = zone.Id,
                    X = x,
                    Y = y,
                    RadiusMeters = zone.RadiusMeters,
                    JammingIntensity = zone.JammingIntensity,
                    IsActive = zone.IsActive
                }
                : zone)
            .ToList();

        return Rebuild(scenario, scenario.Nodes, zones, scenario.Packets);
    }

    public static ScenarioDefinition RemoveZone(ScenarioDefinition scenario, string zoneId)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        List<JammingZoneDefinition> zones = scenario.JammingZones.Where(zone => !IsId(zone.Id, zoneId)).ToList();

        return Rebuild(scenario, scenario.Nodes, zones, scenario.Packets);
    }

    public static ScenarioDefinition ToggleZone(ScenarioDefinition scenario, string zoneId)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        List<JammingZoneDefinition> zones = scenario.JammingZones
            .Select(zone => IsId(zone.Id, zoneId)
                ? new JammingZoneDefinition
                {
                    Id = zone.Id,
                    X = zone.X,
                    Y = zone.Y,
                    RadiusMeters = zone.RadiusMeters,
                    JammingIntensity = zone.JammingIntensity,
                    IsActive = !zone.IsActive
                }
                : zone)
            .ToList();

        return Rebuild(scenario, scenario.Nodes, zones, scenario.Packets);
    }

    // ---------- Пакети ----------

    public static ScenarioDefinition AddPacket(ScenarioDefinition scenario, PacketDefinition packet)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(packet);

        return Rebuild(scenario, scenario.Nodes, scenario.JammingZones, [.. scenario.Packets, packet]);
    }

    public static ScenarioDefinition RemovePacket(ScenarioDefinition scenario, string packetId)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        List<PacketDefinition> packets = scenario.Packets.Where(packet => !IsId(packet.Id, packetId)).ToList();

        return Rebuild(scenario, scenario.Nodes, scenario.JammingZones, packets);
    }

    private static bool IsId(string candidate, string id)
    {
        return string.Equals(candidate, id, StringComparison.Ordinal);
    }

    private static ScenarioDefinition Rebuild(
        ScenarioDefinition source,
        IReadOnlyList<NodeDefinition> nodes,
        IReadOnlyList<JammingZoneDefinition> zones,
        IReadOnlyList<PacketDefinition> packets)
    {
        return new ScenarioDefinition
        {
            SchemaVersion = source.SchemaVersion,
            Name = source.Name,
            RoutingStrategy = source.RoutingStrategy,
            Engine = source.Engine,
            Nodes = nodes,
            JammingZones = zones,
            Packets = packets
        };
    }
}