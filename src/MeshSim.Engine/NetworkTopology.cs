using MeshSim.Core;

namespace MeshSim.Engine;

/// <summary>
/// Представляє граф топології бездротової mesh-мережі, що об'єднує вузли та зони радіозавад.
/// </summary>
public sealed class NetworkTopology
{
    /// <summary>
    /// Поріг інтенсивності завад, вище якого радіоканал вважається повністю заблокованим.
    /// </summary>
    public const double JammingBlockThreshold = 0.85;

    private readonly Dictionary<string, MeshNode> _nodes = new(StringComparer.Ordinal);
    private readonly List<JammingZone> _jammingZones = [];

    public IReadOnlyDictionary<string, MeshNode> Nodes => _nodes;
    public IReadOnlyList<JammingZone> JammingZones => _jammingZones;

    /// <summary>
    /// Додає або оновлює вузол у топології мережі.
    /// </summary>
    public void AddNode(MeshNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _nodes[node.Id] = node;
    }

    /// <summary>
    /// Додає зону радіоелектронних завад до середовища симуляції.
    /// </summary>
    public void AddJammingZone(JammingZone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        _jammingZones.Add(zone);
    }

    /// <summary>
    /// Повертає максимальний рівень завад (0.0..1.0) на лінії зв'язку між двома вузлами.
    /// </summary>
    public double GetLinkJammingIntensity(MeshNode sender, MeshNode receiver)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(receiver);

        double maxIntensity = 0.0;

        foreach (JammingZone zone in _jammingZones)
        {
            if (zone.IsActive && zone.IntersectsLink(sender.Position, receiver.Position))
            {
                if (zone.JammingIntensity > maxIntensity)
                {
                    maxIntensity = zone.JammingIntensity;
                }
            }
        }

        return maxIntensity;
    }

    /// <summary>
    /// Оцінює ймовірність успішної передачі пакета між двома сусідніми вузлами (від 0.0 до 1.0).
    /// </summary>
    public double CalculateLinkQuality(MeshNode sender, MeshNode receiver)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(receiver);

        if (!sender.CanReach(receiver))
        {
            return 0.0;
        }

        double jamming = GetLinkJammingIntensity(sender, receiver);
        if (jamming >= JammingBlockThreshold)
        {
            return 0.0;
        }

        return Math.Clamp(1.0 - jamming, 0.0, 1.0);
    }

    /// <summary>
    /// Перевіряє, чи доступний прямий канал передачі між двома вузлами з урахуванням радіуса дії, заряду та завад.
    /// </summary>
    public bool IsLinkAvailable(MeshNode sender, MeshNode receiver)
    {
        return CalculateLinkQuality(sender, receiver) > 0.0;
    }
}