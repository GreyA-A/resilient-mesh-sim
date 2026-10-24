using MeshSim.Engine.IO;

namespace MeshSim.Tests;

internal static class ScenarioTestData
{
    public static NodeDefinition Node(
        string id,
        double x = 0.0,
        double y = 0.0,
        double range = 120.0,
        double battery = 100.0)
        => new() { Id = id, X = x, Y = y, TransmissionRangeMeters = range, BatteryLevelPercent = battery };

    public static JammingZoneDefinition Zone(
        string id,
        double x = 50.0,
        double y = 0.0,
        double radius = 25.0,
        double intensity = 1.0,
        bool isActive = true)
        => new()
        {
            Id = id,
            X = x,
            Y = y,
            RadiusMeters = radius,
            JammingIntensity = intensity,
            IsActive = isActive
        };

    public static PacketDefinition Packet(
        string id,
        string source = "A",
        string destination = "B",
        int ttl = 5,
        int payloadBytes = 64,
        double scheduledTimeMs = 0.0)
        => new()
        {
            Id = id,
            SourceNodeId = source,
            DestinationNodeId = destination,
            Ttl = ttl,
            PayloadBytes = payloadBytes,
            ScheduledTimeMs = scheduledTimeMs
        };

    public static ScenarioDefinition Scenario(
        IReadOnlyList<NodeDefinition>? nodes = null,
        IReadOnlyList<JammingZoneDefinition>? zones = null,
        IReadOnlyList<PacketDefinition>? packets = null,
        int schemaVersion = ScenarioDefinition.CurrentSchemaVersion,
        string name = "Test scenario",
        string routingStrategy = "Dijkstra",
        EngineSettingsDefinition? engine = null)
        => new()
        {
            SchemaVersion = schemaVersion,
            Name = name,
            RoutingStrategy = routingStrategy,
            Engine = engine ?? new EngineSettingsDefinition(),
            Nodes = nodes ?? new[] { Node("A"), Node("B", x: 50.0) },
            JammingZones = zones ?? Array.Empty<JammingZoneDefinition>(),
            Packets = packets ?? Array.Empty<PacketDefinition>()
        };

    /// <summary>
    /// Канонічний сценарій: пряма лінія A–C заглушена, пакет іде в обхід через B.
    /// </summary>
    public static ScenarioDefinition JammedChain()
        => Scenario(
            nodes: new[] { Node("A"), Node("B", x: 50.0, y: 60.0), Node("C", x: 100.0) },
            zones: new[] { Zone("jam-1") },
            packets: new[] { Packet("p1", "A", "C") },
            name: "Jammed chain",
            engine: new EngineSettingsDefinition { HopDelayMs = 15.0, EnergyPerTransmissionPercent = 2.0 });
}