namespace MeshSim.Desktop.Logging;

/// <summary>
/// Скорочення для запису повідомлень типових рівнів.
/// </summary>
public static class AppLoggerExtensions
{
    public static void LogInfo(this IAppLogger logger, string message)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(LogLevel.Info, message);
    }

    public static void LogWarning(this IAppLogger logger, string message)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(LogLevel.Warning, message);
    }

    public static void LogFailure(this IAppLogger logger, string message, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(LogLevel.Failure, message, exception);
    }
}