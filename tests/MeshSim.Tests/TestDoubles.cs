using MeshSim.Desktop.Logging;
using MeshSim.Desktop.Services;

namespace MeshSim.Tests;

internal sealed class ListLogger : IAppLogger
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        Entries.Add((level, message, exception));
    }
}

internal sealed class RecordingNotifier : IUserNotifier
{
    public List<(string Title, string Message)> Messages { get; } = [];

    public void ShowError(string title, string message)
    {
        Messages.Add((title, message));
    }
}

internal sealed class ThrowingNotifier : IUserNotifier
{
    public void ShowError(string title, string message)
    {
        throw new InvalidOperationException("dialog failed");
    }
}

/// <summary>
/// Таймер, яким керує тест: тік виникає лише після виклику <see cref="FireTick"/>.
/// </summary>
internal sealed class ManualSimulationTimer : ISimulationTimer
{
    public bool IsRunning { get; private set; }
    public TimeSpan Interval { get; set; }
    public int StartCount { get; private set; }
    public int PauseCount { get; private set; }

    public event EventHandler? Tick;

    public void Start()
    {
        IsRunning = true;
        StartCount++;
    }

    public void Pause()
    {
        IsRunning = false;
        PauseCount++;
    }

    public void FireTick()
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Фейкові діалоги: повертають шляхи, задані тестом (або null, якщо «користувач скасував»).
/// </summary>
internal sealed class StubFileDialogService : IFileDialogService
{
    public string? OpenScenarioPath { get; set; }
    public string? SaveScenarioPath { get; set; }
    public string? SaveReportJsonPath { get; set; }
    public string? SaveReportCsvPath { get; set; }
    public string? OpenReportJsonPath { get; set; }

    /// <summary>
    /// Виконується під час «діалогу» перед поверненням шляху (імітує дії користувача, поки діалог відкритий).
    /// </summary>
    public Action? WhileDialogIsOpen { get; set; }
    public int PickCount { get; private set; }
    public List<string> RequestedFileNames { get; } = [];

    public Task<string?> PickOpenScenarioPathAsync()
    {
        return Respond(OpenScenarioPath);
    }

    public Task<string?> PickSaveScenarioPathAsync(string suggestedFileName)
    {
        RequestedFileNames.Add(suggestedFileName);
        return Respond(SaveScenarioPath);
    }

    public Task<string?> PickSaveReportJsonPathAsync(string suggestedFileName)
    {
        RequestedFileNames.Add(suggestedFileName);
        return Respond(SaveReportJsonPath);
    }

    public Task<string?> PickSaveReportCsvPathAsync(string suggestedFileName)
    {
        RequestedFileNames.Add(suggestedFileName);
        return Respond(SaveReportCsvPath);
    }

    public Task<string?> PickOpenReportJsonPathAsync()
    {
        return Respond(OpenReportJsonPath);
    }

    private Task<string?> Respond(string? path)
    {
        PickCount++;
        WhileDialogIsOpen?.Invoke();
        return Task.FromResult(path);
    }
}