namespace MeshSim.Engine.IO;

/// <summary>
/// Опис сценарію симуляції у вигляді, придатному для збереження у файл.
/// З цього опису щоразу будується свіжа топологія та новий рушій.
/// </summary>
public sealed class ScenarioDefinition
{
    /// <summary>
    /// Поточна версія схеми файлу сценарію.
    /// </summary>
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; init; }
    public string Name { get; init; } = string.Empty;
    public string RoutingStrategy { get; init; } = RoutingStrategyRegistry.Dijkstra;
    public EngineSettingsDefinition Engine { get; init; } = new();
    public IReadOnlyList<NodeDefinition> Nodes { get; init; } = [];
    public IReadOnlyList<JammingZoneDefinition> JammingZones { get; init; } = [];
    public IReadOnlyList<PacketDefinition> Packets { get; init; } = [];
}

/// <summary>
/// Параметри рушія симуляції.
/// </summary>
public sealed class EngineSettingsDefinition
{
    public double HopDelayMs { get; init; } = SimulationEngine.DefaultHopDelayMs;
    public double EnergyPerTransmissionPercent { get; init; } = SimulationEngine.DefaultEnergyPerTransmissionPercent;
}

/// <summary>
/// Опис вузла mesh-мережі.
/// </summary>
public sealed class NodeDefinition
{
    public string Id { get; init; } = string.Empty;
    public double X { get; init; }
    public double Y { get; init; }
    public double TransmissionRangeMeters { get; init; }
    public double BatteryLevelPercent { get; init; } = 100.0;
}

/// <summary>
/// Опис кругової зони радіоелектронних завад.
/// </summary>
public sealed class JammingZoneDefinition
{
    public string Id { get; init; } = string.Empty;
    public double X { get; init; }
    public double Y { get; init; }
    public double RadiusMeters { get; init; }
    public double JammingIntensity { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Опис запланованого пакета. Посилається на вузли за їхніми Id (зовнішні ключі).
/// </summary>
public sealed class PacketDefinition
{
    public string Id { get; init; } = string.Empty;
    public string SourceNodeId { get; init; } = string.Empty;
    public string DestinationNodeId { get; init; } = string.Empty;
    public int Ttl { get; init; }
    public int PayloadBytes { get; init; }
    public double ScheduledTimeMs { get; init; }
}