using MeshSim.Desktop.Logging;
using MeshSim.Desktop.Services;

namespace MeshSim.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public void HandleLogsFailureWithSourceAndNotifiesUser()
    {
        var logger = new ListLogger();
        var notifier = new RecordingNotifier();
        var handler = new GlobalExceptionHandler(logger, notifier);
        var exception = new InvalidOperationException("boom");

        handler.Handle(exception, "UI");

        (LogLevel level, string message, Exception? logged) = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Failure, level);
        Assert.Contains("UI", message, StringComparison.Ordinal);
        Assert.Same(exception, logged);

        (string title, string userMessage) = Assert.Single(notifier.Messages);
        Assert.Equal(GlobalExceptionHandler.ErrorTitle, title);
        Assert.Contains("boom", userMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void HandleIncrementsHandledCount()
    {
        var handler = new GlobalExceptionHandler(new ListLogger(), new RecordingNotifier());

        handler.Handle(new InvalidOperationException("a"), "UI");
        handler.Handle(new InvalidOperationException("b"), "Task");

        Assert.Equal(2, handler.HandledCount);
    }

    [Fact]
    public void HandleSurvivesNotifierFailureAndLogsIt()
    {
        var logger = new ListLogger();
        var handler = new GlobalExceptionHandler(logger, new ThrowingNotifier());

        Exception? thrown = Record.Exception(() => handler.Handle(new InvalidOperationException("boom"), "UI"));

        Assert.Null(thrown);
        Assert.Equal(2, logger.Entries.Count);
        Assert.Contains("Не вдалося показати", logger.Entries[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HandleUnknownPassesThroughRealException()
    {
        var logger = new ListLogger();
        var handler = new GlobalExceptionHandler(logger, new RecordingNotifier());
        var exception = new InvalidOperationException("real");

        handler.HandleUnknown(exception, "AppDomain");

        Assert.Same(exception, logger.Entries[0].Exception);
    }

    [Theory]
    [InlineData("just a string")]
    [InlineData(null)]
    public void HandleUnknownWrapsNonExceptionObjects(object? exceptionObject)
    {
        var logger = new ListLogger();
        var handler = new GlobalExceptionHandler(logger, new RecordingNotifier());

        handler.HandleUnknown(exceptionObject, "AppDomain");

        Exception? logged = logger.Entries[0].Exception;
        Assert.IsType<InvalidOperationException>(logged);
        Assert.Contains("не є винятком", logged.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUserMessageContainsExceptionMessageAndLogHint()
    {
        string message = GlobalExceptionHandler.BuildUserMessage(new InvalidOperationException("boom"));

        Assert.Contains("boom", message, StringComparison.Ordinal);
        Assert.Contains("журналу", message, StringComparison.Ordinal);
    }

    [Fact]
    public void MethodsValidateArguments()
    {
        var handler = new GlobalExceptionHandler(new ListLogger(), new RecordingNotifier());

        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(null!, new RecordingNotifier()));
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(new ListLogger(), null!));
        Assert.Throws<ArgumentNullException>(() => handler.Handle(null!, "UI"));
        Assert.ThrowsAny<ArgumentException>(() => handler.Handle(new InvalidOperationException("x"), "  "));
        Assert.Throws<ArgumentNullException>(() => GlobalExceptionHandler.BuildUserMessage(null!));
    }
}