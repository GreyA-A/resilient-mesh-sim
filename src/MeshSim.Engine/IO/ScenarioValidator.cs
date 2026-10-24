using System.Globalization;

namespace MeshSim.Engine.IO;

/// <summary>
/// Перевіряє цілісність і межі значень сценарію: версію схеми, унікальність Id,
/// посилання пакетів на існуючі вузли та допустимі діапазони чисел.
/// </summary>
public static class ScenarioValidator
{
    /// <summary>
    /// Повертає список усіх знайдених помилок (порожній, якщо сценарій коректний).
    /// </summary>
    public static IReadOnlyList<string> Validate(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var errors = new List<string>();

        if (scenario.SchemaVersion != ScenarioDefinition.CurrentSchemaVersion)
        {
            errors.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"Непідтримувана версія схеми: {scenario.SchemaVersion} (очікується {ScenarioDefinition.CurrentSchemaVersion})."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(scenario.Name))
        {
            errors.Add("Назва сценарію не може бути порожньою.");
        }

        if (!RoutingStrategyRegistry.IsKnown(scenario.RoutingStrategy))
        {
            errors.Add($"Невідома стратегія маршрутизації: '{scenario.RoutingStrategy}'. Доступні: {string.Join(", ", RoutingStrategyRegistry.Names)}.");
        }
        ValidateEngine(scenario.Engine, errors);

        HashSet<string> nodeIds = ValidateNodes(scenario.Nodes, errors);
        ValidateZones(scenario.JammingZones, errors);
        ValidatePackets(scenario.Packets, nodeIds, errors);

        return errors;
    }

    /// <summary>
    /// Кидає InvalidDataException з усіма помилками, якщо сценарій некоректний.
    /// </summary>
    public static void EnsureValid(ScenarioDefinition scenario)
    {
        IReadOnlyList<string> errors = Validate(scenario);

        if (errors.Count > 0)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        }
    }

    private static void ValidateEngine(EngineSettingsDefinition? engine, List<string> errors)
    {
        if (engine is null)
        {
            errors.Add("Секція 'engine' не може бути null.");
            return;
        }

        if (!IsPositiveFinite(engine.HopDelayMs))
        {
            errors.Add("engine.hopDelayMs має бути додатним скінченним числом.");
        }

        if (!IsNonNegativeFinite(engine.EnergyPerTransmissionPercent))
        {
            errors.Add("engine.energyPerTransmissionPercent має бути невід'ємним скінченним числом.");
        }
    }

    private static HashSet<string> ValidateNodes(IReadOnlyList<NodeDefinition?>? nodes, List<string> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach ((int index, NodeDefinition node) in NonNullItems(nodes, "nodes", errors))
        {
            string location = Location("nodes", index);

            CheckId(node.Id, location, ids, errors);
            CheckPosition(node.X, node.Y, location, errors);

            if (!IsPositiveFinite(node.TransmissionRangeMeters))
            {
                errors.Add($"{location}: transmissionRangeMeters має бути додатним скінченним числом.");
            }

            if (!IsInRange(node.BatteryLevelPercent, 0.0, 100.0))
            {
                errors.Add($"{location}: batteryLevelPercent має бути в межах від 0 до 100.");
            }
        }

        return ids;
    }

    private static void ValidateZones(IReadOnlyList<JammingZoneDefinition?>? zones, List<string> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach ((int index, JammingZoneDefinition zone) in NonNullItems(zones, "jammingZones", errors))
        {
            string location = Location("jammingZones", index);

            CheckId(zone.Id, location, ids, errors);
            CheckPosition(zone.X, zone.Y, location, errors);

            if (!IsPositiveFinite(zone.RadiusMeters))
            {
                errors.Add($"{location}: radiusMeters має бути додатним скінченним числом.");
            }

            if (!IsInRange(zone.JammingIntensity, 0.0, 1.0))
            {
                errors.Add($"{location}: jammingIntensity має бути в межах від 0.0 до 1.0.");
            }
        }
    }

    private static void ValidatePackets(IReadOnlyList<PacketDefinition?>? packets, HashSet<string> nodeIds, List<string> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach ((int index, PacketDefinition packet) in NonNullItems(packets, "packets", errors))
        {
            string location = Location("packets", index);

            CheckId(packet.Id, location, ids, errors);
            CheckNodeReference(packet.SourceNodeId, "sourceNodeId", nodeIds, location, errors);
            CheckNodeReference(packet.DestinationNodeId, "destinationNodeId", nodeIds, location, errors);

            if (packet.Ttl <= 0)
            {
                errors.Add($"{location}: ttl має бути більшим за нуль.");
            }

            if (packet.PayloadBytes <= 0)
            {
                errors.Add($"{location}: payloadBytes має бути більшим за нуль.");
            }

            if (!IsNonNegativeFinite(packet.ScheduledTimeMs))
            {
                errors.Add($"{location}: scheduledTimeMs має бути невід'ємним скінченним числом.");
            }
        }
    }

    private static List<(int index, T item)> NonNullItems<T>(IReadOnlyList<T?>? items, string section, List<string> errors)
        where T : class
    {
        List<(int index, T item)> result = [];

        if (items is null)
        {
            errors.Add($"Список '{section}' не може бути null.");
            return result;
        }

        for (int i = 0; i < items.Count; i++)
        {
            T? item = items[i];

            if (item is null)
            {
                errors.Add($"{Location(section, i)}: елемент не може бути null.");
                continue;
            }

            result.Add((i, item));
        }

        return result;
    }

    private static void CheckId(string? id, string location, HashSet<string> seen, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add($"{location}: id не може бути порожнім.");
            return;
        }

        if (!seen.Add(id))
        {
            errors.Add($"{location}: id '{id}' дублюється.");
        }
    }

    private static void CheckPosition(double x, double y, string location, List<string> errors)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            errors.Add($"{location}: координати x та y мають бути скінченними числами.");
        }
    }

    private static void CheckNodeReference(string? nodeId, string field, HashSet<string> nodeIds, string location, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            errors.Add($"{location}: {field} не може бути порожнім.");
            return;
        }

        if (!nodeIds.Contains(nodeId))
        {
            errors.Add($"{location}: {field} '{nodeId}' не існує у списку вузлів.");
        }
    }

    private static string Location(string section, int index)
    {
        return string.Concat(section, "[", index.ToString(CultureInfo.InvariantCulture), "]");
    }

    private static bool IsPositiveFinite(double value)
    {
        return double.IsFinite(value) && value > 0.0;
    }

    private static bool IsNonNegativeFinite(double value)
    {
        return double.IsFinite(value) && value >= 0.0;
    }

    private static bool IsInRange(double value, double min, double max)
    {
        return double.IsFinite(value) && value >= min && value <= max;
    }
}