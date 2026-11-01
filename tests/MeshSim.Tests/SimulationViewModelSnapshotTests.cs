using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class SimulationViewModelSnapshotTests
{
    private static SimulationViewModel Create()
    {
        ScenarioDefinition scenario = Scenario(
            nodes: new[] { Node("A"), Node("B", x: 80.0), Node("C", x: 160.0) },
            packets: new[] { Packet("p1", "A", "C") });

        return new SimulationViewModel(new MapViewModel(), scenario, new ManualSimulationTimer(), new ListLogger());
    }

    [Fact]
    public void SnapshotOfNewViewModelHasScheduledPacketsAndNoEvents()
    {
        SimulationResult snapshot = Create().GetCurrentSnapshot();

        Assert.Equal(1, snapshot.TotalPacketsSent);
        Assert.Equal(0, snapshot.TotalPacketsDelivered);
        Assert.Empty(snapshot.EventLog);
    }

    [Fact]
    public void SnapshotReflectsStepsTakenSoFar()
    {
        SimulationViewModel viewModel = Create();
        viewModel.StepCommand.Execute(null);
        viewModel.StepCommand.Execute(null);

        SimulationResult snapshot = viewModel.GetCurrentSnapshot();

        Assert.Equal(2, snapshot.EventLog.Count);
        Assert.Equal(1, snapshot.TotalPacketsDelivered);
    }

    [Fact]
    public void SnapshotIsIndependentOfLaterSteps()
    {
        SimulationViewModel viewModel = Create();
        viewModel.StepCommand.Execute(null);
        SimulationResult early = viewModel.GetCurrentSnapshot();

        viewModel.StepCommand.Execute(null);

        Assert.Single(early.EventLog);
        Assert.Equal(2, viewModel.GetCurrentSnapshot().EventLog.Count);
    }

    [Fact]
    public void SnapshotAfterLoadScenarioDescribesTheNewScenario()
    {
        SimulationViewModel viewModel = Create();
        viewModel.StepCommand.Execute(null);

        viewModel.LoadScenario(Scenario(packets: new[] { Packet("q1"), Packet("q2", "B", "A") }));

        SimulationResult snapshot = viewModel.GetCurrentSnapshot();
        Assert.Equal(2, snapshot.TotalPacketsSent);
        Assert.Empty(snapshot.EventLog);
    }
}