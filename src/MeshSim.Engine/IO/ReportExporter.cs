using System.Text;
using System.Text.Json;

namespace MeshSim.Engine.IO;

/// <summary>
/// Експорт результату симуляції у JSON із супутнім файлом контрольної суми.
/// </summary>
public static class ReportExporter
{
    /// <summary>
    /// Перетворює результат прогону на документ звіту (причини втрат упорядковано за значенням enum).
    /// </summary>
    public static ReportDocument CreateDocument(SimulationResult result, string scenarioName)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(scenarioName);

        Dictionary<string, int> drops = result.DropsByReason
            .OrderBy(pair => (int)pair.Key)
            .ToDictionary(pair => pair.Key.ToString(), pair => pair.Value, StringComparer.Ordinal);

        List<ReportEvent> events = result.EventLog
            .Select(logged => new ReportEvent
            {
                TimestampMs = logged.TimestampMs,
                EventType = logged.EventType.ToString(),
                PacketId = logged.Packet.Id,
                SourceNodeId = logged.Packet.SourceNodeId,
                DestinationNodeId = logged.Packet.DestinationNodeId,
                CurrentNodeId = logged.CurrentNodeId,
                DropReason = logged.DropReason.ToString()
            })
            .ToList();

        return new ReportDocument
        {
            SchemaVersion = ReportDocument.CurrentSchemaVersion,
            ScenarioName = scenarioName,
            Metrics = new ReportMetrics
            {
                TotalPacketsSent = result.TotalPacketsSent,
                TotalPacketsDelivered = result.TotalPacketsDelivered,
                TotalPacketsDropped = result.TotalPacketsDropped,
                AverageHopCount = result.AverageHopCount,
                PacketDeliveryRatio = result.PacketDeliveryRatio,
                DropsByReason = drops
            },
            Events = events
        };
    }

    public static string ToJson(SimulationResult result, string scenarioName)
    {
        return JsonSerializer.Serialize(CreateDocument(result, scenarioName), ReportJson.Options);
    }

    /// <summary>
    /// Атомарно записує JSON-звіт і файл контрольної суми «шлях.sha256».
    /// </summary>
    public static void SaveJson(string path, SimulationResult result, string scenarioName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        SaveWithChecksum(path, ToJson(result, scenarioName));
    }

    /// <summary>
    /// Записує вміст і супутній файл контрольної суми, обчисленої від тих самих байтів.
    /// Спершу пишеться звіт, потім сума: збій між кроками лишає звіт без суми, і при імпорті
    /// він буде відхилений, а не прийнятий без перевірки.
    /// </summary>
    internal static void SaveWithChecksum(string path, string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);

        AtomicFileWriter.WriteAllBytes(path, bytes);
        AtomicFileWriter.WriteAllText(ReportChecksum.GetSidecarPath(path), ReportChecksum.Compute(bytes) + "\n");
    }
}