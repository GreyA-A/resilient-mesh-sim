using System.Globalization;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.Scenarios;
using MeshSim.Desktop.Services;
using MeshSim.Engine.IO;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Модель подання головного вікна: завантажує демонстраційний сценарій, тримає карту,
/// керування симуляцією, редактор сцени та файлові операції.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    public const string AppTitle = "ResilientMesh: симулятор стійкості mesh-мереж";
    private string _statusText;

    public MainViewModel(IAppLogger logger, ISimulationTimer timer, IFileDialogService dialogs, IUserNotifier notifier)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(notifier);

        ScenarioDefinition scenario = DemoScenario.Create();

        Title = AppTitle;
        Map = new MapViewModel();
        Simulation = new SimulationViewModel(Map, scenario, timer, logger);
        Editor = new SceneEditorViewModel(Simulation, logger);
        Files = new FileOperationsViewModel(Simulation, dialogs, notifier, logger, text => StatusText = text);
        _statusText = string.Create(
            CultureInfo.InvariantCulture,
            $"Сценарій «{scenario.Name}»: вузлів — {scenario.Nodes.Count}, зон завад — {scenario.JammingZones.Count}, пакетів — {scenario.Packets.Count}.");

        logger.LogInfo("Головне вікно ініціалізовано, демонстраційний сценарій завантажено.");
    }

    public string Title { get; }
    public MapViewModel Map { get; }
    public SimulationViewModel Simulation { get; }
    public SceneEditorViewModel Editor { get; }
    public FileOperationsViewModel Files { get; }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }
}