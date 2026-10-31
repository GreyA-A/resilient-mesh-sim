using System.Globalization;
using MeshSim.Engine;
using MeshSim.Engine.IO;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Текстове резюме імпортованого звіту для показу користувачу.
/// </summary>
public static class ReportSummaryFormatter
{
    public static string Describe(ReportDocument report)
    {
        ArgumentNullException.ThrowIfNull(report);

        ReportMetrics metrics = report.Metrics;

        List<string> lines =
        [
            string.Concat("Звіт про прогін сценарію «", report.ScenarioName, "»"),
            string.Concat("Надіслано пакетів: ", metrics.TotalPacketsSent.ToString(CultureInfo.InvariantCulture)),
            string.Concat("Доставлено: ", metrics.TotalPacketsDelivered.ToString(CultureInfo.InvariantCulture)),
            string.Concat("Втрачено: ", metrics.TotalPacketsDropped.ToString(CultureInfo.InvariantCulture)),
            string.Concat("PDR: ", (metrics.PacketDeliveryRatio * 100.0).ToString("0.0", CultureInfo.InvariantCulture), " %"),
            string.Concat("Середня кількість стрибків: ", metrics.AverageHopCount.ToString("0.00", CultureInfo.InvariantCulture)),
            string.Concat("Подій у журналі: ", report.Events.Count.ToString(CultureInfo.InvariantCulture))
        ];

        foreach (KeyValuePair<string, int> drop in metrics.DropsByReason)
        {
            lines.Add(string.Concat("  • ", DescribeReason(drop.Key), ": ", drop.Value.ToString(CultureInfo.InvariantCulture)));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string DescribeReason(string key)
    {
        return Enum.TryParse(key, out PacketDropReason reason) && Enum.IsDefined(reason)
            ? EventTextFormatter.DescribeDropReason(reason)
            : key;
    }
}