namespace MeshSim.Engine;

/// <summary>
/// Визначає причину втрати (відкидання) пакета під час симуляції.
/// </summary>
public enum PacketDropReason
{
    None = 0,
    NoRouteFound = 1,
    JammedInTransit = 2,
    TtlExpired = 3,
    NodeBatteryDepleted = 4,
    BufferOverflow = 5
}