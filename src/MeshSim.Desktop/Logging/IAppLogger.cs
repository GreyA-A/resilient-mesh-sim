namespace MeshSim.Desktop.Logging;

/// <summary>
/// Контракт журналу застосунку.
/// </summary>
public interface IAppLogger
{
    void Log(LogLevel level, string message, Exception? exception = null);
}