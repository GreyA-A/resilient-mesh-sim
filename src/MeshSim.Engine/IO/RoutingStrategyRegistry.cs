namespace MeshSim.Engine.IO;

/// <summary>
/// Реєстр алгоритмів маршрутизації за рядковим ключем (без урахування регістру).
/// Дає змогу зберігати вибір стратегії у файлі сценарію та підтримує ADR-006 (патерн Strategy).
/// </summary>
public static class RoutingStrategyRegistry
{
    public const string Dijkstra = "Dijkstra";

    private static readonly Dictionary<string, Func<IRoutingStrategy>> Factories = new(StringComparer.OrdinalIgnoreCase)
    {
        [Dijkstra] = () => new DijkstraRoutingStrategy()
    };

    /// <summary>
    /// Імена всіх зареєстрованих стратегій.
    /// </summary>
    public static IReadOnlyCollection<string> Names => Factories.Keys;

    /// <summary>
    /// Перевіряє, чи зареєстрована стратегія з указаним іменем.
    /// </summary>
    public static bool IsKnown(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && Factories.ContainsKey(name);
    }

    /// <summary>
    /// Створює новий екземпляр стратегії за іменем.
    /// </summary>
    public static IRoutingStrategy Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Factories.TryGetValue(name, out Func<IRoutingStrategy>? factory))
        {
            throw new ArgumentException($"Невідома стратегія маршрутизації: '{name}'.", nameof(name));
        }

        return factory();
    }
}