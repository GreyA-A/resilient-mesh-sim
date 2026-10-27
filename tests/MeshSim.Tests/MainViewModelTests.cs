using CommunityToolkit.Mvvm.ComponentModel;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.ViewModels;

namespace MeshSim.Tests;

public class MainViewModelTests
{
    [Fact]
    public void ConstructorSetsTitleAndLogsInitialization()
    {
        var logger = new ListLogger();

        var viewModel = new MainViewModel(logger);

        Assert.Equal(MainViewModel.AppTitle, viewModel.Title);
        Assert.IsAssignableFrom<ObservableObject>(viewModel);
        (LogLevel level, string message, Exception? exception) = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Info, level);
        Assert.Contains("ініціалізовано", message, StringComparison.Ordinal);
        Assert.Null(exception);
    }

    [Fact]
    public void ConstructorLoadsDemoScenarioIntoMap()
    {
        var viewModel = new MainViewModel(new ListLogger());

        Assert.Equal(6, viewModel.Map.Topology.Nodes.Count);
        Assert.Equal(2, viewModel.Map.Topology.JammingZones.Count);
    }

    [Fact]
    public void StatusTextDescribesLoadedScenario()
    {
        var viewModel = new MainViewModel(new ListLogger());

        Assert.Contains("Демонстраційний сценарій", viewModel.StatusText, StringComparison.Ordinal);
        Assert.Contains("вузлів — 6", viewModel.StatusText, StringComparison.Ordinal);
        Assert.Contains("зон завад — 2", viewModel.StatusText, StringComparison.Ordinal);
        Assert.Contains("пакетів — 1", viewModel.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void MapReceivesSceneAfterResize()
    {
        var viewModel = new MainViewModel(new ListLogger());

        viewModel.Map.Resize(900.0, 600.0);

        Assert.Equal(6, viewModel.Map.Scene.Nodes.Count);
        Assert.Equal(2, viewModel.Map.Scene.Zones.Count);
    }

    [Fact]
    public void StatusTextRaisesPropertyChangedWhenValueChanges()
    {
        var viewModel = new MainViewModel(new ListLogger());

        Assert.PropertyChanged(viewModel, nameof(MainViewModel.StatusText), () => viewModel.StatusText = "Нова подія");

        Assert.Equal("Нова подія", viewModel.StatusText);
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionForNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!));
    }
}