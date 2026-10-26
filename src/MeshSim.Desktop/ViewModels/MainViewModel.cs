using MeshSim.Desktop.Logging;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Модель подання головного вікна.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    public const string AppTitle = "ResilientMesh: симулятор стійкості mesh-мереж";
    private string _statusText = "Готово до роботи.";

    public MainViewModel(IAppLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        Title = AppTitle;
        logger.LogInfo("Головне вікно ініціалізовано.");
    }

    public string Title { get; }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }
}