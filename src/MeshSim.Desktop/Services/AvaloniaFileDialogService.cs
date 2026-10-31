using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MeshSim.Desktop.Services;

[ExcludeFromCodeCoverage(Justification = "Тонка обгортка над StorageProvider Avalonia: потребує вікна й системних діалогів, без власної логіки.")]
public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private static readonly FilePickerFileType ScenarioFiles = new("Сценарії симуляції (*.json)") { Patterns = ["*.json"] };
    private static readonly FilePickerFileType JsonFiles = new("Звіти JSON (*.json)") { Patterns = ["*.json"] };
    private static readonly FilePickerFileType CsvFiles = new("Таблиці CSV (*.csv)") { Patterns = ["*.csv"] };

    private readonly TopLevel _owner;

    public AvaloniaFileDialogService(TopLevel owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _owner = owner;
    }

    public Task<string?> PickOpenScenarioPathAsync()
    {
        return PickOpenAsync("Відкрити сценарій", ScenarioFiles);
    }

    public Task<string?> PickSaveScenarioPathAsync(string suggestedFileName)
    {
        return PickSaveAsync("Зберегти сценарій", suggestedFileName, "json", ScenarioFiles);
    }

    public Task<string?> PickSaveReportJsonPathAsync(string suggestedFileName)
    {
        return PickSaveAsync("Експорт звіту JSON", suggestedFileName, "json", JsonFiles);
    }

    public Task<string?> PickSaveReportCsvPathAsync(string suggestedFileName)
    {
        return PickSaveAsync("Експорт журналу подій CSV", suggestedFileName, "csv", CsvFiles);
    }

    public Task<string?> PickOpenReportJsonPathAsync()
    {
        return PickOpenAsync("Відкрити звіт JSON", JsonFiles);
    }

    private async Task<string?> PickOpenAsync(string title, FilePickerFileType fileType)
    {
        IReadOnlyList<IStorageFile> files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [fileType]
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    private async Task<string?> PickSaveAsync(string title, string suggestedFileName, string extension, FilePickerFileType fileType)
    {
        IStorageFile? file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            FileTypeChoices = [fileType]
        });

        return file?.TryGetLocalPath();
    }
}