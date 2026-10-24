using System.Text;

namespace MeshSim.Engine.IO;

/// <summary>
/// Збереження та завантаження сценаріїв із файлів із повною валідацією.
/// </summary>
public static class ScenarioFile
{
    /// <summary>
    /// Валідує сценарій і атомарно записує його у файл. Некоректний сценарій на диск не потрапляє.
    /// </summary>
    public static void Save(string path, ScenarioDefinition scenario)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(scenario);

        ScenarioValidator.EnsureValid(scenario);
        AtomicFileWriter.WriteAllText(path, ScenarioSerializer.Serialize(scenario));
    }

    /// <summary>
    /// Читає, розбирає та валідує сценарій. Якщо файлу немає, кидає FileNotFoundException;
    /// якщо вміст некоректний, кидає InvalidDataException.
    /// </summary>
    public static ScenarioDefinition Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string json = File.ReadAllText(path, Encoding.UTF8);
        ScenarioDefinition scenario = ScenarioSerializer.Deserialize(json);
        ScenarioValidator.EnsureValid(scenario);

        return scenario;
    }
}