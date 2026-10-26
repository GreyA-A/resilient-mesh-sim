using System.Globalization;

namespace MeshSim.Engine.IO;

/// <summary>
/// Перевіряє цілісність і узгодженість документа звіту після читання з файлу.
/// </summary>
public static class ReportValidator
{
    private const double RatioTolerance = 1e-9;

    private static readonly Dictionary<string, SimulationEventType> EventTypesByName =
        Enum.GetValues<SimulationEventType>().ToDictionary(type => type.ToString(), StringComparer.Ordinal);

    private static readonly Dictionary<string, PacketDropReason> DropReasonsByName =
        Enum.GetValues<PacketDropReason>().ToDictionary(reason => reason.ToString(), StringComparer.Ordinal);

    /// <summary>
    /// Повертає список усіх знайдених помилок (порожній, якщо звіт коректний).
    /// </summary>
    public static IReadOnlyList<string> Validate(ReportDocument report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var errors = new List<string>();

        if (report.SchemaVersion != ReportDocument.CurrentSchemaVersion)
        {
            errors.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"Непідтримувана версія схеми звіту: {report.SchemaVersion} (очікується {ReportDocument.CurrentSchemaVersion})."));
            return errors;
        }

        ValidateMetrics(report.Metrics, errors);
        ValidateEvents(report.Events, errors);

        return errors;
    }

    private static void ValidateMetrics(ReportMetrics? metrics, List<string> errors)
    {
        if (metrics is null)
        {
            errors.Add("Секція 'metrics' відсутня.");
            return;
        }

        var drops = new Dictionary<PacketDropReason, int>();

        if (metrics.DropsByReason is null)
        {
            errors.Add("metrics.dropsByReason відсутній.");
        }
        else
        {
            foreach (KeyValuePair<string, int> pair in metrics.DropsByReason)
            {
                if (DropReasonsByName.TryGetValue(pair.Key, out PacketDropReason reason) && reason != PacketDropReason.None)
                {
                    drops[reason] = pair.Value;
                }
                else
                {
                    errors.Add($"metrics.dropsByReason містить невідому причину втрати '{pair.Key}'.");
                }
            }
        }

        try
        {
            // Інваріанти (delivered + dropped <= sent, сума втрат за причинами тощо)
            // перевіряє конструктор доменного класу: єдине джерело правил.
            _ = new SimulationResult(
                metrics.TotalPacketsSent,
                metrics.TotalPacketsDelivered,
                metrics.TotalPacketsDropped,
                metrics.AverageHopCount,
                drops,
                Array.Empty<SimulationEvent>());
        }
        catch (ArgumentException exception)
        {
            errors.Add($"Метрики суперечливі: {exception.Message}");
        }

        double expectedRatio = metrics.TotalPacketsSent <= 0
            ? 0.0
            : (double)metrics.TotalPacketsDelivered / metrics.TotalPacketsSent;
        bool ratioConsistent = double.IsFinite(metrics.PacketDeliveryRatio)
            && Math.Abs(metrics.PacketDeliveryRatio - expectedRatio) <= RatioTolerance;

        if (!ratioConsistent)
        {
            errors.Add("metrics.packetDeliveryRatio не відповідає відношенню доставлених до відправлених.");
        }
    }

    private static void ValidateEvents(IReadOnlyList<ReportEvent?>? events, List<string> errors)
    {
        if (events is null)
        {
            errors.Add("Список 'events' відсутній.");
            return;
        }

        double previousTimestamp = 0.0;

        for (int i = 0; i < events.Count; i++)
        {
            ReportEvent? reportEvent = events[i];
            string location = string.Concat("events[", i.ToString(CultureInfo.InvariantCulture), "]");

            if (reportEvent is null)
            {
                errors.Add($"{location}: елемент не може бути null.");
                continue;
            }

            if (!double.IsFinite(reportEvent.TimestampMs) || reportEvent.TimestampMs < 0.0)
            {
                errors.Add($"{location}: timestampMs має бути невід'ємним скінченним числом.");
            }
            else
            {
                if (reportEvent.TimestampMs < previousTimestamp)
                {
                    errors.Add($"{location}: порушено хронологічний порядок подій.");
                }

                previousTimestamp = reportEvent.TimestampMs;
            }

            bool typeKnown = EventTypesByName.TryGetValue(reportEvent.EventType ?? string.Empty, out SimulationEventType eventType);
            bool reasonKnown = DropReasonsByName.TryGetValue(reportEvent.DropReason ?? string.Empty, out PacketDropReason dropReason);

            if (!typeKnown)
            {
                errors.Add($"{location}: невідомий eventType '{reportEvent.EventType}'.");
            }

            if (!reasonKnown)
            {
                errors.Add($"{location}: невідомий dropReason '{reportEvent.DropReason}'.");
            }

            if (typeKnown && reasonKnown
                && (eventType == SimulationEventType.PacketDropped) != (dropReason != PacketDropReason.None))
            {
                errors.Add($"{location}: подія PacketDropped має мати причину втрати, а інші події ні.");
            }

            if (string.IsNullOrWhiteSpace(reportEvent.PacketId)
                || string.IsNullOrWhiteSpace(reportEvent.SourceNodeId)
                || string.IsNullOrWhiteSpace(reportEvent.DestinationNodeId)
                || string.IsNullOrWhiteSpace(reportEvent.CurrentNodeId))
            {
                errors.Add($"{location}: ідентифікатори пакета та вузлів не можуть бути порожніми.");
            }
        }
    }
}