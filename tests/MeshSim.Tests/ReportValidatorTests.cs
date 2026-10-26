using MeshSim.Engine.IO;
using static MeshSim.Tests.ReportTestData;

namespace MeshSim.Tests;

public class ReportValidatorTests
{
    private static void AssertHasError(IReadOnlyList<string> errors, string fragment)
    {
        Assert.Contains(errors, error => error.Contains(fragment, StringComparison.Ordinal));
    }

    // ---------- Загальне ----------

    [Fact]
    public void ValidateAcceptsConsistentDocument()
    {
        Assert.Empty(ReportValidator.Validate(Document()));
    }

    [Fact]
    public void ValidateAcceptsExportedReportOfRealRun()
    {
        ReportDocument document = ReportExporter.CreateDocument(SampleResult(), "Sample");

        Assert.Empty(ReportValidator.Validate(document));
    }

    [Fact]
    public void ValidateThrowsArgumentNullExceptionForNullDocument()
    {
        Assert.Throws<ArgumentNullException>(() => ReportValidator.Validate(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ValidateRejectsUnsupportedSchemaVersionAndStopsEarly(int version)
    {
        IReadOnlyList<string> errors = ReportValidator.Validate(Document(schemaVersion: version));

        string single = Assert.Single(errors);
        Assert.Contains("версія схеми", single, StringComparison.Ordinal);
    }

    // ---------- Метрики ----------

    [Fact]
    public void ValidateRejectsMissingMetricsSection()
    {
        var document = new ReportDocument { SchemaVersion = 1, Metrics = null! };

        AssertHasError(ReportValidator.Validate(document), "'metrics'");
    }

    [Fact]
    public void ValidateRejectsMissingDropBreakdown()
    {
        var metrics = new ReportMetrics { DropsByReason = null! };

        AssertHasError(ReportValidator.Validate(Document(metrics: metrics)), "dropsByReason");
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("None")]
    [InlineData("3")]
    public void ValidateRejectsUnknownOrNoneDropReasonKey(string key)
    {
        ReportMetrics metrics = Metrics(
            sent: 1,
            delivered: 0,
            dropped: 1,
            ratio: 0.0,
            drops: new Dictionary<string, int> { [key] = 1 });

        AssertHasError(ReportValidator.Validate(Document(metrics: metrics)), "невідому причину");
    }

    [Fact]
    public void ValidateRejectsDeliveredPlusDroppedAboveSent()
    {
        ReportMetrics metrics = Metrics(sent: 1, delivered: 1, dropped: 1, ratio: 1.0);

        AssertHasError(ReportValidator.Validate(Document(metrics: metrics)), "Метрики суперечливі");
    }

    [Fact]
    public void ValidateRejectsDropBreakdownThatDoesNotMatchTotal()
    {
        ReportMetrics metrics = Metrics(sent: 3, delivered: 1, dropped: 2, ratio: 1.0 / 3.0);

        AssertHasError(ReportValidator.Validate(Document(metrics: metrics)), "Метрики суперечливі");
    }

    [Fact]
    public void ValidateRejectsNegativeCounters()
    {
        ReportMetrics metrics = Metrics(sent: -1, delivered: 0, dropped: 0, ratio: 0.0, drops: new Dictionary<string, int>());

        AssertHasError(ReportValidator.Validate(Document(metrics: metrics)), "Метрики суперечливі");
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(double.NaN)]
    public void ValidateRejectsDeliveryRatioThatDoesNotMatchCounters(double ratio)
    {
        AssertHasError(ReportValidator.Validate(Document(metrics: Metrics(ratio: ratio))), "packetDeliveryRatio");
    }

    [Fact]
    public void ValidateAcceptsZeroRatioWhenNothingWasSent()
    {
        ReportMetrics metrics = Metrics(sent: 0, delivered: 0, dropped: 0, hops: 0.0, ratio: 0.0, drops: new Dictionary<string, int>());

        Assert.Empty(ReportValidator.Validate(Document(metrics: metrics)));
    }

    [Fact]
    public void ValidateRejectsNonZeroRatioWhenNothingWasSent()
    {
        ReportMetrics metrics = Metrics(sent: 0, delivered: 0, dropped: 0, hops: 0.0, ratio: 0.4, drops: new Dictionary<string, int>());

        AssertHasError(ReportValidator.Validate(Document(metrics: metrics)), "packetDeliveryRatio");
    }

    // ---------- Події ----------

    [Fact]
    public void ValidateRejectsMissingEventList()
    {
        var document = new ReportDocument { SchemaVersion = 1, Metrics = Metrics(), Events = null! };

        AssertHasError(ReportValidator.Validate(document), "'events'");
    }

    [Fact]
    public void ValidateRejectsNullEventElement()
    {
        ReportEvent[] events = [null!];

        AssertHasError(ReportValidator.Validate(Document(events: events)), "events[0]");
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ValidateRejectsInvalidEventTimestamp(double timestamp)
    {
        AssertHasError(ReportValidator.Validate(Document(events: new[] { EventOf(timestamp) })), "timestampMs");
    }

    [Fact]
    public void ValidateRejectsEventsOutOfChronologicalOrder()
    {
        AssertHasError(ReportValidator.Validate(Document(events: new[] { EventOf(10.0), EventOf(5.0) })), "хронологічний");
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateRejectsUnknownEventType(string? type)
    {
        AssertHasError(ReportValidator.Validate(Document(events: new[] { EventOf(type: type!) })), "eventType");
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData(null)]
    public void ValidateRejectsUnknownDropReason(string? reason)
    {
        AssertHasError(ReportValidator.Validate(Document(events: new[] { EventOf(reason: reason!) })), "dropReason");
    }

    [Fact]
    public void ValidateRejectsDroppedEventWithoutReason()
    {
        ReportEvent dropped = EventOf(type: "PacketDropped", reason: "None");

        AssertHasError(ReportValidator.Validate(Document(events: new[] { dropped })), "PacketDropped");
    }

    [Fact]
    public void ValidateRejectsNonDroppedEventWithReason()
    {
        ReportEvent forwarded = EventOf(type: "PacketForwarded", reason: "TtlExpired");

        AssertHasError(ReportValidator.Validate(Document(events: new[] { forwarded })), "PacketDropped");
    }

    [Fact]
    public void ValidateAcceptsDroppedEventWithReason()
    {
        ReportEvent dropped = EventOf(type: "PacketDropped", reason: "NoRouteFound");

        Assert.Empty(ReportValidator.Validate(Document(events: new[] { dropped })));
    }

    [Theory]
    [InlineData("", "A", "B", "A")]
    [InlineData("p1", "", "B", "A")]
    [InlineData("p1", "A", "   ", "A")]
    [InlineData("p1", "A", "B", null)]
    public void ValidateRejectsBlankIdentifiersInEvents(string packetId, string source, string destination, string? node)
    {
        ReportEvent reportEvent = EventOf(packetId: packetId, source: source, destination: destination, node: node!);

        AssertHasError(ReportValidator.Validate(Document(events: new[] { reportEvent })), "ідентифікатори");
    }
}