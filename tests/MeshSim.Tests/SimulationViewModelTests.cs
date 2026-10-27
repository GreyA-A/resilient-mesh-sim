using MeshSim.Desktop.Logging;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class SimulationViewModelTests
{
    /// <summary>
    /// Ланцюг A-B-C без прямого лінка A-C. Події: 0 Enq A, 10 Fwd B, 20 Del C.
    /// </summary>
    private static ScenarioDefinition ChainScenario()
        => Scenario(
            nodes: new[] { Node("A"), Node("B", x: 80.0), Node("C", x: 160.0) },
            packets: new[] { Packet("p1", "A", "C") });

    /// <summary>
    /// Пакет до ізольованого вузла D: 0 Enq A, 0 Dropped A (немає маршруту).
    /// </summary>
    private static ScenarioDefinition DropScenario()
        => Scenario(
            nodes: new[] { Node("A"), Node("B", x: 80.0), Node("D", x: 1000.0) },
            packets: new[] { Packet("p2", "A", "D") });

    private static ScenarioDefinition EmptyScenario() => Scenario();

    private sealed record Fixture(SimulationViewModel ViewModel, ManualSimulationTimer Timer, ListLogger Logger, MapViewModel Map);

    private static Fixture Create(ScenarioDefinition? scenario = null)
    {
        var map = new MapViewModel();
        var timer = new ManualSimulationTimer();
        var logger = new ListLogger();
        var viewModel = new SimulationViewModel(map, scenario ?? ChainScenario(), timer, logger);
        return new Fixture(viewModel, timer, logger, map);
    }

    private static PacketActivity ActivityOf(MapViewModel map)
    {
        Assert.True(map.Activity.HasValue);
        return map.Activity.GetValueOrDefault();
    }

    private static void RunToEndWithTicks(Fixture fixture)
    {
        fixture.ViewModel.PlayCommand.Execute(null);
        while (fixture.Timer.IsRunning)
        {
            fixture.Timer.FireTick();
        }
    }

    // ---------- Початковий стан ----------

    [Fact]
    public void InitialStateIsReadyWithScenarioLoadedIntoMap()
    {
        Fixture f = Create();
        SimulationViewModel vm = f.ViewModel;

        Assert.Same(f.Map, vm.Map);
        Assert.Equal(3, f.Map.Topology.Nodes.Count);
        Assert.Equal(SimulationState.Ready, vm.State);
        Assert.Equal("Готово", vm.StateText);
        Assert.Empty(vm.EventLog);
        Assert.Equal(1, vm.Metrics.TotalPacketsSent);
        Assert.Equal("0.0 %", vm.Metrics.PacketDeliveryRatioText);
        Assert.Null(f.Map.Activity);
    }

    [Fact]
    public void InitialCommandAvailabilityMatchesReadyState()
    {
        SimulationViewModel vm = Create().ViewModel;

        Assert.True(vm.PlayCommand.CanExecute(null));
        Assert.False(vm.PauseCommand.CanExecute(null));
        Assert.True(vm.StepCommand.CanExecute(null));
        Assert.True(vm.ResetCommand.CanExecute(null));
    }

    [Fact]
    public void InitialSpeedIsOneTimesAndTimerIntervalIsApplied()
    {
        Fixture f = Create();

        Assert.Equal(5, f.ViewModel.SpeedOptions.Count);
        Assert.Equal(1.0, f.ViewModel.SelectedSpeed.Multiplier);
        Assert.Equal(SimulationViewModel.BaseTickInterval, f.Timer.Interval);
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionForNullArguments()
    {
        var map = new MapViewModel();
        var timer = new ManualSimulationTimer();
        var logger = new ListLogger();

        Assert.Throws<ArgumentNullException>(() => new SimulationViewModel(null!, ChainScenario(), timer, logger));
        Assert.Throws<ArgumentNullException>(() => new SimulationViewModel(map, null!, timer, logger));
        Assert.Throws<ArgumentNullException>(() => new SimulationViewModel(map, ChainScenario(), null!, logger));
        Assert.Throws<ArgumentNullException>(() => new SimulationViewModel(map, ChainScenario(), timer, null!));
    }

    // ---------- Step ----------

    [Fact]
    public void StepCommandAdvancesOneEventAndUpdatesLogMetricsAndMap()
    {
        Fixture f = Create();
        SimulationViewModel vm = f.ViewModel;

        vm.StepCommand.Execute(null);

        Assert.Equal(SimulationState.Paused, vm.State);
        Assert.Equal("Пауза", vm.StateText);
        string row = Assert.Single(vm.EventLog);
        Assert.Equal("0.0 мс | p1 | Поставлено в чергу | вузол A", row);
        Assert.Equal("0.0 мс", vm.Metrics.CurrentTimeText);
        PacketActivity activity = ActivityOf(f.Map);
        Assert.Equal("p1", activity.PacketId);
        Assert.Equal("A", activity.NodeId);
        Assert.Null(activity.PreviousNodeId);
        Assert.Equal(SimulationEventType.PacketEnqueued, activity.EventType);
        Assert.Equal(PacketDropReason.None, activity.DropReason);
        Assert.Equal(99.0, f.Map.Topology.Nodes["A"].BatteryLevelPercent);
    }

    [Fact]
    public void StepsTrackPreviousNodeAndReachFinishedState()
    {
        Fixture f = Create();
        SimulationViewModel vm = f.ViewModel;

        vm.StepCommand.Execute(null);
        vm.StepCommand.Execute(null);

        PacketActivity second = ActivityOf(f.Map);
        Assert.Equal("B", second.NodeId);
        Assert.Equal("A", second.PreviousNodeId);
        Assert.Equal(SimulationEventType.PacketForwarded, second.EventType);
        Assert.Equal("10.0 мс | p1 | Передано на вузол | вузол B", vm.EventLog[0]);
        Assert.Equal(1, vm.Metrics.TotalPacketsDelivered);

        vm.StepCommand.Execute(null);

        Assert.Equal(SimulationState.Finished, vm.State);
        Assert.Equal("Завершено", vm.StateText);
        Assert.Equal(3, vm.EventLog.Count);
        Assert.Equal("20.0 мс | p1 | Доставлено | вузол C", vm.EventLog[0]);
        Assert.Equal("100.0 %", vm.Metrics.PacketDeliveryRatioText);
        Assert.Equal("2.00", vm.Metrics.AverageHopCountText);
        Assert.Equal("20.0 мс", vm.Metrics.CurrentTimeText);
        PacketActivity last = ActivityOf(f.Map);
        Assert.Equal("C", last.NodeId);
        Assert.Equal("B", last.PreviousNodeId);
        Assert.Equal(SimulationEventType.PacketDelivered, last.EventType);
    }

    [Fact]
    public void CommandsAreDisabledAfterRunFinishes()
    {
        SimulationViewModel vm = Create().ViewModel;
        vm.StepCommand.Execute(null);
        vm.StepCommand.Execute(null);
        vm.StepCommand.Execute(null);

        Assert.False(vm.PlayCommand.CanExecute(null));
        Assert.False(vm.StepCommand.CanExecute(null));
        Assert.False(vm.PauseCommand.CanExecute(null));
        Assert.True(vm.ResetCommand.CanExecute(null));
    }

    [Fact]
    public void StepAfterFinishDoesNothing()
    {
        SimulationViewModel vm = Create().ViewModel;
        for (int i = 0; i < 3; i++)
        {
            vm.StepCommand.Execute(null);
        }

        vm.StepCommand.Execute(null);

        Assert.Equal(3, vm.EventLog.Count);
        Assert.Equal(SimulationState.Finished, vm.State);
    }

    [Fact]
    public void StepOnEmptyQueueFinishesRunGracefully()
    {
        Fixture f = Create(EmptyScenario());

        Assert.False(f.ViewModel.StepCommand.CanExecute(null));
        f.ViewModel.StepCommand.Execute(null);

        Assert.Equal(SimulationState.Finished, f.ViewModel.State);
        Assert.Empty(f.ViewModel.EventLog);
    }

    [Fact]
    public void StepMarkerAppearsInMapSceneAfterViewportResize()
    {
        Fixture f = Create();
        f.Map.Resize(800.0, 600.0);

        f.ViewModel.StepCommand.Execute(null);

        Assert.True(f.Map.Scene.Marker.HasValue);
        Assert.Equal("p1", f.Map.Scene.Marker.GetValueOrDefault().PacketId);
    }

    [Fact]
    public void DroppedPacketShowsReasonInLogMetricsAndMarker()
    {
        Fixture f = Create(DropScenario());
        f.Map.Resize(800.0, 600.0);

        f.ViewModel.StepCommand.Execute(null);
        Assert.Equal(1, f.ViewModel.Metrics.TotalPacketsDropped);
        f.ViewModel.StepCommand.Execute(null);

        Assert.Equal(SimulationState.Finished, f.ViewModel.State);
        Assert.Equal("0.0 мс | p2 | Втрачено | вузол A | немає маршруту", f.ViewModel.EventLog[0]);
        DropReasonRow row = Assert.Single(f.ViewModel.Metrics.DropsByReason);
        Assert.Equal(PacketDropReason.NoRouteFound, row.Reason);
        Assert.Equal(1, row.Count);
        Assert.Equal("0.0 %", f.ViewModel.Metrics.PacketDeliveryRatioText);
        PacketMarkerVisual marker = f.Map.Scene.Marker.GetValueOrDefault();
        Assert.Equal("p2: немає маршруту", marker.Caption);
        Assert.Null(f.Map.Scene.Segment);
    }

    // ---------- Play / Pause / Tick ----------

    [Fact]
    public void PlayStartsTimerAndSwitchesToRunningState()
    {
        Fixture f = Create();

        f.ViewModel.PlayCommand.Execute(null);

        Assert.True(f.Timer.IsRunning);
        Assert.Equal(1, f.Timer.StartCount);
        Assert.Equal(SimulationState.Running, f.ViewModel.State);
        Assert.Equal("Виконується", f.ViewModel.StateText);
        Assert.False(f.ViewModel.PlayCommand.CanExecute(null));
        Assert.True(f.ViewModel.PauseCommand.CanExecute(null));
        Assert.False(f.ViewModel.StepCommand.CanExecute(null));
    }

    [Fact]
    public void PlayWhileRunningDoesNotRestartTimer()
    {
        Fixture f = Create();
        f.ViewModel.PlayCommand.Execute(null);

        f.ViewModel.PlayCommand.Execute(null);

        Assert.Equal(1, f.Timer.StartCount);
    }

    [Fact]
    public void TicksAdvanceSimulationAndTimerStopsWhenEventsAreExhausted()
    {
        Fixture f = Create();
        f.ViewModel.PlayCommand.Execute(null);

        f.Timer.FireTick();
        f.Timer.FireTick();
        Assert.True(f.Timer.IsRunning);
        Assert.Equal(SimulationState.Running, f.ViewModel.State);

        f.Timer.FireTick();

        Assert.False(f.Timer.IsRunning);
        Assert.Equal(SimulationState.Finished, f.ViewModel.State);
        Assert.Equal(3, f.ViewModel.EventLog.Count);
        Assert.Equal("100.0 %", f.ViewModel.Metrics.PacketDeliveryRatioText);
    }

    [Fact]
    public void TickOnEmptyQueueWhileRunningFinishesAndStopsTimer()
    {
        Fixture f = Create(EmptyScenario());
        Assert.False(f.ViewModel.PlayCommand.CanExecute(null));

        f.ViewModel.PlayCommand.Execute(null);
        f.Timer.FireTick();

        Assert.False(f.Timer.IsRunning);
        Assert.Equal(SimulationState.Finished, f.ViewModel.State);
    }

    [Fact]
    public void TickIsIgnoredWhenNotRunning()
    {
        Fixture f = Create();

        f.Timer.FireTick();
        Assert.Empty(f.ViewModel.EventLog);

        f.ViewModel.StepCommand.Execute(null);
        f.Timer.FireTick();
        Assert.Single(f.ViewModel.EventLog);
    }

    [Fact]
    public void PauseStopsTimerAndFreezesSimulation()
    {
        Fixture f = Create();
        f.ViewModel.PlayCommand.Execute(null);
        f.Timer.FireTick();

        f.ViewModel.PauseCommand.Execute(null);
        f.Timer.FireTick();

        Assert.False(f.Timer.IsRunning);
        Assert.Equal(SimulationState.Paused, f.ViewModel.State);
        Assert.Single(f.ViewModel.EventLog);
        Assert.True(f.ViewModel.PlayCommand.CanExecute(null));
    }

    [Fact]
    public void PauseWhenNotRunningDoesNothing()
    {
        Fixture f = Create();

        f.ViewModel.PauseCommand.Execute(null);

        Assert.Equal(0, f.Timer.PauseCount);
        Assert.Equal(SimulationState.Ready, f.ViewModel.State);
    }

    [Fact]
    public void PlayAfterPauseResumesFromWhereItStopped()
    {
        Fixture f = Create();
        f.ViewModel.PlayCommand.Execute(null);
        f.Timer.FireTick();
        f.ViewModel.PauseCommand.Execute(null);

        f.ViewModel.PlayCommand.Execute(null);
        f.Timer.FireTick();

        Assert.Equal(2, f.Timer.StartCount);
        Assert.Equal(2, f.ViewModel.EventLog.Count);
    }

    [Fact]
    public void StepWhileRunningIsIgnored()
    {
        Fixture f = Create();
        f.ViewModel.PlayCommand.Execute(null);

        f.ViewModel.StepCommand.Execute(null);

        Assert.Empty(f.ViewModel.EventLog);
        Assert.Equal(SimulationState.Running, f.ViewModel.State);
    }

    // ---------- Reset ----------

    [Fact]
    public void ResetRestoresInitialStateAndFreshTopology()
    {
        Fixture f = Create();
        NetworkTopology before = f.Map.Topology;
        f.ViewModel.StepCommand.Execute(null);
        f.ViewModel.StepCommand.Execute(null);
        Assert.Equal(99.0, f.Map.Topology.Nodes["A"].BatteryLevelPercent);

        f.ViewModel.ResetCommand.Execute(null);

        Assert.Equal(SimulationState.Ready, f.ViewModel.State);
        Assert.Empty(f.ViewModel.EventLog);
        Assert.Equal(0, f.ViewModel.Metrics.TotalPacketsDelivered);
        Assert.Equal("0.0 мс", f.ViewModel.Metrics.CurrentTimeText);
        Assert.Null(f.Map.Activity);
        Assert.NotSame(before, f.Map.Topology);
        Assert.Equal(100.0, f.Map.Topology.Nodes["A"].BatteryLevelPercent);
        Assert.True(f.ViewModel.StepCommand.CanExecute(null));
    }

    [Fact]
    public void ResetWhileRunningStopsTimer()
    {
        Fixture f = Create();
        f.ViewModel.PlayCommand.Execute(null);
        f.Timer.FireTick();

        f.ViewModel.ResetCommand.Execute(null);

        Assert.False(f.Timer.IsRunning);
        Assert.Equal(SimulationState.Ready, f.ViewModel.State);
        Assert.Empty(f.ViewModel.EventLog);
    }

    [Fact]
    public void ResetAtInitialStateKeepsReadyState()
    {
        Fixture f = Create();

        f.ViewModel.ResetCommand.Execute(null);

        Assert.Equal(SimulationState.Ready, f.ViewModel.State);
    }

    [Fact]
    public void RunAfterResetProducesIdenticalLogAndMetrics()
    {
        Fixture f = Create();
        RunToEndWithTicks(f);
        string[] firstLog = f.ViewModel.EventLog.ToArray();
        string firstRatio = f.ViewModel.Metrics.PacketDeliveryRatioText;

        f.ViewModel.ResetCommand.Execute(null);
        RunToEndWithTicks(f);

        Assert.Equal(firstLog, f.ViewModel.EventLog);
        Assert.Equal(firstRatio, f.ViewModel.Metrics.PacketDeliveryRatioText);
        Assert.Equal(SimulationState.Finished, f.ViewModel.State);
    }

    // ---------- Швидкість ----------

    [Fact]
    public void SelectingSpeedUpdatesTimerInterval()
    {
        Fixture f = Create();

        f.ViewModel.SelectedSpeed = f.ViewModel.SpeedOptions[3];

        Assert.Equal(2.0, f.ViewModel.SelectedSpeed.Multiplier);
        Assert.Equal(TimeSpan.FromMilliseconds(400), f.Timer.Interval);
    }

    [Fact]
    public void SelectingSpeedRaisesPropertyChanged()
    {
        SimulationViewModel vm = Create().ViewModel;

        Assert.PropertyChanged(vm, nameof(SimulationViewModel.SelectedSpeed), () => vm.SelectedSpeed = vm.SpeedOptions[0]);
    }

    [Fact]
    public void SelectingSameSpeedAgainDoesNotChangeTimerOrRaiseEvent()
    {
        Fixture f = Create();
        int raised = 0;
        f.ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SimulationViewModel.SelectedSpeed))
            {
                raised++;
            }
        };

        f.ViewModel.SelectedSpeed = f.ViewModel.SelectedSpeed;

        Assert.Equal(0, raised);
        Assert.Equal(SimulationViewModel.BaseTickInterval, f.Timer.Interval);
    }

    [Fact]
    public void SelectingNullSpeedIsIgnored()
    {
        Fixture f = Create();
        SimulationSpeedOption before = f.ViewModel.SelectedSpeed;

        f.ViewModel.SelectedSpeed = null!;

        Assert.Same(before, f.ViewModel.SelectedSpeed);
    }

    // ---------- Сповіщення та журнал ----------

    [Fact]
    public void StateChangeRaisesStateAndStateTextNotifications()
    {
        SimulationViewModel vm = Create().ViewModel;
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.PlayCommand.Execute(null);

        Assert.Contains(nameof(SimulationViewModel.State), changed);
        Assert.Contains(nameof(SimulationViewModel.StateText), changed);
    }

    [Fact]
    public void StateChangeRaisesCanExecuteChangedOnCommands()
    {
        SimulationViewModel vm = Create().ViewModel;
        bool raised = false;
        vm.PauseCommand.CanExecuteChanged += (_, _) => raised = true;

        vm.PlayCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public void UserActionsAreLogged()
    {
        Fixture f = Create();

        f.ViewModel.PlayCommand.Execute(null);
        f.ViewModel.PauseCommand.Execute(null);
        f.ViewModel.ResetCommand.Execute(null);
        RunToEndWithTicks(f);

        Assert.All(f.Logger.Entries, entry => Assert.Equal(LogLevel.Info, entry.Level));
        Assert.Contains(f.Logger.Entries, entry => entry.Message.Contains("запущено", StringComparison.Ordinal));
        Assert.Contains(f.Logger.Entries, entry => entry.Message.Contains("призупинено", StringComparison.Ordinal));
        Assert.Contains(f.Logger.Entries, entry => entry.Message.Contains("скинуто", StringComparison.Ordinal));
        Assert.Contains(f.Logger.Entries, entry => entry.Message.Contains("PDR 100.0 %", StringComparison.Ordinal));
    }

    // ---------- DescribeState ----------

    [Theory]
    [InlineData(SimulationState.Ready, "Готово")]
    [InlineData(SimulationState.Running, "Виконується")]
    [InlineData(SimulationState.Paused, "Пауза")]
    [InlineData(SimulationState.Finished, "Завершено")]
    public void DescribeStateReturnsUkrainianText(SimulationState state, string expected)
    {
        Assert.Equal(expected, SimulationViewModel.DescribeState(state));
    }

    [Fact]
    public void DescribeStateThrowsForUnknownState()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimulationViewModel.DescribeState((SimulationState)99));
    }
}