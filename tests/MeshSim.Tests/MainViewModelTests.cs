using CommunityToolkit.Mvvm.ComponentModel;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class MainViewModelTests
{
    private static MainViewModel Create(ListLogger? logger = null, StubFileDialogService? dialogs = null)
        => new(logger ?? new ListLogger(), new ManualSimulationTimer(), dialogs ?? new StubFileDialogService(), new RecordingNotifier());

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
    public void EditorAndFilesAreWiredToSimulationAndMap()
    {
        MainViewModel viewModel = Create();

        Assert.Same(viewModel.Simulation, viewModel.Editor.Simulation);
        Assert.Same(viewModel.Map, viewModel.Editor.Map);
        Assert.Single(viewModel.Editor.Packets);
        Assert.NotNull(viewModel.Files);
        Assert.False(viewModel.Files.HasImportedReport);
    }

    [Fact]
    public void EditingThroughEditorUpdatesSimulationScenarioAndMap()
    {
        MainViewModel viewModel = Create();
        viewModel.Editor.NodeId = "N7";

        viewModel.Editor.AddNodeCommand.Execute(null);

        Assert.False(viewModel.Editor.HasError);
        Assert.Equal(7, viewModel.Simulation.Scenario.Nodes.Count);
        Assert.True(viewModel.Map.Topology.Nodes.ContainsKey("N7"));
    }

    [Fact]
    public async Task FileOperationsReportTheirStatusThroughMainStatusText()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("opened.json");
        ScenarioFile.Save(path, JammedChain());
        MainViewModel viewModel = Create(dialogs: new StubFileDialogService { OpenScenarioPath = path });

        await viewModel.Files.OpenScenarioAsync();

        Assert.Equal("Сценарій «Jammed chain» відкрито з файлу opened.json.", viewModel.StatusText);
        Assert.Equal(3, viewModel.Map.Topology.Nodes.Count);
        Assert.Equal("Jammed chain", viewModel.Simulation.Scenario.Name);
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
        var logger = new ListLogger();
        var timer = new ManualSimulationTimer();
        var dialogs = new StubFileDialogService();
        var notifier = new RecordingNotifier();

        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!, timer, dialogs, notifier));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(logger, null!, dialogs, notifier));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(logger, timer, null!, notifier));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(logger, timer, dialogs, null!));
    }
}