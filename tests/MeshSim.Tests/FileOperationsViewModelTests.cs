using MeshSim.Desktop.Logging;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class FileOperationsViewModelTests
{
    private static void AssertFailureReported(FileOperationsFixture f, string expectedAction)
    {
        (string title, string message) = Assert.Single(f.Notifier.Messages);
        Assert.Equal(FileOperationsViewModel.ErrorTitle, title);
        Assert.Contains(expectedAction, message, StringComparison.Ordinal);
        Assert.Contains(f.Logger.Entries, entry => entry.Level == LogLevel.Failure && entry.Exception is not null);
        Assert.Contains(expectedAction, f.Statuses[^1], StringComparison.Ordinal);
    }

    private static void AssertNothingHappened(FileOperationsFixture f)
    {
        Assert.Empty(f.Statuses);
        Assert.Empty(f.Notifier.Messages);
        Assert.Empty(f.Logger.Entries);
    }

    // ---------- Конструктор і властивості ----------

    [Fact]
    public void NewViewModelExposesCommandsAndEmptyReport()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        FileOperationsViewModel files = f.Files;

        Assert.NotNull(files.OpenScenarioCommand);
        Assert.NotNull(files.SaveScenarioCommand);
        Assert.NotNull(files.ExportReportJsonCommand);
        Assert.NotNull(files.ExportReportCsvCommand);
        Assert.NotNull(files.OpenReportCommand);
        Assert.NotNull(files.CloseReportCommand);
        Assert.Null(files.ImportedReportText);
        Assert.False(files.HasImportedReport);
        Assert.False(files.CloseReportCommand.CanExecute(null));
        Assert.Equal("Помилка роботи з файлом", FileOperationsViewModel.ErrorTitle);
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionForNullArguments()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        var dialogs = new StubFileDialogService();
        var notifier = new RecordingNotifier();
        var logger = new ListLogger();
        Action<string> sink = _ => { };

        Assert.Throws<ArgumentNullException>(() => new FileOperationsViewModel(null!, dialogs, notifier, logger, sink));
        Assert.Throws<ArgumentNullException>(() => new FileOperationsViewModel(f.Simulation, null!, notifier, logger, sink));
        Assert.Throws<ArgumentNullException>(() => new FileOperationsViewModel(f.Simulation, dialogs, null!, logger, sink));
        Assert.Throws<ArgumentNullException>(() => new FileOperationsViewModel(f.Simulation, dialogs, notifier, null!, sink));
        Assert.Throws<ArgumentNullException>(() => new FileOperationsViewModel(f.Simulation, dialogs, notifier, logger, null!));
    }

    // ---------- Відкриття сценарію ----------

    [Fact]
    public async Task OpenScenarioLoadsFileIntoSimulationAndReportsSuccess()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string path = f.PathOf("opened.json");
        ScenarioFile.Save(path, JammedChain());
        f.Dialogs.OpenScenarioPath = path;

        await f.Files.OpenScenarioAsync();

        Assert.Equal("Jammed chain", f.Simulation.Scenario.Name);
        Assert.Equal(3, f.Simulation.Map.Topology.Nodes.Count);
        Assert.Equal("Сценарій «Jammed chain» відкрито з файлу opened.json.", f.Statuses[^1]);
        Assert.Empty(f.Notifier.Messages);
        Assert.Contains(f.Logger.Entries, entry => entry.Level == LogLevel.Info && entry.Message == f.Statuses[^1]);
    }

    [Fact]
    public async Task OpenScenarioDoesNothingWhenDialogIsCancelled()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        ScenarioDefinition before = f.Simulation.Scenario;
        f.Logger.Entries.Clear();

        await f.Files.OpenScenarioAsync();

        Assert.Equal(1, f.Dialogs.PickCount);
        Assert.Same(before, f.Simulation.Scenario);
        AssertNothingHappened(f);
    }

    [Fact]
    public async Task OpenScenarioIsRejectedWhileSimulationIsRunning()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string path = f.PathOf("opened.json");
        ScenarioFile.Save(path, JammedChain());
        f.Dialogs.OpenScenarioPath = path;
        ScenarioDefinition before = f.Simulation.Scenario;
        f.Simulation.PlayCommand.Execute(null);

        await f.Files.OpenScenarioAsync();

        Assert.Same(before, f.Simulation.Scenario);
        Assert.Contains("недоступне під час виконання", f.Statuses[^1], StringComparison.Ordinal);
        Assert.Empty(f.Notifier.Messages);
    }

    [Fact]
    public async Task OpenScenarioIsRejectedWhenSimulationStartsWhileDialogIsOpen()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string path = f.PathOf("opened.json");
        ScenarioFile.Save(path, JammedChain());
        f.Dialogs.OpenScenarioPath = path;
        f.Dialogs.WhileDialogIsOpen = () => f.Simulation.PlayCommand.Execute(null);
        ScenarioDefinition before = f.Simulation.Scenario;

        await f.Files.OpenScenarioAsync();

        Assert.Same(before, f.Simulation.Scenario);
        Assert.Contains("недоступне під час виконання", f.Statuses[^1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenScenarioReportsMissingFileAndKeepsCurrentScene()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Dialogs.OpenScenarioPath = f.PathOf("missing.json");
        ScenarioDefinition before = f.Simulation.Scenario;

        await f.Files.OpenScenarioAsync();

        AssertFailureReported(f, "Не вдалося відкрити сценарій");
        Assert.Contains("не знайдено", f.Notifier.Messages[0].Message, StringComparison.Ordinal);
        Assert.IsAssignableFrom<IOException>(f.Logger.Entries[^1].Exception);
        Assert.Same(before, f.Simulation.Scenario);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ broken")]
    [InlineData("{\"schemaVersion\":99,\"name\":\"x\"}")]
    [InlineData("{\"schemaVersion\":1,\"name\":\"x\",\"nodes\":[{\"id\":\"A\",\"transmissionRangeMeters\":0}]}")]
    [InlineData("{\"schemaVersion\":1,\"name\":\"x\",\"unexpected\":true}")]
    public async Task OpenScenarioReportsCorruptedOrInvalidFileAndKeepsCurrentScene(string content)
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string path = f.PathOf("bad.json");
        File.WriteAllText(path, content);
        f.Dialogs.OpenScenarioPath = path;
        ScenarioDefinition before = f.Simulation.Scenario;
        object topologyBefore = f.Simulation.Map.Topology;

        await f.Files.OpenScenarioAsync();

        AssertFailureReported(f, "Не вдалося відкрити сценарій");
        Assert.Contains("пошкоджено", f.Notifier.Messages[0].Message, StringComparison.Ordinal);
        Assert.IsType<InvalidDataException>(f.Logger.Entries[^1].Exception);
        Assert.Same(before, f.Simulation.Scenario);
        Assert.Same(topologyBefore, f.Simulation.Map.Topology);
    }

    [Fact]
    public async Task OpenScenarioReportsDirectoryPassedAsFileWithoutCrashing()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Dialogs.OpenScenarioPath = f.Temp.DirectoryPath;
        ScenarioDefinition before = f.Simulation.Scenario;

        await f.Files.OpenScenarioAsync();

        AssertFailureReported(f, "Не вдалося відкрити сценарій");
        Assert.Same(before, f.Simulation.Scenario);
    }

    [Fact]
    public async Task OpenScenarioDoesNotSwallowUnexpectedExceptions()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Dialogs.OpenScenarioPath = "   ";

        await Assert.ThrowsAnyAsync<ArgumentException>(() => f.Files.OpenScenarioAsync());

        Assert.Empty(f.Notifier.Messages);
    }

    [Fact]
    public void OpenScenarioCommandIsDisabledWhileSimulationRuns()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        bool canExecuteChanged = false;
        f.Files.OpenScenarioCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;
        Assert.True(f.Files.OpenScenarioCommand.CanExecute(null));

        f.Simulation.PlayCommand.Execute(null);

        Assert.True(canExecuteChanged);
        Assert.False(f.Files.OpenScenarioCommand.CanExecute(null));

        f.Simulation.PauseCommand.Execute(null);

        Assert.True(f.Files.OpenScenarioCommand.CanExecute(null));
    }

    [Fact]
    public void UnrelatedSimulationChangesDoNotAffectOpenScenarioCommand()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        bool canExecuteChanged = false;
        f.Files.OpenScenarioCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;

        f.Simulation.SelectedSpeed = f.Simulation.SpeedOptions[0];

        Assert.False(canExecuteChanged);
    }

    // ---------- Збереження сценарію ----------

    [Fact]
    public async Task SaveScenarioWritesFileThatLoadsBackAndSuggestsSafeName()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string path = f.PathOf("saved.json");
        f.Dialogs.SaveScenarioPath = path;

        await f.Files.SaveScenarioAsync();

        Assert.Equal("Test-scenario.json", f.Dialogs.RequestedFileNames[^1]);
        Assert.Equal("Test scenario", ScenarioFile.Load(path).Name);
        Assert.Equal("Сценарій збережено у файл saved.json.", f.Statuses[^1]);
        Assert.Empty(f.Notifier.Messages);
    }

    [Fact]
    public async Task SaveScenarioDoesNothingWhenDialogIsCancelled()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Logger.Entries.Clear();

        await f.Files.SaveScenarioAsync();

        AssertNothingHappened(f);
        Assert.Empty(Directory.GetFiles(f.Temp.DirectoryPath));
    }

    [Fact]
    public async Task SaveScenarioReportsDiskErrorWithoutCrashing()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Dialogs.SaveScenarioPath = Path.Combine(f.Temp.DirectoryPath, "missing-dir", "saved.json");

        await f.Files.SaveScenarioAsync();

        AssertFailureReported(f, "Не вдалося зберегти сценарій");
        Assert.Contains("не знайдено", f.Notifier.Messages[0].Message, StringComparison.Ordinal);
    }

    // ---------- Експорт звітів ----------

    [Fact]
    public async Task ExportReportJsonWritesVerifiableReportWithChecksum()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Simulation.StepCommand.Execute(null);
        f.Simulation.StepCommand.Execute(null);
        string path = f.PathOf("report.json");
        f.Dialogs.SaveReportJsonPath = path;

        await f.Files.ExportReportJsonAsync();

        Assert.Equal("Test-scenario-report.json", f.Dialogs.RequestedFileNames[^1]);
        Assert.True(File.Exists(ReportChecksum.GetSidecarPath(path)));
        ReportDocument document = ReportImporter.Load(path);
        Assert.Equal("Test scenario", document.ScenarioName);
        Assert.Equal(1, document.Metrics.TotalPacketsSent);
        Assert.Equal(2, document.Events.Count);
        Assert.Equal(
            "Звіт експортовано у файл report.json (контрольна сума: report.json.sha256).",
            f.Statuses[^1]);
        Assert.Empty(f.Notifier.Messages);
    }

    [Fact]
    public async Task ExportReportJsonDoesNothingWhenDialogIsCancelled()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Logger.Entries.Clear();

        await f.Files.ExportReportJsonAsync();

        AssertNothingHappened(f);
        Assert.Empty(Directory.GetFiles(f.Temp.DirectoryPath));
    }

    [Fact]
    public async Task ExportReportJsonReportsDiskErrorWithoutCrashing()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Dialogs.SaveReportJsonPath = Path.Combine(f.Temp.DirectoryPath, "missing-dir", "report.json");

        await f.Files.ExportReportJsonAsync();

        AssertFailureReported(f, "Не вдалося експортувати звіт JSON");
    }

    [Fact]
    public async Task ExportReportCsvWritesEventsAndChecksum()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Simulation.StepCommand.Execute(null);
        string path = f.PathOf("events.csv");
        f.Dialogs.SaveReportCsvPath = path;

        await f.Files.ExportReportCsvAsync();

        Assert.Equal("Test-scenario-events.csv", f.Dialogs.RequestedFileNames[^1]);
        string csv = File.ReadAllText(path);
        Assert.StartsWith("timestampMs,eventType,packetId", csv, StringComparison.Ordinal);
        Assert.Contains("0,PacketEnqueued,p1,A,C,A,None", csv, StringComparison.Ordinal);
        Assert.True(File.Exists(ReportChecksum.GetSidecarPath(path)));
        Assert.Equal(
            "Журнал подій експортовано у файл events.csv (контрольна сума: events.csv.sha256).",
            f.Statuses[^1]);
    }

    [Fact]
    public async Task ExportReportCsvDoesNothingWhenDialogIsCancelled()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Logger.Entries.Clear();

        await f.Files.ExportReportCsvAsync();

        AssertNothingHappened(f);
    }

    [Fact]
    public async Task ExportReportCsvReportsDiskErrorWithoutCrashing()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Dialogs.SaveReportCsvPath = Path.Combine(f.Temp.DirectoryPath, "missing-dir", "events.csv");

        await f.Files.ExportReportCsvAsync();

        AssertFailureReported(f, "Не вдалося експортувати журнал подій CSV");
    }

    // ---------- Імпорт звіту ----------

    private static async Task<string> ExportAndOpenReport(FileOperationsFixture f, string fileName)
    {
        string path = f.PathOf(fileName);
        f.Dialogs.SaveReportJsonPath = path;
        await f.Files.ExportReportJsonAsync();
        f.Dialogs.OpenReportJsonPath = path;
        await f.Files.OpenReportAsync();
        return path;
    }

    [Fact]
    public async Task OpenReportShowsSummaryAfterIntegrityCheck()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Simulation.StepCommand.Execute(null);
        bool hasReportRaised = false;
        f.Files.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FileOperationsViewModel.HasImportedReport))
            {
                hasReportRaised = true;
            }
        };

        await ExportAndOpenReport(f, "report.json");

        Assert.True(f.Files.HasImportedReport);
        Assert.True(hasReportRaised);
        Assert.NotNull(f.Files.ImportedReportText);
        Assert.Contains("«Test scenario»", f.Files.ImportedReportText, StringComparison.Ordinal);
        Assert.Contains("Надіслано пакетів: 1", f.Files.ImportedReportText, StringComparison.Ordinal);
        Assert.Equal("Звіт відкрито з файлу report.json: цілісність підтверджено.", f.Statuses[^1]);
        Assert.True(f.Files.CloseReportCommand.CanExecute(null));
    }

    [Fact]
    public async Task OpeningTheSameReportTwiceKeepsSummaryWithoutExtraNotification()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string path = await ExportAndOpenReport(f, "report.json");
        int raised = 0;
        f.Files.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FileOperationsViewModel.HasImportedReport))
            {
                raised++;
            }
        };
        f.Dialogs.OpenReportJsonPath = path;

        await f.Files.OpenReportAsync();

        Assert.Equal(0, raised);
        Assert.True(f.Files.HasImportedReport);
    }

    [Fact]
    public async Task OpenReportDoesNothingWhenDialogIsCancelled()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Logger.Entries.Clear();

        await f.Files.OpenReportAsync();

        AssertNothingHappened(f);
        Assert.False(f.Files.HasImportedReport);
    }

    [Fact]
    public async Task OpenReportRejectsReportModifiedAfterExportAndKeepsPreviousSummary()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string good = await ExportAndOpenReport(f, "good.json");
        string summaryBefore = f.Files.ImportedReportText!;
        f.Notifier.Messages.Clear();

        string tampered = f.PathOf("tampered.json");
        File.Copy(good, tampered);
        File.Copy(ReportChecksum.GetSidecarPath(good), ReportChecksum.GetSidecarPath(tampered));
        File.WriteAllText(tampered, File.ReadAllText(tampered).Replace("\"totalPacketsSent\": 1", "\"totalPacketsSent\": 7", StringComparison.Ordinal));
        f.Dialogs.OpenReportJsonPath = tampered;

        await f.Files.OpenReportAsync();

        AssertFailureReported(f, "Не вдалося відкрити звіт");
        Assert.Contains("Контрольна сума", f.Notifier.Messages[0].Message, StringComparison.Ordinal);
        Assert.Equal(summaryBefore, f.Files.ImportedReportText);
    }

    [Fact]
    public async Task OpenReportRejectsReportWithoutChecksumFile()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string path = f.PathOf("report.json");
        f.Dialogs.SaveReportJsonPath = path;
        await f.Files.ExportReportJsonAsync();
        File.Delete(ReportChecksum.GetSidecarPath(path));
        f.Dialogs.OpenReportJsonPath = path;

        await f.Files.OpenReportAsync();

        AssertFailureReported(f, "Не вдалося відкрити звіт");
        Assert.Contains("контрольної суми", f.Notifier.Messages[0].Message, StringComparison.Ordinal);
        Assert.False(f.Files.HasImportedReport);
    }

    [Fact]
    public async Task OpenReportRejectsUnsupportedSchemaEvenWithValidChecksum()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        string path = f.PathOf("future.json");
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("{\"schemaVersion\":7}");
        File.WriteAllBytes(path, bytes);
        File.WriteAllText(ReportChecksum.GetSidecarPath(path), ReportChecksum.Compute(bytes));
        f.Dialogs.OpenReportJsonPath = path;

        await f.Files.OpenReportAsync();

        AssertFailureReported(f, "Не вдалося відкрити звіт");
        Assert.Contains("версія схеми", f.Notifier.Messages[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenReportReportsMissingFile()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        f.Dialogs.OpenReportJsonPath = f.PathOf("missing.json");

        await f.Files.OpenReportAsync();

        AssertFailureReported(f, "Не вдалося відкрити звіт");
    }

    [Fact]
    public async Task CloseReportClearsSummaryAndDisablesCommand()
    {
        using FileOperationsFixture f = FileOperationsFixture.Create();
        await ExportAndOpenReport(f, "report.json");

        f.Files.CloseReportCommand.Execute(null);

        Assert.Null(f.Files.ImportedReportText);
        Assert.False(f.Files.HasImportedReport);
        Assert.False(f.Files.CloseReportCommand.CanExecute(null));
        Assert.Equal("Звіт закрито.", f.Statuses[^1]);
    }

    // ---------- Допоміжні статичні методи ----------

    [Theory]
    [InlineData(typeof(InvalidDataException), true)]
    [InlineData(typeof(IOException), true)]
    [InlineData(typeof(FileNotFoundException), true)]
    [InlineData(typeof(UnauthorizedAccessException), true)]
    [InlineData(typeof(ArgumentException), false)]
    [InlineData(typeof(InvalidOperationException), false)]
    public void IsExpectedFileFailureAcceptsOnlyFileAndDiskErrors(Type exceptionType, bool expected)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Equal(expected, FileOperationsViewModel.IsExpectedFileFailure(exception));
    }

    [Theory]
    [InlineData(typeof(InvalidDataException), "пошкоджено")]
    [InlineData(typeof(FileNotFoundException), "не знайдено")]
    [InlineData(typeof(DirectoryNotFoundException), "не знайдено")]
    [InlineData(typeof(UnauthorizedAccessException), "Немає доступу")]
    [InlineData(typeof(IOException), "читання або запису")]
    [InlineData(typeof(ArgumentException), "Невідома помилка")]
    public void DescribeFailureExplainsEachErrorKind(Type exceptionType, string expectedFragment)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Contains(expectedFragment, FileOperationsViewModel.DescribeFailure(exception), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUserMessageContainsActionPathExplanationAndDetails()
    {
        string message = FileOperationsViewModel.BuildUserMessage("Не вдалося відкрити сценарій", "C:/x/a.json", new InvalidDataException("деталі помилки"));

        Assert.Contains("Не вдалося відкрити сценарій.", message, StringComparison.Ordinal);
        Assert.Contains("Файл: C:/x/a.json", message, StringComparison.Ordinal);
        Assert.Contains("пошкоджено", message, StringComparison.Ordinal);
        Assert.Contains("Подробиці: деталі помилки", message, StringComparison.Ordinal);
    }

    [Fact]
    public void StaticHelpersThrowArgumentNullExceptionForNullArguments()
    {
        var exception = new IOException("x");

        Assert.Throws<ArgumentNullException>(() => FileOperationsViewModel.DescribeFailure(null!));
        Assert.Throws<ArgumentNullException>(() => FileOperationsViewModel.BuildUserMessage(null!, "p", exception));
        Assert.Throws<ArgumentNullException>(() => FileOperationsViewModel.BuildUserMessage("a", null!, exception));
        Assert.Throws<ArgumentNullException>(() => FileOperationsViewModel.BuildUserMessage("a", "p", null!));
    }
}