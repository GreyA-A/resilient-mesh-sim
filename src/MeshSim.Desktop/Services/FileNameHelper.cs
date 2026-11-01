using System.Text;

namespace MeshSim.Desktop.Services;

/// <summary>
/// Формує безпечну для файлової системи назву файлу за замовчуванням із назви сценарію.
/// </summary>
public static class FileNameHelper
{
    public const string FallbackStem = "scenario";

    public static string Suggest(string name, string extension)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(extension);

        char[] invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();

        foreach (char symbol in name.Trim())
        {
            builder.Append(Array.IndexOf(invalid, symbol) >= 0 || char.IsWhiteSpace(symbol) ? '-' : symbol);
        }

        string stem = builder.ToString().Trim('-');

        return string.Concat(stem.Length == 0 ? FallbackStem : stem, extension);
    }
}