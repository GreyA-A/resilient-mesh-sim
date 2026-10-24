using System.Globalization;

namespace MeshSim.Tests;

/// <summary>
/// Унікальний тимчасовий каталог, який видаляється разом із вмістом після тесту.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "meshsim-tests-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath { get; }

    public string FilePath(string fileName)
    {
        return Path.Combine(DirectoryPath, fileName);
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}