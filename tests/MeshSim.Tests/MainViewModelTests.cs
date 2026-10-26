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