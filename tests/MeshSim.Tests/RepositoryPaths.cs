namespace MeshSim.Tests;

/// <summary>
/// Шляхи до файлів у корені репозиторію (каталог із ResilientMesh.sln) для приймальних тестів.
/// </summary>
internal static class RepositoryPaths
{
    private const string SolutionFileName = "ResilientMesh.sln";

    public static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Корінь репозиторію (каталог із ResilientMesh.sln) не знайдено.");
    }

    public static string ScenariosDirectory()
    {
        return Path.Combine(Root(), "scenarios");
    }

    public static string AcceptanceDocument()
    {
        return Path.Combine(Root(), "ACCEPTANCE.md");
    }
}