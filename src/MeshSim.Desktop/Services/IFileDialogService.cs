namespace MeshSim.Desktop.Services;

/// <summary>
/// Вибір шляхів до файлів. Усі методи повертають null, якщо користувач скасував діалог.
/// </summary>
public interface IFileDialogService
{
    Task<string?> PickOpenScenarioPathAsync();
    Task<string?> PickSaveScenarioPathAsync(string suggestedFileName);
    Task<string?> PickSaveReportJsonPathAsync(string suggestedFileName);
    Task<string?> PickSaveReportCsvPathAsync(string suggestedFileName);
    Task<string?> PickOpenReportJsonPathAsync();
}