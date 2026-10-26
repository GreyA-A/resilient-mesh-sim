using System.Globalization;
using System.Text;

namespace MeshSim.Engine.IO;

/// <summary>
/// Експорт журналу подій симуляції у CSV (один рядок на подію).
/// </summary>
public static class CsvReportExporter
{
    private const string Header = "timestampMs,eventType,packetId,sourceNodeId,destinationNodeId,currentNodeId,dropReason";
    private const string LineBreak = "\r\n";

    public static string ToCsv(SimulationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.Append(Header).Append(LineBreak);

        foreach (SimulationEvent logged in result.EventLog)
        {
            string row = string.Join(
                ',',
                logged.TimestampMs.ToString(CultureInfo.InvariantCulture),
                CsvField.Escape(logged.EventType.ToString()),
                CsvField.Escape(logged.Packet.Id),
                CsvField.Escape(logged.Packet.SourceNodeId),
                CsvField.Escape(logged.Packet.DestinationNodeId),
                CsvField.Escape(logged.CurrentNodeId),
                CsvField.Escape(logged.DropReason.ToString()));

            builder.Append(row).Append(LineBreak);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Атомарно записує CSV і файл контрольної суми «шлях.sha256».
    /// </summary>
    public static void SaveCsv(string path, SimulationResult result)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(path);

        ReportExporter.SaveWithChecksum(path, ToCsv(result));
    }
}