using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public class SceneEditorInteractionTests
{
    private static readonly ScreenPoint EmptySpot = new(5, 5);

    private static ScreenPoint Offset(ScreenPoint origin, double dx, double dy) => new(origin.X + dx, origin.Y + dy);

    // ---------- Виділення ----------

    [Fact]
    public void PressOnEmptySpaceClearsSelectionAndDoesNotStartDrag()
    {
        EditorFixture f = EditorFixture.Create();
        f.Editor.PointerPressed(f.NodeCenter("B"));
        f.Editor.PointerReleased();

        f.Editor.PointerPressed(EmptySpot);

        Assert.Null(f.Map.Selection);
        Assert.False(f.Editor.IsDragging);
        Assert.Equal("Нічого не вибрано.", f.Editor.SelectionText);
    }

    [Fact]
    public void PressOnNodeSelectsItAndStartsDrag()
    {
        EditorFixture f = EditorFixture.Create();

        f.Editor.PointerPressed(f.NodeCenter("B"));

        Assert.Equal(new MapTarget(MapTargetKind.Node, "B"), f.Map.Selection);
        Assert.Equal("Вибрано вузол B", f.Editor.SelectionText);
        Assert.True(f.Editor.IsDragging);
        Assert.True(f.Map.IsDragging);
        Assert.True(f.Map.Scene.Selection.HasValue);
    }

    [Fact]
    public void PressOnZoneSelectsIt()
    {
        EditorFixture f = EditorFixture.Create();

        f.Editor.PointerPressed(f.ZoneCenter("z1"));

        Assert.Equal(new MapTarget(MapTargetKind.Zone, "z1"), f.Map.Selection);
        Assert.Equal("Вибрано зону завад z1", f.Editor.SelectionText);
        Assert.True(f.Editor.IsDragging);
    }

    [Fact]
    public void PressWhileSimulationIsRunningSelectsButDoesNotDrag()
    {
        EditorFixture f = EditorFixture.Create();
        f.Simulation.PlayCommand.Execute(null);

        f.Editor.PointerPressed(f.NodeCenter("B"));

        Assert.NotNull(f.Map.Selection);
        Assert.False(f.Editor.IsDragging);
        Assert.False(f.Map.IsDragging);
    }

    // ---------- Перетягування ----------

    [Fact]
    public void DragMovesNodeByCursorDeltaAndKeepsSelection()
    {
        EditorFixture f = EditorFixture.Create();
        double scale = f.Scale;
        ScreenPoint start = f.NodeCenter("B");

        f.Editor.PointerPressed(start);
        f.Editor.PointerMoved(Offset(start, 10.0 * scale, -5.0 * scale));
        f.Editor.PointerReleased();

        NodeDefinition moved = f.NodeDefinitionOf("B");
        Assert.Equal(90.0, moved.X);
        Assert.Equal(5.0, moved.Y);
        Assert.Equal(new Position(90.0, 5.0), f.Map.Topology.Nodes["B"].Position);
        Assert.Equal(new MapTarget(MapTargetKind.Node, "B"), f.Map.Selection);
        Assert.False(f.Editor.IsDragging);
        Assert.False(f.Map.IsDragging);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Переміщено вузол B.");
    }

    [Fact]
    public void DragMovesZoneByCursorDelta()
    {
        EditorFixture f = EditorFixture.Create();
        double scale = f.Scale;
        ScreenPoint start = f.ZoneCenter("z1");

        f.Editor.PointerPressed(start);
        f.Editor.PointerMoved(Offset(start, -10.0 * scale, 5.0 * scale));
        f.Editor.PointerReleased();

        JammingZoneDefinition moved = f.ZoneDefinitionOf("z1");
        Assert.Equal(70.0, moved.X);
        Assert.Equal(95.0, moved.Y);
        Assert.Equal(30.0, moved.RadiusMeters);
        Assert.Contains(f.Logger.Entries, entry => entry.Message == "Переміщено зону завад z1.");
    }

    [Fact]
    public void DragKeepsGrabOffsetSoNodeDoesNotJumpToCursor()
    {
        EditorFixture f = EditorFixture.Create();
        double scale = f.Scale;
        ScreenPoint start = f.NodeCenter("B");
        ScreenPoint grab = Offset(start, 5.0, 5.0);

        f.Editor.PointerPressed(grab);
        f.Editor.PointerMoved(Offset(grab, 10.0 * scale, 0.0));

        Assert.Equal(90.0, f.NodeDefinitionOf("B").X);
        Assert.Equal(0.0, f.NodeDefinitionOf("B").Y);
    }

    [Fact]
    public void DragSnapsCoordinatesToOneDecimalPlace()
    {
        EditorFixture f = EditorFixture.Create();
        double scale = f.Scale;
        ScreenPoint start = f.NodeCenter("B");
        f.Editor.PointerPressed(start);

        f.Editor.PointerMoved(Offset(start, 10.04 * scale, 0.0));
        Assert.Equal(90.0, f.NodeDefinitionOf("B").X);

        f.Editor.PointerMoved(Offset(start, 10.06 * scale, 0.0));
        Assert.Equal(90.1, f.NodeDefinitionOf("B").X);
    }

    [Fact]
    public void DragKeepsPacketListUntouchedAndSelectionOfPacket()
    {
        EditorFixture f = EditorFixture.Create();
        PacketRow row = f.Editor.Packets[0];
        f.Editor.SelectedPacket = row;
        double scale = f.Scale;
        ScreenPoint start = f.NodeCenter("B");

        f.Editor.PointerPressed(start);
        f.Editor.PointerMoved(Offset(start, 10.0 * scale, 0.0));

        Assert.Same(row, f.Editor.SelectedPacket);
        Assert.Same(row, f.Editor.Packets[0]);
    }

    [Fact]
    public void DragInPausedStateResetsRunProgress()
    {
        EditorFixture f = EditorFixture.Create();
        f.Simulation.StepCommand.Execute(null);
        Assert.Single(f.Simulation.EventLog);
        double scale = f.Scale;
        ScreenPoint start = f.NodeCenter("B");

        f.Editor.PointerPressed(start);
        f.Editor.PointerMoved(Offset(start, 10.0 * scale, 0.0));

        Assert.Empty(f.Simulation.EventLog);
        Assert.Equal(SimulationState.Ready, f.Simulation.State);
    }

    [Fact]
    public void ReleaseWithoutMovementDoesNotLogOrChangeScenario()
    {
        EditorFixture f = EditorFixture.Create();
        ScenarioDefinition before = f.Simulation.Scenario;

        f.Editor.PointerPressed(f.NodeCenter("B"));
        f.Editor.PointerReleased();

        Assert.Same(before, f.Simulation.Scenario);
        Assert.Empty(f.Logger.Entries);
        Assert.False(f.Editor.IsDragging);
    }

    [Fact]
    public void MoveAndReleaseWithoutDragAreIgnored()
    {
        EditorFixture f = EditorFixture.Create();
        ScenarioDefinition before = f.Simulation.Scenario;

        f.Editor.PointerMoved(new ScreenPoint(100, 100));
        f.Editor.PointerReleased();

        Assert.Same(before, f.Simulation.Scenario);
        Assert.Empty(f.Logger.Entries);
    }

    [Fact]
    public void MoveIsIgnoredWhenViewportCollapsesDuringDrag()
    {
        EditorFixture f = EditorFixture.Create();
        ScreenPoint start = f.NodeCenter("B");
        f.Editor.PointerPressed(start);
        f.Map.Resize(0.0, 0.0);
        ScenarioDefinition before = f.Simulation.Scenario;

        f.Editor.PointerMoved(Offset(start, 30.0, 0.0));
        f.Editor.PointerReleased();

        Assert.Same(before, f.Simulation.Scenario);
        Assert.False(f.Editor.IsDragging);
        Assert.False(f.Map.IsDragging);
    }

    [Fact]
    public void MoveIsRejectedWhenSimulationStartsMidDrag()
    {
        EditorFixture f = EditorFixture.Create();
        ScreenPoint start = f.NodeCenter("B");
        f.Editor.PointerPressed(start);
        f.Simulation.PlayCommand.Execute(null);
        ScenarioDefinition before = f.Simulation.Scenario;

        f.Editor.PointerMoved(Offset(start, 30.0, 0.0));
        f.Editor.PointerReleased();

        Assert.True(f.Editor.HasError);
        Assert.Contains("Редагування недоступне", f.Editor.ErrorMessage, StringComparison.Ordinal);
        Assert.Same(before, f.Simulation.Scenario);
        Assert.DoesNotContain(f.Logger.Entries, entry => entry.Message.StartsWith("Переміщено", StringComparison.Ordinal));
    }

    // ---------- Блокування під час виконання та доступність команд ----------

    [Fact]
    public void CommandsAreRejectedWhileSimulationIsRunning()
    {
        EditorFixture f = EditorFixture.Create();
        f.Editor.NodeId = "D";
        f.Editor.ZoneId = "z2";
        f.Editor.PacketId = "p2";
        f.Editor.PacketSource = "A";
        f.Editor.PacketDestination = "B";
        f.Editor.SelectedPacket = f.Editor.Packets[0];
        f.Simulation.PlayCommand.Execute(null);
        ScenarioDefinition before = f.Simulation.Scenario;

        f.Map.SetSelection(new MapTarget(MapTargetKind.Node, "B"));
        f.Editor.AddNodeCommand.Execute(null);
        f.Editor.RemoveNodeCommand.Execute(null);
        f.Editor.AddZoneCommand.Execute(null);
        f.Editor.AddPacketCommand.Execute(null);
        f.Editor.RemovePacketCommand.Execute(null);
        f.Map.SetSelection(new MapTarget(MapTargetKind.Zone, "z1"));
        f.Editor.RemoveZoneCommand.Execute(null);
        f.Editor.ToggleZoneCommand.Execute(null);

        Assert.Same(before, f.Simulation.Scenario);
        Assert.Contains("Редагування недоступне", f.Editor.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void IsEditingEnabledFollowsSimulationStateAndRaisesNotification()
    {
        EditorFixture f = EditorFixture.Create();

        Assert.PropertyChanged(f.Editor, nameof(SceneEditorViewModel.IsEditingEnabled), () => f.Simulation.PlayCommand.Execute(null));
        Assert.False(f.Editor.IsEditingEnabled);
        Assert.False(f.Editor.AddNodeCommand.CanExecute(null));

        f.Simulation.PauseCommand.Execute(null);

        Assert.True(f.Editor.IsEditingEnabled);
        Assert.True(f.Editor.AddNodeCommand.CanExecute(null));
    }

    [Fact]
    public void SelectionDrivesAvailabilityOfRemoveAndToggleCommands()
    {
        EditorFixture f = EditorFixture.Create();
        Assert.False(f.Editor.RemoveNodeCommand.CanExecute(null));
        Assert.False(f.Editor.RemoveZoneCommand.CanExecute(null));
        Assert.False(f.Editor.ToggleZoneCommand.CanExecute(null));

        f.Map.SetSelection(new MapTarget(MapTargetKind.Node, "B"));
        Assert.True(f.Editor.RemoveNodeCommand.CanExecute(null));
        Assert.False(f.Editor.RemoveZoneCommand.CanExecute(null));

        f.Map.SetSelection(new MapTarget(MapTargetKind.Zone, "z1"));
        Assert.False(f.Editor.RemoveNodeCommand.CanExecute(null));
        Assert.True(f.Editor.RemoveZoneCommand.CanExecute(null));
        Assert.True(f.Editor.ToggleZoneCommand.CanExecute(null));

        f.Simulation.PlayCommand.Execute(null);
        Assert.False(f.Editor.RemoveZoneCommand.CanExecute(null));
        f.Map.SetSelection(new MapTarget(MapTargetKind.Node, "B"));
        Assert.False(f.Editor.RemoveNodeCommand.CanExecute(null));
    }

    [Fact]
    public void SelectedPacketDrivesRemovePacketCommandAvailability()
    {
        EditorFixture f = EditorFixture.Create();
        Assert.False(f.Editor.RemovePacketCommand.CanExecute(null));

        f.Editor.SelectedPacket = f.Editor.Packets[0];
        Assert.True(f.Editor.RemovePacketCommand.CanExecute(null));

        f.Simulation.PlayCommand.Execute(null);
        Assert.False(f.Editor.RemovePacketCommand.CanExecute(null));
    }

    [Fact]
    public void SettingSameSelectedPacketTwiceDoesNotNotifyAgain()
    {
        EditorFixture f = EditorFixture.Create();
        f.Editor.SelectedPacket = f.Editor.Packets[0];
        int raised = 0;
        f.Editor.RemovePacketCommand.CanExecuteChanged += (_, _) => raised++;

        f.Editor.SelectedPacket = f.Editor.Packets[0];

        Assert.Equal(0, raised);
    }

    [Fact]
    public void SelectionChangeRaisesSelectionTextAndCanExecuteNotifications()
    {
        EditorFixture f = EditorFixture.Create();
        bool canExecuteRaised = false;
        f.Editor.RemoveNodeCommand.CanExecuteChanged += (_, _) => canExecuteRaised = true;

        Assert.PropertyChanged(
            f.Editor,
            nameof(SceneEditorViewModel.SelectionText),
            () => f.Map.SetSelection(new MapTarget(MapTargetKind.Node, "B")));

        Assert.True(canExecuteRaised);
    }

    [Fact]
    public void UnrelatedSimulationPropertyChangeDoesNotAffectEditor()
    {
        EditorFixture f = EditorFixture.Create();

        f.Simulation.SelectedSpeed = f.Simulation.SpeedOptions[0];

        Assert.True(f.Editor.IsEditingEnabled);
        Assert.Single(f.Editor.Packets);
    }

    [Fact]
    public void ExternalScenarioLoadRefreshesPacketListAndClearsPacketSelection()
    {
        EditorFixture f = EditorFixture.Create();
        f.Editor.SelectedPacket = f.Editor.Packets[0];
        ScenarioDefinition replacement = ScenarioTestData.Scenario(
            nodes: new[] { ScenarioTestData.Node("X"), ScenarioTestData.Node("Y", x: 50.0) },
            packets: new[] { ScenarioTestData.Packet("q1", "X", "Y"), ScenarioTestData.Packet("q2", "Y", "X") });

        f.Simulation.LoadScenario(replacement);

        Assert.Equal(2, f.Editor.Packets.Count);
        Assert.Equal("q1", f.Editor.Packets[0].Id);
        Assert.Null(f.Editor.SelectedPacket);
    }
}