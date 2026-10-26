using MeshSim.Desktop.Logging;

namespace MeshSim.Tests;

public class FileLoggerTests
{
    private static readonly DateTimeOffset FixedMoment = new(2026, 10, 26, 12, 30, 45, 123, TimeSpan.FromHours(2));

    private static FileLogger CreateLogger(string directory)
    {
        return new FileLogger(directory, () => FixedMoment);
    }

    [Fact]
    public void LogCreatesDirectoryAndWritesFormattedEntry()
    {
        using var temp = new TempDirectory();
        string logs = temp.FilePath("nested-logs");
        FileLogger logger = CreateLogger(logs);

        logger.Log(LogLevel.Info, "Hello");

        string expected = "2026-10-26T12:30:45.1230000+02:00 [INFO] Hello" + Environment.NewLine;
        Assert.Equal(expected, File.ReadAllText(logger.GetLogFilePath(FixedMoment)));
        Assert.Equal(logs, logger.DirectoryPath);
        Assert.Null(logger.LastWriteError);
    }

    [Fact]
    public void GetLogFilePathContainsDateOfMoment()
    {
        FileLogger logger = CreateLogger("logs");

        Assert.EndsWith("meshsim-20261026.log", logger.GetLogFilePath(FixedMoment), StringComparison.Ordinal);
    }

    [Fact]
    public void LogAppendsEntriesAndWritesExceptionDetails()
    {
        using var temp = new TempDirectory();
        FileLogger logger = CreateLogger(temp.DirectoryPath);

        logger.Log(LogLevel.Warning, "first");
        logger.Log(LogLevel.Failure, "second", new InvalidOperationException("boom"));

        string[] lines = File.ReadAllLines(logger.GetLogFilePath(FixedMoment));
        Assert.Contains("[WARN] first", lines[0], StringComparison.Ordinal);
        Assert.Contains("[ERROR] second", lines[1], StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException: boom", lines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void LogRotatesToNewFileWhenDateChanges()
    {
        using var temp = new TempDirectory();
        var moments = new Queue<DateTimeOffset>([FixedMoment, FixedMoment.AddDays(1)]);
        var logger = new FileLogger(temp.DirectoryPath, () => moments.Dequeue());

        logger.Log(LogLevel.Info, "day one");
        logger.Log(LogLevel.Info, "day two");

        Assert.Equal(2, Directory.GetFiles(temp.DirectoryPath, "meshsim-*.log").Length);
    }

    [Fact]
    public void LogUsesSystemClockWhenNoClockIsProvided()
    {
        using var temp = new TempDirectory();
        var logger = new FileLogger(temp.DirectoryPath);

        logger.Log(LogLevel.Info, "now");

        Assert.Single(Directory.GetFiles(temp.DirectoryPath, "meshsim-*.log"));
    }

    [Fact]
    public void LogDoesNotThrowWhenDirectoryPathIsOccupiedByFile()
    {
        using var temp = new TempDirectory();
        string occupied = temp.FilePath("occupied");
        File.WriteAllText(occupied, "not a directory");
        FileLogger logger = CreateLogger(occupied);

        Exception? thrown = Record.Exception(() => logger.Log(LogLevel.Info, "x"));

        Assert.Null(thrown);
        Assert.NotNull(logger.LastWriteError);
    }

    [Fact]
    public void LogDoesNotThrowWhenLogFilePathIsOccupiedByDirectory()
    {
        using var temp = new TempDirectory();
        FileLogger logger = CreateLogger(temp.FilePath("logs"));
        Directory.CreateDirectory(logger.GetLogFilePath(FixedMoment));

        Exception? thrown = Record.Exception(() => logger.Log(LogLevel.Info, "x"));

        Assert.Null(thrown);
        Assert.NotNull(logger.LastWriteError);
    }

    [Fact]
    public void LastWriteErrorIsClearedAfterSuccessfulWrite()
    {
        using var temp = new TempDirectory();
        var moments = new Queue<DateTimeOffset>([FixedMoment, FixedMoment.AddDays(1)]);
        var logger = new FileLogger(temp.DirectoryPath, () => moments.Dequeue());
        Directory.CreateDirectory(logger.GetLogFilePath(FixedMoment));

        logger.Log(LogLevel.Info, "fails");
        Assert.NotNull(logger.LastWriteError);

        logger.Log(LogLevel.Info, "succeeds");
        Assert.Null(logger.LastWriteError);
    }

    [Fact]
    public void LogIsThreadSafe()
    {
        using var temp = new TempDirectory();
        FileLogger logger = CreateLogger(temp.DirectoryPath);

        Parallel.For(0, 100, index => logger.Log(LogLevel.Info, "entry"));

        Assert.Equal(100, File.ReadAllLines(logger.GetLogFilePath(FixedMoment)).Length);
    }

    [Theory]
    [InlineData(LogLevel.Info, "INFO")]
    [InlineData(LogLevel.Warning, "WARN")]
    [InlineData(LogLevel.Failure, "ERROR")]
    public void FormatEntryUsesLevelName(LogLevel level, string expectedName)
    {
        string entry = FileLogger.FormatEntry(FixedMoment, level, "msg", null);

        Assert.Equal("2026-10-26T12:30:45.1230000+02:00 [" + expectedName + "] msg", entry);
    }

    [Fact]
    public void FormatEntryThrowsForUnknownLevel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FileLogger.FormatEntry(FixedMoment, (LogLevel)99, "msg", null));
    }

    [Theory]
    [InlineData(typeof(IOException), true)]
    [InlineData(typeof(UnauthorizedAccessException), true)]
    [InlineData(typeof(ArgumentException), false)]
    public void IsExpectedFailureAcceptsOnlyFileSystemErrors(Type exceptionType, bool expected)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Equal(expected, FileLogger.IsExpectedFailure(exception));
    }

    [Fact]
    public void GetDefaultDirectoryEndsWithMeshSimLogs()
    {
        string expectedSuffix = Path.Combine("MeshSim", "logs");

        Assert.EndsWith(expectedSuffix, FileLogger.GetDefaultDirectory(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorAndLogValidateArguments()
    {
        Assert.ThrowsAny<ArgumentException>(() => new FileLogger("   "));
        Assert.ThrowsAny<ArgumentException>(() => new FileLogger(null!));

        var logger = new FileLogger("logs");
        Assert.Throws<ArgumentNullException>(() => logger.Log(LogLevel.Info, null!));
        Assert.Throws<ArgumentNullException>(() => FileLogger.FormatEntry(FixedMoment, LogLevel.Info, null!, null));
    }
}