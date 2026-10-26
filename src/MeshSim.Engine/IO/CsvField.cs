namespace MeshSim.Engine.IO;

/// <summary>
/// Безпечне формування текстового поля CSV: нейтралізація формул (CSV Formula Injection)
/// та екранування ком, лапок і переносів рядків за RFC 4180.
/// </summary>
public static class CsvField
{
    private static readonly char[] SpecialCharacters = [',', '"', '\r', '\n'];

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string safe = IsFormulaTrigger(value[0]) ? "'" + value : value;

        if (safe.IndexOfAny(SpecialCharacters) < 0)
        {
            return safe;
        }

        return "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static bool IsFormulaTrigger(char first)
    {
        return first is '=' or '+' or '-' or '@' or '\t' or '\r';
    }
}