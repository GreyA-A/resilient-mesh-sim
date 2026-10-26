using MeshSim.Desktop.Logging;

namespace MeshSim.Tests;

public class AppLoggerExtensionsTests
{
    [Fact]
    public void ExtensionsWriteEntriesWithMatchingLevels()
    {
        var logger = new ListLogger();
        var exception = new InvalidOperationException("boom");

        logger.LogInfo("info");
        logger.LogWarning("warning");
        logger.LogFailure("failure", exception);

        Assert.Equal(LogLevel.Info, logger.Entries[0].Level);
        Assert.Equal(LogLevel.Warning, logger.Entries[1].Level);
        Assert.Equal(LogLevel.Failure, logger.Entries[2].Level);
        Assert.Same(exception, logger.Entries[2].Exception);
        Assert.Null(logger.Entries[0].Exception);
    }

    [Fact]
    public void ExtensionsThrowArgumentNullExceptionForNullLogger()
    {
        IAppLogger logger = null!;

        Assert.Throws<ArgumentNullException>(() => logger.LogInfo("x"));
        Assert.Throws<ArgumentNullException>(() => logger.LogWarning("x"));
        Assert.Throws<ArgumentNullException>(() => logger.LogFailure("x"));
    }
}