using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

/// <summary>
/// Складений набір для тестів редактора: сцена A–B–C, зона z1 осторонь вузлів, пакет p1, вікно 800×600.
/// </summary>
internal sealed class EditorFixture
{
    private EditorFixture(SimulationViewModel simulation, SceneEditorViewModel editor, ManualSimulationTimer timer, ListLogger logger)
    {
        Simulation = simulation;
        Editor = editor;
        Timer = timer;
        Logger = logger;
    }

    public SimulationViewModel Simulation { get; }
    public SceneEditorViewModel Editor { get; }
    public ManualSimulationTimer Timer { get; }
    public ListLogger Logger { get; }
    public MapViewModel Map => Simulation.Map;

    public static EditorFixture Create()
    {
        ScenarioDefinition scenario = Scenario(
            nodes: new[] { Node("A"), Node("B", x: 80.0), Node("C", x: 160.0) },
            zones: new[] { Zone("z1", x: 80.0, y: 100.0, radius: 30.0, intensity: 0.5) },
            packets: new[] { Packet("p1", "A", "C") });

        var map = new MapViewModel();
        var timer = new ManualSimulationTimer();
        var logger = new ListLogger();
        var simulation = new SimulationViewModel(map, scenario, timer, logger);
        var editor = new SceneEditorViewModel(simulation, logger);
        map.Resize(800.0, 600.0);
        logger.Entries.Clear();

        return new EditorFixture(simulation, editor, timer, logger);
    }

    public ScreenPoint NodeCenter(string id) => Map.Scene.Nodes.First(node => node.Id == id).Center;

    public ScreenPoint ZoneCenter(string id) => Map.Scene.Zones.First(zone => zone.Id == id).Center;

    public NodeDefinition NodeDefinitionOf(string id) => Simulation.Scenario.Nodes.First(node => node.Id == id);

    public JammingZoneDefinition ZoneDefinitionOf(string id) => Simulation.Scenario.JammingZones.First(zone => zone.Id == id);

    public double Scale => Map.Transform?.Scale ?? throw new InvalidOperationException("Вікно має нульовий розмір.");
}