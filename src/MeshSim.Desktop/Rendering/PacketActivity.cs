using MeshSim.Engine;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Остання подія пакета для візуалізації: де він зараз, звідки прийшов і що з ним сталося.
/// </summary>
public readonly record struct PacketActivity(
    string PacketId,
    string NodeId,
    string? PreviousNodeId,
    SimulationEventType EventType,
    PacketDropReason DropReason);