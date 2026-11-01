using System.Globalization;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ReportTestData;

namespace MeshSim.Tests;

public class ReportSummaryFormatterTests
{
    [Fact]
    public void DescribeListsMetricsAndTranslatedDropReasons()
    {
        ReportDocument report = ReportExporter.CreateDocument(SampleResult(), "Sample");

        string text = ReportSummaryFormatter.Describe(report);

        string[] expectedLines =
        [
            "Звіт про прогін сценарію «Sample»",
            "Надіслано пакетів: 2",
            "Доставлено: 1",
            "Втрачено: 1",
            "PDR: 50.0 %",
            "Середня кількість стрибків: 2.00",
            "Подій у журналі: 5",
            "  • немає маршруту: 1"
        ];
        Assert.Equal(string.Join(Environment.NewLine, expectedLines), text);
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("99")]
    public void DescribeKeepsRawKeyForUnknownDropReasons(string key)
    {
        ReportMetrics metrics = Metrics(drops: new Dictionary<string, int> { [key] = 3 });

        string text = ReportSummaryFormatter.Describe(Document(metrics: metrics));

        Assert.EndsWith("  • " + key + ": 3", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeOmitsReasonLinesWhenNothingWasDropped()
    {
        ReportMetrics metrics = Metrics(sent: 1, delivered: 1, dropped: 0, ratio: 1.0, drops: new Dictionary<string, int>());

        string text = ReportSummaryFormatter.Describe(Document(metrics: metrics));

        Assert.DoesNotContain("•", text, StringComparison.Ordinal);
        Assert.Contains("PDR: 100.0 %", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeUsesInvariantCulture()
    {
        ReportDocument report = ReportExporter.CreateDocument(SampleResult(), "Sample");
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Contains("PDR: 50.0 %", ReportSummaryFormatter.Describe(report), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void DescribeThrowsArgumentNullExceptionForNull()
    {
        Assert.Throws<ArgumentNullException>(() => ReportSummaryFormatter.Describe(null!));
    }
}