using System.Diagnostics.CodeAnalysis;
using Avalonia;
using MeshSim.Desktop.Logging;

namespace MeshSim.Desktop;

[ExcludeFromCodeCoverage(Justification = "Точка входу та ініціалізація Avalonia: платформний код без власної логіки.")]
public static class Program
{
    [STAThread]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Найвищий рівень: фатальну помилку потрібно записати в журнал до завершення процесу.")]
    public static int Main(string[] args)
    {
        IAppLogger logger = AppServices.Logger;
        logger.LogInfo("Застосунок запускається.");

        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            logger.LogFailure("Фатальна помилка застосунку.", exception);
            return 1;
        }
        finally
        {
            logger.LogInfo("Застосунок завершено.");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UsePlatformDetect();
    }
}