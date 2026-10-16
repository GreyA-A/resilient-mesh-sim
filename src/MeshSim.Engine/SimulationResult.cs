namespace MeshSim.Engine;

/// <summary>
/// Агрегує підсумкові метрики та журнал подій завершеного прогону симуляції.
/// </summary>
public sealed class SimulationResult
{
    public int TotalPacketsSent { get; }
    public int TotalPacketsDelivered { get; }
    public int TotalPacketsDropped { get; }
    public double AverageHopCount { get; }
    public IReadOnlyDictionary<PacketDropReason, int> DropsByReason { get; }
    public IReadOnlyList<SimulationEvent> EventLog { get; }

    /// <summary>
    /// Коефіцієнт успішної доставки пакетів (PDR — Packet Delivery Ratio) у межах від 0.0 до 1.0.
    /// </summary>
    public double PacketDeliveryRatio => TotalPacketsSent == 0 
        ? 0.0 
        : (double)TotalPacketsDelivered / TotalPacketsSent;

    public SimulationResult(
        int totalPacketsSent,
        int totalPacketsDelivered,
        int totalPacketsDropped,
        double averageHopCount,
        IReadOnlyDictionary<PacketDropReason, int> dropsByReason,
        IReadOnlyList<SimulationEvent> eventLog)
    {
        if (totalPacketsSent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPacketsSent), "Кількість відправлених пакетів не може бути від'ємною.");
        }

        if (totalPacketsDelivered < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPacketsDelivered), "Кількість доставлених пакетів не може бути від'ємною.");
        }

        if (totalPacketsDropped < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPacketsDropped), "Кількість втрачених пакетів не може бути від'ємною.");
        }

        if (averageHopCount < 0.0 || double.IsNaN(averageHopCount) || double.IsInfinity(averageHopCount))
        {
            throw new ArgumentOutOfRangeException(nameof(averageHopCount), "Середня кількість стрибків має бути невід'ємним скінченним числом.");
        }

        ArgumentNullException.ThrowIfNull(dropsByReason);
        ArgumentNullException.ThrowIfNull(eventLog);

        if ((long)totalPacketsDelivered + totalPacketsDropped > totalPacketsSent)
        {
            throw new ArgumentException("Сума доставлених і втрачених пакетів не може перевищувати кількість відправлених.");
        }

        if (dropsByReason.Values.Any(count => count < 0))
        {
            throw new ArgumentException("Кількість втрат за причиною не може бути від'ємною.", nameof(dropsByReason));
        }

        if (dropsByReason.Values.Sum(count => (long)count) != totalPacketsDropped)
        {
            throw new ArgumentException("Сума втрат за причинами має дорівнювати загальній кількості втрачених пакетів.", nameof(dropsByReason));
        }

        TotalPacketsSent = totalPacketsSent;
        TotalPacketsDelivered = totalPacketsDelivered;
        TotalPacketsDropped = totalPacketsDropped;
        AverageHopCount = averageHopCount;
        DropsByReason = dropsByReason;
        EventLog = eventLog;
    }
}