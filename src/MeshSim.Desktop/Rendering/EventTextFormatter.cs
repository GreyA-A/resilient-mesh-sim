using System.Globalization;
using MeshSim.Engine;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Українські текстові описи подій симуляції та причин втрат.
/// </summary>
public static class EventTextFormatter
{
    public static string DescribeEventType(SimulationEventType eventType)
    {
        return eventType switch
        {
            SimulationEventType.PacketEnqueued => "Поставлено в чергу",
            SimulationEventType.PacketForwarded => "Передано на вузол",
            SimulationEventType.PacketDelivered => "Доставлено",
            SimulationEventType.PacketDropped => "Втрачено",
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Невідомий тип події.")
        };
    }

    public static string DescribeDropReason(PacketDropReason reason)
    {
        return reason switch
        {
            PacketDropReason.None => "без причини",
            PacketDropReason.NoRouteFound => "немає маршруту",
            PacketDropReason.JammedInTransit => "канал заглушено завадами",
            PacketDropReason.TtlExpired => "вичерпано TTL",
            PacketDropReason.NodeBatteryDepleted => "розряджено батарею вузла",
            PacketDropReason.BufferOverflow => "переповнено буфер вузла",
            PacketDropReason.NodeNotFound => "вузол не знайдено",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Невідома причина втрати.")
        };
    }

    /// <summary>
    /// Формує рядок журналу: «час | пакет | подія | вузол [| причина втрати]».
    /// </summary>
    public static string FormatEvent(SimulationEvent simulationEvent)
    {
        string head = string.Concat(
            simulationEvent.TimestampMs.ToString("0.0", CultureInfo.InvariantCulture),
            " мс | ",
            simulationEvent.Packet.Id,
            " | ",
            DescribeEventType(simulationEvent.EventType),
            " | вузол ",
            simulationEvent.CurrentNodeId);

        return simulationEvent.EventType == SimulationEventType.PacketDropped
            ? string.Concat(head, " | ", DescribeDropReason(simulationEvent.DropReason))
            : head;
    }
}