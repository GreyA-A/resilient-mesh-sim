using System.Security.Cryptography;

namespace MeshSim.Engine.IO;

/// <summary>
/// Контрольна сума SHA-256 для файлів звітів. Сума зберігається у супутньому файлі «звіт.sha256».
/// Захищає від випадкового пошкодження, але не від навмисної підміни: зловмисник може
/// перерахувати суму (для автентичності потрібен підпис або HMAC).
/// </summary>
public static class ReportChecksum
{
    public const string SidecarExtension = ".sha256";

    public static string GetSidecarPath(string reportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        return reportPath + SidecarExtension;
    }

    /// <summary>
    /// Обчислює SHA-256 у вигляді шістнадцяткового рядка у верхньому регістрі.
    /// </summary>
    public static string Compute(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return Convert.ToHexString(SHA256.HashData(content));
    }

    /// <summary>
    /// Порівнює суму вмісту з очікуваною (без урахування регістру та крайніх пробілів).
    /// </summary>
    public static bool Matches(byte[] content, string expectedHex)
    {
        ArgumentNullException.ThrowIfNull(expectedHex);

        return string.Equals(Compute(content), expectedHex.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}