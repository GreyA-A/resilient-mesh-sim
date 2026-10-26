using System.Diagnostics.CodeAnalysis;
using Avalonia;

namespace MeshSim.Desktop;

[ExcludeFromCodeCoverage(Justification = "Точка входу та ініціалізація Avalonia: платформний код без власної логіки.")]
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UsePlatformDetect();
    }
}