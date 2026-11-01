using MeshSim.Desktop.ViewModels;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

/// <summary>
/// Набір для тестів файлових операцій: сцена A–B–C із пакетом p1, тимчасовий каталог і фейкові залежності.
/// </summary>
internal sealed class FileOperationsFixture : IDisposable
{
    private FileOperationsFixture(
        TempDirectory temp,
        SimulationViewModel simulation,
        ManualSimulationTimer timer,
        StubFileDialogService dialogs,
        RecordingNotifier notifier,
        ListLogger logger,
        List<string> statuses,
        FileOperationsViewModel files)
    {
        Temp = temp;
        Simulation = simulation;
        Timer = timer;
        Dialogs = dialogs;
        Notifier = notifier;
        Logger = logger;
        Statuses = statuses;
        Files = files;
    }

    public TempDirectory Temp { get; }
    public SimulationViewModel Simulation { get; }
    public ManualSimulationTimer Timer { get; }
    public StubFileDialogService Dialogs { get; }
    public RecordingNotifier Notifier { get; }
    public ListLogger Logger { get; }
    public List<string> Statuses { get; }
    public FileOperationsViewModel Files { get; }

    public static FileOperationsFixture Create()
    {
        var temp = new TempDirectory();
        var timer = new ManualSimulationTimer();
        var dialogs = new StubFileDialogService();
        var notifier = new RecordingNotifier();
        var logger = new ListLogger();
        var statuses = new List<string>();

        ScenarioDefinition scenario = Scenario(
            nodes: new[] { Node("A"), Node("B", x: 80.0), Node("C", x: 160.0) },
            packets: new[] { Packet("p1", "A", "C") });

        var simulation = new SimulationViewModel(new MapViewModel(), scenario, timer, logger);
        var files = new FileOperationsViewModel(simulation, dialogs, notifier, logger, statuses.Add);

        return new FileOperationsFixture(temp, simulation, timer, dialogs, notifier, logger, statuses, files);
    }

    public string PathOf(string fileName) => Temp.FilePath(fileName);

    public void Dispose() => Temp.Dispose();
}