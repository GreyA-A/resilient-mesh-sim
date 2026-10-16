using MeshSim.Core;

namespace MeshSim.Engine;

/// <summary>
/// Тип дискретної події в життєвому циклі передавання пакета.
/// </summary>
public enum SimulationEventType
{
    PacketEnqueued = 0,
    PacketForwarded = 1,
    PacketDelivered = 2,
    PacketDropped = 3
}

/// <summary>
/// Представляє окрему дискретну подію у віртуальному часі симуляції.
/// </summary>
public readonly record struct SimulationEvent
{
    public double TimestampMs { get; }
    public SimulationEventType EventType { get; }
    public NetworkPacket Packet { get; }
    public string CurrentNodeId { get; }
    public PacketDropReason DropReason { get; }

    public SimulationEvent(
        double timestampMs,
        SimulationEventType eventType,
        NetworkPacket packet,
        string currentNodeId,
        PacketDropReason dropReason = PacketDropReason.None)
    {
        if (timestampMs < 0.0 || double.IsNaN(timestampMs) || double.IsInfinity(timestampMs))
        {
            throw new ArgumentOutOfRangeException(nameof(timestampMs), "Час події має бути невід'ємним скінченним числом.");
        }

        ArgumentNullException.ThrowIfNull(packet);

        if (string.IsNullOrWhiteSpace(currentNodeId))
        {
            throw new ArgumentException("Ідентифікатор поточного вузла не може бути порожнім.", nameof(currentNodeId));
        }

        TimestampMs = timestampMs;
        EventType = eventType;
        Packet = packet;
        CurrentNodeId = currentNodeId;
        DropReason = dropReason;
    }
}