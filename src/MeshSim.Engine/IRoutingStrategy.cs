namespace MeshSim.Engine;

/// <summary>
/// Визначає контракт алгоритму маршрутизації пакетів у mesh-мережі (патерн Strategy).
/// </summary>
public interface IRoutingStrategy
{
    /// <summary>
    /// Знаходить оптимальний маршрут (послідовність ідентифікаторів вузлів включно з відправником та отримувачем).
    /// Повертає порожній список, якщо маршрут недоступний.
    /// </summary>
    IReadOnlyList<string> FindRoute(NetworkTopology topology, string sourceNodeId, string destinationNodeId);
}