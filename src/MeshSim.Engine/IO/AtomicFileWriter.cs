using System.Globalization;
using System.Text;

namespace MeshSim.Engine.IO;

/// <summary>
/// Транзакційний запис файлу: дані спершу потрапляють у тимчасовий файл поруч із цільовим,
/// після чого він підміняє цільовий одним переміщенням. Тимчасовий файл прибирається завжди.
/// </summary>
public static class AtomicFileWriter
{
    public const string TemporaryExtension = ".tmp";

    public static void WriteAllText(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Encoding.UTF8.GetBytes не додає BOM.
        WriteAllBytes(path, Encoding.UTF8.GetBytes(content));
    }

    public static void WriteAllBytes(string path, byte[] content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        string fullPath = Path.GetFullPath(path);
        string temporaryPath = string.Concat(
            fullPath,
            ".",
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            TemporaryExtension);

        try
        {
            File.WriteAllBytes(temporaryPath, content);
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}