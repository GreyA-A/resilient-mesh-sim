using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshSim.Engine.IO;

/// <summary>
/// Перетворює сценарій у JSON і назад (camelCase, суворий розбір без невідомих полів).
/// Семантичну перевірку виконує <see cref="ScenarioValidator"/>.
/// </summary>
public static class ScenarioSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static string Serialize(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        return JsonSerializer.Serialize(scenario, Options);
    }

    /// <summary>
    /// Читає сценарій із JSON. Кидає InvalidDataException для порожнього, пошкодженого
    /// або структурно неправильного вмісту.
    /// </summary>
    public static ScenarioDefinition Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("Файл сценарію порожній.");
        }

        ScenarioDefinition? scenario;

        try
        {
            scenario = JsonSerializer.Deserialize<ScenarioDefinition>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Файл сценарію має некоректний формат JSON: {exception.Message}", exception);
        }

        return scenario ?? throw new InvalidDataException("Файл сценарію не містить об'єкта сценарію (null).");
    }
}