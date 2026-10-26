using MeshSim.Engine.IO;
using static MeshSim.Tests.ReportTestData;

namespace MeshSim.Tests;

public class ReportImporterTests
{
    [Fact]
    public void LoadRoundTripsExportedReport()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("report.json");
        ReportExporter.SaveJson(path, SampleResult(), "Sample");

        ReportDocument document = ReportImporter.Load(path);

        Assert.Equal("Sample", document.ScenarioName);
        Assert.Equal(2, document.Metrics.TotalPacketsSent);
        Assert.Equal(0.5, document.Metrics.PacketDeliveryRatio);
        Assert.Equal(1, document.Metrics.DropsByReason["NoRouteFound"]);
        Assert.Equal(5, document.Events.Count);
        Assert.Equal("PacketDelivered", document.Events[^1].EventType);
    }

    [Fact]
    public void LoadRejectsReportModifiedAfterExport()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("report.json");
        ReportExporter.SaveJson(path, SampleResult(), "Sample");
        string tampered = File.ReadAllText(path).Replace("\"totalPacketsDelivered\": 1", "\"totalPacketsDelivered\": 2", StringComparison.Ordinal);
        File.WriteAllText(path, tampered);

        var exception = Assert.Throws<InvalidDataException>(() => ReportImporter.Load(path));

        Assert.Contains("Контрольна сума", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadRejectsReportWithoutChecksumFile()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("report.json");
        ReportExporter.SaveJson(path, SampleResult(), "Sample");
        File.Delete(ReportChecksum.GetSidecarPath(path));

        var exception = Assert.Throws<InvalidDataException>(() => ReportImporter.Load(path));

        Assert.Contains("контрольної суми", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadRejectsReportWithWrongChecksum()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("report.json");
        ReportExporter.SaveJson(path, SampleResult(), "Sample");
        File.WriteAllText(ReportChecksum.GetSidecarPath(path), new string('0', 64));

        Assert.Throws<InvalidDataException>(() => ReportImporter.Load(path));
    }

    [Fact]
    public void LoadRejectsSemanticallyInvalidReportEvenWhenChecksumMatches()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("report.json");
        // Файл і сума узгоджені, але версія схеми непідтримувана.
        byte[] rawBytes = System.Text.Encoding.UTF8.GetBytes("{\"schemaVersion\":7}");
        AtomicFileWriter.WriteAllBytes(path, rawBytes);
        AtomicFileWriter.WriteAllText(ReportChecksum.GetSidecarPath(path), ReportChecksum.Compute(rawBytes));

        var exception = Assert.Throws<InvalidDataException>(() => ReportImporter.Load(path));

        Assert.Contains("версія схеми", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadThrowsFileNotFoundExceptionForMissingFile()
    {
        using var temp = new TempDirectory();

        Assert.Throws<FileNotFoundException>(() => ReportImporter.Load(temp.FilePath("missing.json")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"schemaVersion\":1,\"unexpected\":true}")]
    [InlineData("{\"schemaVersion\":1,\"metrics\":{\"totalPacketsSent\":\"many\"}}")]
    public void ParseThrowsInvalidDataExceptionForBrokenOrUnexpectedContent(string json)
    {
        Assert.Throws<InvalidDataException>(() => ReportImporter.Parse(json));
    }

    [Fact]
    public void ParseAcceptsValidReportText()
    {
        string json = ReportExporter.ToJson(SampleResult(), "Sample");

        ReportDocument document = ReportImporter.Parse(json);

        Assert.Equal(2, document.Metrics.TotalPacketsSent);
    }

    [Fact]
    public void ImportMethodsValidateArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ReportImporter.Parse(null!));
        Assert.ThrowsAny<ArgumentException>(() => ReportImporter.Load("   "));
    }
}