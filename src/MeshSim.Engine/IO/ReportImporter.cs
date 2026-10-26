using System.Text;
using System.Text.Json;

namespace MeshSim.Engine.IO;

/// <summary>
/// Імпорт JSON-звіту з перевіркою контрольної суми та узгодженості даних.
/// Повертає документ звіту, а не <see cref="SimulationResult"/>: пакети подій неможливо
/// відновити повністю, бо звіт не містить початкового TTL і розміру корисного навантаження.
/// </summary>
public static class ReportImporter
{
    /// <summary>
    /// Читає файл звіту, перевіряє SHA-256 за файлом «шлях.sha256», розбирає та валідує вміст.
    /// Відсутній файл дає FileNotFoundException; відсутня чи неправильна сума та некоректний вміст
    /// дають InvalidDataException.
    /// </summary>
    public static ReportDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        byte[] bytes = File.ReadAllBytes(path);
        string sidecarPath = ReportChecksum.GetSidecarPath(path);

        if (!File.Exists(sidecarPath))
        {
            throw new InvalidDataException(
                $"Не знайдено файл контрольної суми '{Path.GetFileName(sidecarPath)}': цілісність звіту не можна підтвердити.");
        }

        string expected = File.ReadAllText(sidecarPath, Encoding.UTF8);

        if (!ReportChecksum.Matches(bytes, expected))
        {
            throw new InvalidDataException("Контрольна сума SHA-256 не збігається: файл звіту змінено або пошкоджено.");
        }

        return Parse(Encoding.UTF8.GetString(bytes));
    }

    /// <summary>
    /// Розбирає та валідує JSON-звіт без перевірки контрольної суми.
    /// </summary>
    public static ReportDocument Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("Файл звіту порожній.");
        }

        ReportDocument? document;

        try
        {
            document = JsonSerializer.Deserialize<ReportDocument>(json, ReportJson.Options);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Файл звіту має некоректний формат JSON: {exception.Message}", exception);
        }

        if (document is null)
        {
            throw new InvalidDataException("Файл звіту не містить об'єкта звіту (null).");
        }

        IReadOnlyList<string> errors = ReportValidator.Validate(document);

        if (errors.Count > 0)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        }

        return document;
    }
}