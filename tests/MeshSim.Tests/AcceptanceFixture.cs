using MeshSim.Desktop.ViewModels;

namespace MeshSim.Tests;

/// <summary>
/// Складений застосунок для наскрізних тестів: MainViewModel із ручним таймером, фейковими діалогами,
/// тимчасовим каталогом і вікном 900×600. Поводиться як застосунок після запуску.
/// </summary>
internal sealed class AcceptanceFixture : IDisposable
{
    private const int MaxTicks = 1000;

    private AcceptanceFixture(
        MainViewModel main,
        ManualSimulationTimer timer,
        StubFileDialogService dialogs,
        RecordingNotifier notifier,
        ListLogger logger,
        TempDirectory temp)
    {
        Main = main;
        Timer = timer;
        Dialogs = dialogs;
        Notifier = notifier;
        Logger = logger;
        Temp = temp;
    }

    public MainViewModel Main { get; }
    public ManualSimulationTimer Timer { get; }
    public StubFileDialogService Dialogs { get; }
    public RecordingNotifier Notifier { get; }
    public ListLogger Logger { get; }
    public TempDirectory Temp { get; }
    public SimulationViewModel Simulation => Main.Simulation;
    public SceneEditorViewModel Editor => Main.Editor;
    public FileOperationsViewModel Files => Main.Files;
    public MapViewModel Map => Main.Map;
    public double Scale => Map.Transform?.Scale ?? throw new InvalidOperationException("Вікно має нульовий розмір.");

    public static AcceptanceFixture Create()
    {
        var temp = new TempDirectory();
        var timer = new ManualSimulationTimer();
        var dialogs = new StubFileDialogService();
        var notifier = new RecordingNotifier();
        var logger = new ListLogger();

        var main = new MainViewModel(logger, timer, dialogs, notifier);
        main.Map.Resize(900.0, 600.0);

        return new AcceptanceFixture(main, timer, dialogs, notifier, logger, temp);
    }

    /// <summary>
    /// Відкриває приклад із каталогу scenarios/ так, як це робить користувач: через діалог і команду.
    /// Успішне відкриття не має показувати діалогів із помилками.
    /// </summary>
    public async Task OpenCatalogScenarioAsync(string fileName)
    {
        Dialogs.OpenScenarioPath = Path.Combine(RepositoryPaths.ScenariosDirectory(), fileName);

        await Files.OpenScenarioAsync();

        Assert.Empty(Notifier.Messages);
    }

    /// <summary>
    /// Натискає «Старт» і подає такти таймера, доки симуляція не завершиться сама.
    /// </summary>
    public void RunToEnd()
    {
        Simulation.PlayCommand.Execute(null);

        for (int tick = 0; tick < MaxTicks && Timer.IsRunning; tick++)
        {
            Timer.FireTick();
        }

        Assert.False(Timer.IsRunning, "Таймер має зупинитись сам, коли події вичерпано.");
        Assert.Equal(SimulationState.Finished, Simulation.State);
    }

    public void Dispose()
    {
        Temp.Dispose();
    }
}