using MeshSim.Desktop.Logging;

namespace MeshSim.Tests;

internal sealed class ListLogger : IAppLogger
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        Entries.Add((level, message, exception));
    }
}