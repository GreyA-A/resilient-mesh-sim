using System.Globalization;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.Scenarios;
using MeshSim.Engine.IO;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Модель подання головного вікна: завантажує демонстраційний сценарій і тримає карту.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    public const string AppTitle = "ResilientMesh: симулятор стійкості mesh-мереж";

    private string _statusText;

    public MainViewModel(IAppLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        ScenarioDefinition scenario = DemoScenario.Create();
        ScenarioRuntime runtime = ScenarioFactory.Build(scenario);

        Title = AppTitle;
        Map = new MapViewModel();
        Map.SetTopology(runtime.Topology);
        _statusText = string.Create(
            CultureInfo.InvariantCulture,
            $"Сценарій «{scenario.Name}»: вузлів — {scenario.Nodes.Count}, зон завад — {scenario.JammingZones.Count}, пакетів — {scenario.Packets.Count}.");

        logger.LogInfo("Головне вікно ініціалізовано, демонстраційний сценарій завантажено.");
    }

    public string Title { get; }

    public MapViewModel Map { get; }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }
}