using CommunityToolkit.Mvvm.ComponentModel;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.ViewModels;

namespace MeshSim.Tests;

public class MainViewModelTests
{
    private static MainViewModel Create(ListLogger? logger = null)
        => new(logger ?? new ListLogger(), new ManualSimulationTimer());

    [Fact]
    public void ConstructorSetsTitleAndLogsInitialization()
    {
        var logger = new ListLogger();

        MainViewModel viewModel = Create(logger);

        Assert.Equal(MainViewModel.AppTitle, viewModel.Title);
        Assert.IsAssignableFrom<ObservableObject>(viewModel);
        (LogLevel level, string message, Exception? exception) = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Info, level);
        Assert.Contains("ініціалізовано", message, StringComparison.Ordinal);
        Assert.Null(exception);
    }

    [Fact]
    public void ConstructorLoadsDemoScenarioIntoMapThroughSimulation()
    {
        MainViewModel viewModel = Create();

        Assert.Same(viewModel.Map, viewModel.Simulation.Map);
        Assert.Equal(6, viewModel.Map.Topology.Nodes.Count);
        Assert.Equal(2, viewModel.Map.Topology.JammingZones.Count);
        Assert.Equal(SimulationState.Ready, viewModel.Simulation.State);
    }

    [Fact]
    public void StatusTextDescribesLoadedScenario()
    {
        MainViewModel viewModel = Create();

        Assert.Contains("Демонстраційний сценарій", viewModel.StatusText, StringComparison.Ordinal);
        Assert.Contains("вузлів — 6", viewModel.StatusText, StringComparison.Ordinal);
        Assert.Contains("зон завад — 2", viewModel.StatusText, StringComparison.Ordinal);
        Assert.Contains("пакетів — 1", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void MapReceivesSceneAfterResize()
    {
        MainViewModel viewModel = Create();

        viewModel.Map.Resize(900.0, 600.0);

        Assert.Equal(6, viewModel.Map.Scene.Nodes.Count);
        Assert.Equal(2, viewModel.Map.Scene.Zones.Count);
    }

    [Fact]
    public void DemoPacketIsDeliveredStepByStepThroughSimulation()
    {
        MainViewModel viewModel = Create();

        for (int i = 0; i < 4; i++)
        {
            viewModel.Simulation.StepCommand.Execute(null);
        }

        Assert.Equal(SimulationState.Finished, viewModel.Simulation.State);
        Assert.Equal(4, viewModel.Simulation.EventLog.Count);
        Assert.Equal("100.0 %", viewModel.Simulation.Metrics.PacketDeliveryRatioText);
        Assert.Equal("3.00", viewModel.Simulation.Metrics.AverageHopCountText);
        Assert.Equal("60.0 мс", viewModel.Simulation.Metrics.CurrentTimeText);
    }

    [Fact]
    public void StatusTextRaisesPropertyChangedWhenValueChanges()
    {
        MainViewModel viewModel = Create();

        Assert.PropertyChanged(viewModel, nameof(MainViewModel.StatusText), () => viewModel.StatusText = "Нова подія");

        Assert.Equal("Нова подія", viewModel.StatusText);
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionForNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!, new ManualSimulationTimer()));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(new ListLogger(), null!));
    }
}