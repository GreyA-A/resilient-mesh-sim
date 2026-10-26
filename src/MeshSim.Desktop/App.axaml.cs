using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MeshSim.Desktop.Views;
using MeshSim.Desktop.ViewModels;
using MeshSim.Desktop.Logging;

namespace MeshSim.Desktop;

[ExcludeFromCodeCoverage(Justification = "Кореневий клас Avalonia: композиція вікна та тема, без власної логіки.")]
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = new MainViewModel(new FileLogger(FileLogger.GetDefaultDirectory())) };
        }

        base.OnFrameworkInitializationCompleted();
    }
}