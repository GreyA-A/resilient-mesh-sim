namespace MeshSim.Engine.IO;

/// <summary>
/// Документ звіту про прогін симуляції у вигляді, придатному для збереження у JSON.
/// </summary>
public sealed class ReportDocument
{
    /// <summary>
    /// Поточна версія схеми файлу звіту.
    /// </summary>
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; init; }
    public string ScenarioName { get; init; } = string.Empty;
    public ReportMetrics Metrics { get; init; } = new();
    public IReadOnlyList<ReportEvent> Events { get; init; } = [];
}

/// <summary>
/// Підсумкові метрики прогону.
/// </summary>
public sealed class ReportMetrics
{
    public int TotalPacketsSent { get; init; }
    public int TotalPacketsDelivered { get; init; }
    public int TotalPacketsDropped { get; init; }
    public double AverageHopCount { get; init; }
    public double PacketDeliveryRatio { get; init; }

    /// <summary>
    /// Кількість втрат за причиною; ключ це ім'я елемента PacketDropReason.
    /// </summary>
    public IReadOnlyDictionary<string, int> DropsByReason { get; init; } = new Dictionary<string, int>();
}

/// <summary>
/// Одна подія журналу. Містить лише незмінні відомості про пакет.
/// </summary>
public sealed class ReportEvent
{
    public double TimestampMs { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string PacketId { get; init; } = string.Empty;
    public string SourceNodeId { get; init; } = string.Empty;
    public string DestinationNodeId { get; init; } = string.Empty;
    public string CurrentNodeId { get; init; } = string.Empty;
    public string DropReason { get; init; } = nameof(PacketDropReason.None);
}