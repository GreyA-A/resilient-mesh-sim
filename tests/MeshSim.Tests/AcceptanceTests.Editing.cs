using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public partial class AcceptanceTests
{
    // ================= AC-05: інтерактивне редагування сцени =================

    [Fact]
    public void Ac05EditorAddsMovesAndTogglesObjectsAndUpdatesTheMap()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        SceneEditorViewModel editor = f.Editor;

        // 1. Додавання вузла N7.
        editor.NodeId = "N7";
        editor.NodeX = "60";
        editor.NodeY = "-60";
        editor.AddNodeCommand.Execute(null);
        Assert.False(editor.HasError);
        Assert.Equal(7, f.Simulation.Scenario.Nodes.Count);
        Assert.True(f.Map.Topology.Nodes.ContainsKey("N7"));
        Assert.Equal(7, f.Map.Scene.Nodes.Count);
        Assert.Equal(SimulationState.Ready, f.Simulation.State);

        // 2. Перетягування N7 на 10 м праворуч.
        ScreenPoint start = f.Map.Scene.Nodes.First(node => node.Id == "N7").Center;
        double scale = f.Scale;
        editor.PointerPressed(start);
        Assert.Equal(new MapTarget(MapTargetKind.Node, "N7"), f.Map.Selection);
        editor.PointerMoved(new ScreenPoint(start.X + (10.0 * scale), start.Y));
        editor.PointerReleased();
        NodeDefinition moved = f.Simulation.Scenario.Nodes.First(node => node.Id == "N7");
        Assert.Equal(70.0, moved.X);
        Assert.Equal(-60.0, moved.Y);
        Assert.Equal(new Position(70.0, -60.0), f.Map.Topology.Nodes["N7"].Position);

        // 3. Перемикання активності зони J1: якість лінка N2–N3 зростає з 0.4 до 1.0.
        LinkVisual jammed = f.Map.Scene.Links.First(link => link.FromId == "N2" && link.ToId == "N3");
        Assert.Equal(0.4, jammed.Quality, precision: 9);
        ScreenPoint zoneCenter = f.Map.Scene.Zones.First(zone => zone.Id == "J1").Center;
        editor.PointerPressed(zoneCenter);
        editor.PointerReleased();
        Assert.Equal(new MapTarget(MapTargetKind.Zone, "J1"), f.Map.Selection);
        editor.ToggleZoneCommand.Execute(null);
        Assert.False(editor.HasError);
        Assert.False(f.Simulation.Scenario.JammingZones.First(zone => zone.Id == "J1").IsActive);
        Assert.False(f.Map.Topology.JammingZones.First(zone => zone.Id == "J1").IsActive);
        LinkVisual cleared = f.Map.Scene.Links.First(link => link.FromId == "N2" && link.ToId == "N3");
        Assert.Equal(1.0, cleared.Quality);

        // 4. Додавання й видалення пакета.
        editor.PacketId = "P2";
        editor.PacketSource = "N1";
        editor.PacketDestination = "N2";
        editor.AddPacketCommand.Execute(null);
        Assert.False(editor.HasError);
        Assert.Equal(2, editor.Packets.Count);
        editor.SelectedPacket = editor.Packets[1];
        editor.RemovePacketCommand.Execute(null);
        Assert.Single(editor.Packets);
        Assert.Single(f.Simulation.Scenario.Packets);

        // 5. Видалення вибраного вузла (на N7 не посилається жоден пакет).
        ScreenPoint nodeCenter = f.Map.Scene.Nodes.First(node => node.Id == "N7").Center;
        editor.PointerPressed(nodeCenter);
        editor.PointerReleased();
        editor.RemoveNodeCommand.Execute(null);
        Assert.False(editor.HasError);
        Assert.Equal(6, f.Simulation.Scenario.Nodes.Count);
        Assert.False(f.Map.Topology.Nodes.ContainsKey("N7"));
        Assert.Null(f.Map.Selection);
    }

    [Theory]
    [InlineData("NodeId", "N1", "дублюється")]
    [InlineData("NodeId", "", "не може бути порожнім")]
    [InlineData("NodeX", "abc", "X вузла")]
    [InlineData("NodeY", "1,5", "Y вузла")]
    [InlineData("NodeRange", "-5", "transmissionRangeMeters")]
    [InlineData("NodeBattery", "101", "batteryLevelPercent")]
    public void Ac05InvalidFormInputIsRejectedWithoutChangingTheScene(string field, string value, string expectedFragment)
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        SceneEditorViewModel editor = f.Editor;
        ScenarioDefinition before = f.Simulation.Scenario;
        NetworkTopology topologyBefore = f.Map.Topology;

        editor.NodeId = "N9";
        editor.NodeX = "10";
        editor.NodeY = "10";
        editor.NodeRange = "140";
        editor.NodeBattery = "100";

        switch (field)
        {
            case nameof(SceneEditorViewModel.NodeId):
                editor.NodeId = value;
                break;
            case nameof(SceneEditorViewModel.NodeX):
                editor.NodeX = value;
                break;
            case nameof(SceneEditorViewModel.NodeY):
                editor.NodeY = value;
                break;
            case nameof(SceneEditorViewModel.NodeRange):
                editor.NodeRange = value;
                break;
            default:
                editor.NodeBattery = value;
                break;
        }

        editor.AddNodeCommand.Execute(null);

        Assert.True(editor.HasError);
        Assert.Contains(expectedFragment, editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Same(before, f.Simulation.Scenario);
        Assert.Same(topologyBefore, f.Map.Topology);
        Assert.Empty(f.Notifier.Messages);

        // Після виправлення введення помилка зникає й вузол додається.
        editor.NodeId = "N9";
        editor.NodeX = "10";
        editor.NodeY = "10";
        editor.NodeRange = "140";
        editor.NodeBattery = "100";
        editor.AddNodeCommand.Execute(null);

        Assert.False(editor.HasError);
        Assert.Null(editor.ErrorMessage);
        Assert.Equal(7, f.Simulation.Scenario.Nodes.Count);
    }

    [Fact]
    public void Ac05InvalidPacketAndZoneInputIsRejectedWithoutChangingTheScene()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        SceneEditorViewModel editor = f.Editor;
        ScenarioDefinition before = f.Simulation.Scenario;

        void AssertRejected(string expectedFragment)
        {
            Assert.True(editor.HasError);
            Assert.Contains(expectedFragment, editor.ErrorMessage, StringComparison.Ordinal);
            Assert.Same(before, f.Simulation.Scenario);
        }

        editor.ZoneId = "Z9";
        editor.ZoneIntensity = "1.5";
        editor.AddZoneCommand.Execute(null);
        AssertRejected("jammingIntensity");

        editor.ZoneIntensity = "0.5";
        editor.ZoneRadius = "abc";
        editor.AddZoneCommand.Execute(null);
        AssertRejected("Радіус зони");

        editor.PacketId = "P9";
        editor.PacketSource = "GHOST";
        editor.PacketDestination = "N2";
        editor.AddPacketCommand.Execute(null);
        AssertRejected("не існує");

        editor.PacketSource = "N1";
        editor.PacketTtl = "0";
        editor.AddPacketCommand.Execute(null);
        AssertRejected("ttl");

        // Виправлена зона додається, помилка очищується.
        editor.ZoneRadius = "30";
        editor.AddZoneCommand.Execute(null);
        Assert.False(editor.HasError);
        Assert.Equal(3, f.Simulation.Scenario.JammingZones.Count);
    }

    [Fact]
    public void Ac05EditingIsBlockedWhileSimulationRuns()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        SceneEditorViewModel editor = f.Editor;
        ScenarioDefinition before = f.Simulation.Scenario;

        f.Simulation.PlayCommand.Execute(null);
        editor.NodeId = "N7";
        editor.AddNodeCommand.Execute(null);

        Assert.False(editor.IsEditingEnabled);
        Assert.False(editor.AddNodeCommand.CanExecute(null));
        Assert.True(editor.HasError);
        Assert.Contains("Редагування недоступне", editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Same(before, f.Simulation.Scenario);

        // Виділення працює, але перетягування не починається.
        ScreenPoint center = f.Map.Scene.Nodes.First(node => node.Id == "N2").Center;
        editor.PointerPressed(center);
        Assert.NotNull(f.Map.Selection);
        Assert.False(editor.IsDragging);

        // Після паузи редагування знову доступне.
        f.Simulation.PauseCommand.Execute(null);
        editor.AddNodeCommand.Execute(null);

        Assert.False(editor.HasError);
        Assert.Equal(7, f.Simulation.Scenario.Nodes.Count);
    }

    // ================= AC-06: збереження редагованого сценарію та повторне відкриття =================

    [Fact]
    public async Task Ac06EditedScenarioSurvivesSaveAndReopenInFreshApplication()
    {
        using AcceptanceFixture first = AcceptanceFixture.Create();
        first.Editor.NodeId = "N7";
        first.Editor.NodeX = "60";
        first.Editor.NodeY = "-60";
        first.Editor.AddNodeCommand.Execute(null);
        first.Map.SetSelection(new MapTarget(MapTargetKind.Zone, "J1"));
        first.Editor.ToggleZoneCommand.Execute(null);
        Assert.False(first.Editor.HasError);

        string path = first.Temp.FilePath("edited.json");
        first.Dialogs.SaveScenarioPath = path;
        await first.Files.SaveScenarioAsync();

        Assert.Equal("Сценарій збережено у файл edited.json.", first.Main.StatusText);
        Assert.Empty(first.Notifier.Messages);
        string json = File.ReadAllText(path);
        Assert.Contains("\"id\": \"N7\"", json, StringComparison.Ordinal);
        Assert.Contains("\"isActive\": false", json, StringComparison.Ordinal);

        // «Новий запуск застосунку»: другий MainViewModel відкриває той самий файл.
        using AcceptanceFixture second = AcceptanceFixture.Create();
        second.Dialogs.OpenScenarioPath = path;
        await second.Files.OpenScenarioAsync();

        Assert.Empty(second.Notifier.Messages);
        Assert.Equal(
            ScenarioSerializer.Serialize(first.Simulation.Scenario),
            ScenarioSerializer.Serialize(second.Simulation.Scenario));
        Assert.Equal(7, second.Map.Topology.Nodes.Count);
        Assert.Equal(new Position(60.0, -60.0), second.Map.Topology.Nodes["N7"].Position);
        Assert.False(second.Map.Topology.JammingZones.First(zone => zone.Id == "J1").IsActive);
        Assert.Equal(SimulationState.Ready, second.Simulation.State);
    }
}