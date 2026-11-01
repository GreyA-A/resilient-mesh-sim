using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Input;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.Services;
using MeshSim.Engine.IO;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Файлові операції інтерфейсу: відкриття й збереження сценарію, експорт звіту (JSON/CSV з контрольними сумами)
/// та імпорт JSON-звіту з перевіркою цілісності. Помилки файлів не завалюють застосунок і не псують поточну сцену:
/// вони логуються, показуються користувачу й відображаються в статусі.
/// </summary>
public sealed class FileOperationsViewModel : ViewModelBase
{
    public const string ErrorTitle = "Помилка роботи з файлом";

    private readonly SimulationViewModel _simulation;
    private readonly IFileDialogService _dialogs;
    private readonly IUserNotifier _notifier;
    private readonly IAppLogger _logger;
    private readonly Action<string> _reportStatus;

    private string? _importedReportText;

    public FileOperationsViewModel(
        SimulationViewModel simulation,
        IFileDialogService dialogs,
        IUserNotifier notifier,
        IAppLogger logger,
        Action<string> reportStatus)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(reportStatus);

        _simulation = simulation;
        _dialogs = dialogs;
        _notifier = notifier;
        _logger = logger;
        _reportStatus = reportStatus;

        OpenScenarioCommand = new AsyncRelayCommand(OpenScenarioAsync, CanOpenScenario);
        SaveScenarioCommand = new AsyncRelayCommand(SaveScenarioAsync);
        ExportReportJsonCommand = new AsyncRelayCommand(ExportReportJsonAsync);
        ExportReportCsvCommand = new AsyncRelayCommand(ExportReportCsvAsync);
        OpenReportCommand = new AsyncRelayCommand(OpenReportAsync);
        CloseReportCommand = new RelayCommand(CloseReport, () => HasImportedReport);

