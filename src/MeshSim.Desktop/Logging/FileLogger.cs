using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace MeshSim.Desktop.Logging;

/// <summary>
/// Потокобезпечний файловий журнал із ротацією за датою. Збій запису ніколи не перериває застосунок.
/// </summary>
public sealed class FileLogger : IAppLogger
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _directory;
    private readonly Func<DateTimeOffset> _clock;
    private readonly object _gate = new();

    public FileLogger(string directory, Func<DateTimeOffset>? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        _directory = directory;
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public string DirectoryPath => _directory;

    /// <summary>
    /// Текст останньої помилки запису (null, якщо останній запис вдався).
    /// </summary>
    public string? LastWriteError { get; private set; }

    public static string GetDefaultDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeshSim", "logs");
    }

    public string GetLogFilePath(DateTimeOffset moment)
    {
        return Path.Combine(_directory, string.Concat("meshsim-", moment.ToString("yyyyMMdd", CultureInfo.InvariantCulture), ".log"));
    }

    public static string FormatEntry(DateTimeOffset moment, LogLevel level, string message, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(message);

        string header = string.Concat(moment.ToString("o", CultureInfo.InvariantCulture), " [", LevelName(level), "] ", message);

        return exception is null
            ? header
            : string.Concat(header, Environment.NewLine, exception.ToString());
    }

    /// <summary>
    /// Помилки файлової системи, які журнал має «проковтнути», щоб не завалити застосунок.
    /// </summary>
    public static bool IsExpectedFailure(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Фільтр IsExpectedFailure пропускає лише помилки файлової системи; журнал не має права перервати застосунок.")]
    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        DateTimeOffset moment = _clock();
        string entry = FormatEntry(moment, level, message, exception) + Environment.NewLine;

        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                File.AppendAllText(GetLogFilePath(moment), entry, Utf8NoBom);
                LastWriteError = null;
            }
            catch (Exception failure) when (IsExpectedFailure(failure))
            {
                LastWriteError = failure.Message;
            }
        }
    }

    private static string LevelName(LogLevel level)
    {
        return level switch
        {
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Failure => "ERROR",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Невідомий рівень журналу.")
        };
    }
}