using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MeshSim.Desktop.Services;
using MeshSim.Desktop.ViewModels;
using MeshSim.Desktop.Views;

namespace MeshSim.Desktop;

[ExcludeFromCodeCoverage(Justification = "Кореневий клас Avalonia: композиція вікна та реєстрація обробників, без власної логіки.")]
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
            var handler = new GlobalExceptionHandler(AppServices.Logger, new ErrorDialogNotifier());
            RegisterGlobalHandlers(handler);

            desktop.MainWindow = new MainWindow { DataContext = new MainViewModel(AppServices.Logger, new AvaloniaSimulationTimer()) };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterGlobalHandlers(GlobalExceptionHandler handler)
    {
        AppDomain.CurrentDomain.UnhandledException +=
            (_, args) => handler.HandleUnknown(args.ExceptionObject, "AppDomain.UnhandledException");

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            handler.Handle(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
        };

        Dispatcher.UIThread.UnhandledException += (_, args) =>
        {
            handler.Handle(args.Exception, "Dispatcher.UnhandledException");
            args.Handled = true;
        };
    }
}