        simulation.PropertyChanged += OnSimulationPropertyChanged;
    }

    public AsyncRelayCommand OpenScenarioCommand { get; }

    public AsyncRelayCommand SaveScenarioCommand { get; }

    public AsyncRelayCommand ExportReportJsonCommand { get; }

    public AsyncRelayCommand ExportReportCsvCommand { get; }

    public AsyncRelayCommand OpenReportCommand { get; }

    public RelayCommand CloseReportCommand { get; }

    /// <summary>
    /// Текстове резюме останнього успішно імпортованого звіту (null, якщо звіт не відкрито).
    /// </summary>
    public string? ImportedReportText
    {
        get => _importedReportText;
        private set
        {
            if (SetProperty(ref _importedReportText, value))
            {
                OnPropertyChanged(nameof(HasImportedReport));
                CloseReportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasImportedReport => _importedReportText is not null;

    /// <summary>
    /// Помилки, які файлові операції перехоплюють: пошкоджений або непідтримуваний вміст, помилки диска та доступу.
    /// </summary>
    public static bool IsExpectedFileFailure(Exception exception)
    {
        return exception is InvalidDataException or IOException or UnauthorizedAccessException;
    }

    /// <summary>
    /// Зрозуміле користувачу пояснення типу помилки.
    /// </summary>
    public static string DescribeFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            InvalidDataException => "Файл пошкоджено, змінено після створення або він має непідтримуваний формат.",
            FileNotFoundException or DirectoryNotFoundException => "Файл або каталог не знайдено.",
            UnauthorizedAccessException => "Немає доступу до файлу або каталогу.",
            IOException => "Помилка читання або запису файлу.",
            _ => "Невідома помилка роботи з файлом."
        };
    }

    public static string BuildUserMessage(string action, string path, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Concat(
            action,
            ".",
            Environment.NewLine,
            "Файл: ",
            path,
            Environment.NewLine,
            Environment.NewLine,
            DescribeFailure(exception),
            Environment.NewLine,
            Environment.NewLine,
            "Подробиці: ",
            exception.Message);
    }

    // ---------- Сценарій ----------

    /// <summary>
    /// Відкриває сценарій із файлу. Під час виконання симуляції відхиляється (перевірка після діалогу,
    /// бо користувач міг натиснути «Старт», поки діалог був відкритий).
    /// </summary>
    public async Task OpenScenarioAsync()
    {
        string? picked = await _dialogs.PickOpenScenarioPathAsync();
        if (picked is null)
        {
            return;
        }

        if (_simulation.State == SimulationState.Running)
        {
            _reportStatus("Відкриття сценарію недоступне під час виконання симуляції. Призупиніть її.");
            return;
        }

        string path = picked;

        RunFileOperation("Не вдалося відкрити сценарій", path, () =>
        {
            ScenarioDefinition scenario = ScenarioFile.Load(path);
            _simulation.LoadScenario(scenario);
            ReportSuccess(string.Concat("Сценарій «", scenario.Name, "» відкрито з файлу ", Path.GetFileName(path), "."));
        });
    }

    public async Task SaveScenarioAsync()
    {
        ScenarioDefinition scenario = _simulation.Scenario;

        string? picked = await _dialogs.PickSaveScenarioPathAsync(FileNameHelper.Suggest(scenario.Name, ".json"));
        if (picked is null)
        {
            return;
        }

        string path = picked;

        RunFileOperation("Не вдалося зберегти сценарій", path, () =>
        {
            ScenarioFile.Save(path, scenario);
            ReportSuccess(string.Concat("Сценарій збережено у файл ", Path.GetFileName(path), "."));
        });
    }

    // ---------- Звіти ----------

    public async Task ExportReportJsonAsync()
    {
        string scenarioName = _simulation.Scenario.Name;

        string? picked = await _dialogs.PickSaveReportJsonPathAsync(FileNameHelper.Suggest(scenarioName + " report", ".json"));
        if (picked is null)
        {
            return;
        }

        string path = picked;

        RunFileOperation("Не вдалося експортувати звіт JSON", path, () =>
        {
            ReportExporter.SaveJson(path, _simulation.GetCurrentSnapshot(), scenarioName);
            ReportSuccess(string.Concat(
                "Звіт експортовано у файл ",
                Path.GetFileName(path),
                " (контрольна сума: ",
                Path.GetFileName(ReportChecksum.GetSidecarPath(path)),
                ")."));
        });
    }

    public async Task ExportReportCsvAsync()
    {
        string scenarioName = _simulation.Scenario.Name;

        string? picked = await _dialogs.PickSaveReportCsvPathAsync(FileNameHelper.Suggest(scenarioName + " events", ".csv"));
        if (picked is null)
        {
            return;
        }

        string path = picked;

        RunFileOperation("Не вдалося експортувати журнал подій CSV", path, () =>
        {
            CsvReportExporter.SaveCsv(path, _simulation.GetCurrentSnapshot());
            ReportSuccess(string.Concat(
                "Журнал подій експортовано у файл ",
                Path.GetFileName(path),
                " (контрольна сума: ",
                Path.GetFileName(ReportChecksum.GetSidecarPath(path)),
                ")."));
        });
    }

    /// <summary>
    /// Імпортує JSON-звіт із перевіркою SHA-256 і показує його резюме. Невдалий імпорт не змінює показаний звіт.
    /// </summary>
    public async Task OpenReportAsync()
    {
        string? picked = await _dialogs.PickOpenReportJsonPathAsync();
        if (picked is null)
        {
            return;
        }

        string path = picked;

        RunFileOperation("Не вдалося відкрити звіт", path, () =>
        {
            ReportDocument document = ReportImporter.Load(path);
            ImportedReportText = ReportSummaryFormatter.Describe(document);
            ReportSuccess(string.Concat("Звіт відкрито з файлу ", Path.GetFileName(path), ": цілісність підтверджено."));
        });
    }

    // ---------- Допоміжні методи ----------

    private bool CanOpenScenario()
    {
        return _simulation.State != SimulationState.Running;
    }

    private void CloseReport()
    {
        ImportedReportText = null;
        _reportStatus("Звіт закрито.");
    }

    private void OnSimulationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SimulationViewModel.State))
        {
            OpenScenarioCommand.NotifyCanExecuteChanged();
        }
    }

    private void ReportSuccess(string message)
    {
        _logger.LogInfo(message);
        _reportStatus(message);
    }

    /// <summary>
    /// Єдине місце перехоплення файлових помилок: перехоплюються лише очікувані (див. IsExpectedFileFailure),
    /// решта винятків (помилки програмування) не ховаються й доходять до глобального обробника.
    /// </summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Фільтр IsExpectedFileFailure пропускає лише помилки файлів і диска; інші винятки не перехоплюються.")]
    private void RunFileOperation(string action, string path, Action operation)
    {
        try
        {
            operation();
        }
        catch (Exception exception) when (IsExpectedFileFailure(exception))
        {
            _logger.LogFailure(string.Concat(action, ": ", path), exception);
            _notifier.ShowError(ErrorTitle, BuildUserMessage(action, path, exception));
            _reportStatus(string.Concat(action, ": ", DescribeFailure(exception)));
        }
    }
}