using MeshSim.Engine;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Колір маркера пакета за типом події.
/// </summary>
public static class MarkerStyleCalculator
{
    public static RgbaColor ForEvent(SimulationEventType eventType)
    {
        return eventType switch
        {
            SimulationEventType.PacketEnqueued => MapPalette.MarkerQueued,
            SimulationEventType.PacketForwarded => MapPalette.MarkerForwarded,
            SimulationEventType.PacketDelivered => MapPalette.MarkerDelivered,
            SimulationEventType.PacketDropped => MapPalette.MarkerDropped,
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Невідомий тип події.")
        };
    }
}