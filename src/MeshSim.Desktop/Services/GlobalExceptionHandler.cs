using System.Diagnostics.CodeAnalysis;
using MeshSim.Desktop.Logging;

namespace MeshSim.Desktop.Services;

/// <summary>
/// Єдина точка обробки необроблених винятків: запис у журнал і повідомлення користувачу.
/// Не кидає власних винятків, щоб не спричинити каскадний збій.
/// </summary>
public sealed class GlobalExceptionHandler
{
    public const string ErrorTitle = "Неочікувана помилка";

    private readonly IAppLogger _logger;
    private readonly IUserNotifier _notifier;
    private int _handledCount;

    public GlobalExceptionHandler(IAppLogger logger, IUserNotifier notifier)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(notifier);

        _logger = logger;
        _notifier = notifier;
    }

    /// <summary>
    /// Кількість винятків, оброблених цим екземпляром.
    /// </summary>
    public int HandledCount => Volatile.Read(ref _handledCount);

    public static string BuildUserMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Concat(
            "Під час роботи застосунку сталася неочікувана помилка:",
            Environment.NewLine,
            exception.Message,
            Environment.NewLine,
            Environment.NewLine,
            "Подробиці записано до журналу.");
    }

    public void Handle(Exception exception, string source)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        Interlocked.Increment(ref _handledCount);
        _logger.LogFailure(string.Concat("Необроблений виняток (", source, ")."), exception);

        NotifyUser(exception);
    }

    /// <summary>
    /// Обробляє довільний об'єкт винятку (AppDomain може передати не Exception).
    /// </summary>
    public void HandleUnknown(object? exceptionObject, string source)
    {
        Exception exception = exceptionObject as Exception
            ?? new InvalidOperationException($"Отримано об'єкт, що не є винятком: {exceptionObject}");

        Handle(exception, source);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Глобальний обробник не має права кидати винятки: збій діалогу лише записується в журнал.")]
    private void NotifyUser(Exception exception)
    {
        try
        {
            _notifier.ShowError(ErrorTitle, BuildUserMessage(exception));
        }
        catch (Exception notifierFailure)
        {
            _logger.LogFailure("Не вдалося показати повідомлення про помилку.", notifierFailure);
        }
    }
}