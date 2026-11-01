using System.Text;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public partial class AcceptanceTests
{
    // ================= AC-07: експорт звітів та імпорт JSON-звіту з перевіркою цілісності =================

    [Fact]
    public async Task Ac07ReportsAreExportedWithChecksumsAndJsonIsImportedAfterIntegrityCheck()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        await f.OpenCatalogScenarioAsync("network-partition.json");
        f.RunToEnd();
        string jsonPath = f.Temp.FilePath("report.json");
        string csvPath = f.Temp.FilePath("events.csv");

        f.Dialogs.SaveReportJsonPath = jsonPath;
        await f.Files.ExportReportJsonAsync();
        f.Dialogs.SaveReportCsvPath = csvPath;
        await f.Files.ExportReportCsvAsync();

        Assert.Empty(f.Notifier.Messages);
        Assert.Contains("Розділення-мережі-завадою-report.json", f.Dialogs.RequestedFileNames);
        Assert.Contains("Розділення-мережі-завадою-events.csv", f.Dialogs.RequestedFileNames);

        // Для обох файлів створено файл контрольної суми, що збігається з SHA-256 вмісту.
        foreach (string path in new[] { jsonPath, csvPath })
        {
            string sidecar = File.ReadAllText(ReportChecksum.GetSidecarPath(path));
            Assert.Equal(ReportChecksum.Compute(File.ReadAllBytes(path)), sidecar.Trim());
        }

        // CSV: заголовок і по рядку на подію.
        SimulationResult snapshot = f.Simulation.GetCurrentSnapshot();
        string[] lines = File.ReadAllText(csvPath).Split("\r\n");
        Assert.StartsWith("timestampMs,eventType,packetId", lines[0], StringComparison.Ordinal);
        Assert.Equal(snapshot.EventLog.Count + 2, lines.Length);
        Assert.Contains("50,PacketDropped,P2,A,D,A,NoRouteFound", lines);

        // JSON: метрики відповідають прогону.
        ReportDocument document = ReportImporter.Load(jsonPath);
        Assert.Equal("Розділення мережі завадою", document.ScenarioName);
        Assert.Equal(3, document.Metrics.TotalPacketsSent);
        Assert.Equal(2, document.Metrics.TotalPacketsDelivered);
        Assert.Equal(1, document.Metrics.TotalPacketsDropped);
        Assert.Equal(1, document.Metrics.DropsByReason["NoRouteFound"]);
        Assert.Equal(snapshot.EventLog.Count, document.Events.Count);

        // Імпорт через інтерфейс: цілісність підтверджено, резюме показано.
        f.Dialogs.OpenReportJsonPath = jsonPath;
        await f.Files.OpenReportAsync();

        Assert.True(f.Files.HasImportedReport);
        Assert.Equal("Звіт відкрито з файлу report.json: цілісність підтверджено.", f.Main.StatusText);
        string summary = f.Files.ImportedReportText!;
        Assert.Contains("«Розділення мережі завадою»", summary, StringComparison.Ordinal);
        Assert.Contains("Надіслано пакетів: 3", summary, StringComparison.Ordinal);
        Assert.Contains("Доставлено: 2", summary, StringComparison.Ordinal);
        Assert.Contains("Втрачено: 1", summary, StringComparison.Ordinal);
        Assert.Contains("PDR: 66.7 %", summary, StringComparison.Ordinal);
        Assert.Contains("  • немає маршруту: 1", summary, StringComparison.Ordinal);
        Assert.Empty(f.Notifier.Messages);
    }

    [Fact]
    public async Task Ac07CsvExportNeutralizesFormulaInjection()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        f.Editor.PacketId = "=SUM(1+1)";
        f.Editor.PacketSource = "N1";
        f.Editor.PacketDestination = "N2";
        f.Editor.AddPacketCommand.Execute(null);
        Assert.False(f.Editor.HasError);
        f.RunToEnd();
        string csvPath = f.Temp.FilePath("events.csv");
        string jsonPath = f.Temp.FilePath("report.json");

        f.Dialogs.SaveReportCsvPath = csvPath;
        await f.Files.ExportReportCsvAsync();
        f.Dialogs.SaveReportJsonPath = jsonPath;
        await f.Files.ExportReportJsonAsync();

        // CSV: небезпечне поле має префікс «'», тож таблиця не виконає його як формулу.
        string csv = File.ReadAllText(csvPath);
        Assert.Contains(",'=SUM(1+1),", csv, StringComparison.Ordinal);
        Assert.DoesNotContain(",=SUM(1+1),", csv, StringComparison.Ordinal);

        // JSON зберігає вихідне значення без змін.
        ReportDocument document = ReportImporter.Load(jsonPath);
        Assert.Contains(document.Events, e => e.PacketId == "=SUM(1+1)");
    }

    // ================= AC-08: стійкість до пошкоджених і підроблених файлів =================

    [Theory]
    [InlineData("", "порожній")]
    [InlineData("{ broken", "формат JSON")]
    [InlineData("{\"schemaVersion\":99,\"name\":\"x\"}", "версія схеми")]
    [InlineData("{\"schemaVersion\":1,\"name\":\"x\",\"nodes\":[{\"id\":\"A\",\"transmissionRangeMeters\":0}]}", "transmissionRangeMeters")]
    [InlineData("{\"schemaVersion\":1,\"name\":\"x\",\"unexpected\":true}", "unexpected")]
    public async Task Ac08CorruptedScenarioFilesAreRejectedAndCurrentSceneIsKept(string content, string expectedFragment)
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        string path = f.Temp.FilePath("bad.json");
        File.WriteAllText(path, content);
        f.Dialogs.OpenScenarioPath = path;
        ScenarioDefinition before = f.Simulation.Scenario;
        NetworkTopology topologyBefore = f.Map.Topology;

        await f.Files.OpenScenarioAsync();

        (string title, string message) = Assert.Single(f.Notifier.Messages);
        Assert.Equal(FileOperationsViewModel.ErrorTitle, title);
        Assert.Contains("Не вдалося відкрити сценарій", message, StringComparison.Ordinal);
        Assert.Contains(expectedFragment, message, StringComparison.Ordinal);
        Assert.Contains("Не вдалося відкрити сценарій", f.Main.StatusText, StringComparison.Ordinal);
        Assert.Contains(f.Logger.Entries, entry => entry.Level == LogLevel.Failure && entry.Exception is InvalidDataException);
        Assert.Same(before, f.Simulation.Scenario);
        Assert.Same(topologyBefore, f.Map.Topology);

        // Застосунок працездатний: поточна сцена далі виконується.
        f.Simulation.StepCommand.Execute(null);
        Assert.Single(f.Simulation.EventLog);
    }

    [Fact]
    public async Task Ac08TamperedOrIncompleteReportsAreRejectedAndApplicationKeepsWorking()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        await f.OpenCatalogScenarioAsync("network-partition.json");
        f.RunToEnd();
        string good = f.Temp.FilePath("good.json");
        f.Dialogs.SaveReportJsonPath = good;
        await f.Files.ExportReportJsonAsync();
        f.Simulation.ResetCommand.Execute(null);
        ScenarioDefinition sceneBefore = f.Simulation.Scenario;
        f.Notifier.Messages.Clear();

        async Task<string> OpenReportExpectingFailureAsync(string path)
        {
            int before = f.Notifier.Messages.Count;
            f.Dialogs.OpenReportJsonPath = path;
            await f.Files.OpenReportAsync();
            Assert.Equal(before + 1, f.Notifier.Messages.Count);
            return f.Notifier.Messages[^1].Message;
        }

        // 1. Звіт змінено вручну після експорту: контрольна сума не збігається.
        string tampered = f.Temp.FilePath("tampered.json");
        File.Copy(good, tampered);
        File.Copy(ReportChecksum.GetSidecarPath(good), ReportChecksum.GetSidecarPath(tampered));
        File.WriteAllText(
            tampered,
            File.ReadAllText(tampered).Replace("\"totalPacketsSent\": 3", "\"totalPacketsSent\": 7", StringComparison.Ordinal));
        Assert.Contains("Контрольна сума", await OpenReportExpectingFailureAsync(tampered), StringComparison.Ordinal);

        // 2. Файл контрольної суми відсутній.
        string withoutSum = f.Temp.FilePath("nosum.json");
        File.Copy(good, withoutSum);
        Assert.Contains("контрольної суми", await OpenReportExpectingFailureAsync(withoutSum), StringComparison.Ordinal);

        // 3. Сума збігається, але схема звіту непідтримувана.
        string future = f.Temp.FilePath("future.json");
        byte[] bytes = Encoding.UTF8.GetBytes("{\"schemaVersion\":7}");
        File.WriteAllBytes(future, bytes);
        File.WriteAllText(ReportChecksum.GetSidecarPath(future), ReportChecksum.Compute(bytes));
        Assert.Contains("версія схеми", await OpenReportExpectingFailureAsync(future), StringComparison.Ordinal);

        // 4. Файл звіту не існує.
        await OpenReportExpectingFailureAsync(f.Temp.FilePath("missing.json"));

        // Жоден невдалий імпорт не змінив сцену чи не показав звіт.
        Assert.False(f.Files.HasImportedReport);
        Assert.Same(sceneBefore, f.Simulation.Scenario);
        Assert.Equal(SimulationState.Ready, f.Simulation.State);
        Assert.Equal(4, f.Logger.Entries.Count(entry => entry.Level == LogLevel.Failure));

        // Застосунок працездатний, а справжній звіт відкривається.
        f.Simulation.StepCommand.Execute(null);
        Assert.Single(f.Simulation.EventLog);
        f.Dialogs.OpenReportJsonPath = good;
        await f.Files.OpenReportAsync();
        Assert.True(f.Files.HasImportedReport);
    }

    [Fact]
    public async Task Ac08CancelledDialogsAreSilentAndHarmless()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        ScenarioDefinition sceneBefore = f.Simulation.Scenario;
        string statusBefore = f.Main.StatusText;
        int loggedBefore = f.Logger.Entries.Count;

        await f.Files.OpenScenarioAsync();
        await f.Files.SaveScenarioAsync();
        await f.Files.ExportReportJsonAsync();
        await f.Files.ExportReportCsvAsync();
        await f.Files.OpenReportAsync();

        Assert.Equal(5, f.Dialogs.PickCount);
        Assert.Empty(f.Notifier.Messages);
        Assert.Equal(loggedBefore, f.Logger.Entries.Count);
        Assert.Equal(statusBefore, f.Main.StatusText);
        Assert.Same(sceneBefore, f.Simulation.Scenario);
        Assert.False(f.Files.HasImportedReport);
        Assert.Empty(Directory.GetFiles(f.Temp.DirectoryPath));
    }
}