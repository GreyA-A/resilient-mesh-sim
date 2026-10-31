using MeshSim.Desktop.ViewModels;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class SimulationViewModelLoadScenarioTests
{
    private static ScenarioDefinition ChainScenario()
        => Scenario(
            nodes: new[] { Node("A"), Node("B", x: 80.0), Node("C", x: 160.0) },
            packets: new[] { Packet("p1", "A", "C") });

    private static ScenarioDefinition PairScenario()
        => Scenario(
            nodes: new[] { Node("X"), Node("Y", x: 50.0) },
            packets: new[] { Packet("q1", "X", "Y"), Packet("q2", "Y", "X") });

    private static (SimulationViewModel ViewModel, ManualSimulationTimer Timer, ScenarioDefinition Original) Create()
    {
        ScenarioDefinition original = ChainScenario();
        var timer = new ManualSimulationTimer();
        var viewModel = new SimulationViewModel(new MapViewModel(), original, timer, new ListLogger());
        return (viewModel, timer, original);
    }

    [Fact]
    public void ScenarioPropertyReturnsInitialScenario()
    {
        (SimulationViewModel viewModel, _, ScenarioDefinition original) = Create();

        Assert.Same(original, viewModel.Scenario);
    }

    [Fact]
    public void LoadScenarioReplacesScenarioAndRebuildsTopologyAndMetrics()
    {
        (SimulationViewModel viewModel, _, _) = Create();
        ScenarioDefinition replacement = PairScenario();

        viewModel.LoadScenario(replacement);

        Assert.Same(replacement, viewModel.Scenario);
        Assert.Equal(2, viewModel.Map.Topology.Nodes.Count);
        Assert.Equal(SimulationState.Ready, viewModel.State);
        Assert.Equal(2, viewModel.Metrics.TotalPacketsSent);
        Assert.Empty(viewModel.EventLog);
    }

    [Fact]
    public void LoadScenarioDiscardsRunProgressAndMapActivity()
    {
        (SimulationViewModel viewModel, _, _) = Create();
        viewModel.StepCommand.Execute(null);
        viewModel.StepCommand.Execute(null);
        Assert.Equal(2, viewModel.EventLog.Count);
        Assert.NotNull(viewModel.Map.Activity);

        viewModel.LoadScenario(ChainScenario());

        Assert.Empty(viewModel.EventLog);
        Assert.Null(viewModel.Map.Activity);
        Assert.Equal(SimulationState.Ready, viewModel.State);
        Assert.Equal("0.0 мс", viewModel.Metrics.CurrentTimeText);
    }

    [Fact]
    public void LoadScenarioStopsRunningTimer()
    {
        (SimulationViewModel viewModel, ManualSimulationTimer timer, _) = Create();
        viewModel.PlayCommand.Execute(null);
        Assert.True(timer.IsRunning);

        viewModel.LoadScenario(PairScenario());

        Assert.False(timer.IsRunning);
        Assert.Equal(SimulationState.Ready, viewModel.State);
    }

    [Fact]
    public void LoadScenarioRaisesScenarioPropertyChanged()
    {
        (SimulationViewModel viewModel, _, _) = Create();

        Assert.PropertyChanged(viewModel, nameof(SimulationViewModel.Scenario), () => viewModel.LoadScenario(PairScenario()));
    }

    [Fact]
    public void LoadScenarioRejectsInvalidScenarioWithoutChangingAnyState()
    {
        (SimulationViewModel viewModel, _, ScenarioDefinition original) = Create();
        viewModel.StepCommand.Execute(null);
        object topologyBefore = viewModel.Map.Topology;

        Assert.Throws<InvalidDataException>(() => viewModel.LoadScenario(Scenario(schemaVersion: 9)));

        Assert.Same(original, viewModel.Scenario);
        Assert.Same(topologyBefore, viewModel.Map.Topology);
        Assert.Single(viewModel.EventLog);
        Assert.Equal(SimulationState.Paused, viewModel.State);
    }

    [Fact]
    public void LoadScenarioThrowsArgumentNullExceptionForNull()
    {
        (SimulationViewModel viewModel, _, _) = Create();

        Assert.Throws<ArgumentNullException>(() => viewModel.LoadScenario(null!));
    }

    [Fact]
    public void ResetAfterLoadScenarioUsesTheNewScenario()
    {
        (SimulationViewModel viewModel, _, _) = Create();
        viewModel.LoadScenario(PairScenario());
        viewModel.StepCommand.Execute(null);

        viewModel.ResetCommand.Execute(null);

        Assert.Equal(2, viewModel.Map.Topology.Nodes.Count);
        Assert.Empty(viewModel.EventLog);
        Assert.Equal(SimulationState.Ready, viewModel.State);
    }
}