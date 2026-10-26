using MeshSim.Engine;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ReportTestData;

namespace MeshSim.Tests;

public class ReportExporterTests
{
    [Fact]
    public void CreateDocumentMapsMetricsAndEvents()
    {
        SimulationResult result = SampleResult();

        ReportDocument document = ReportExporter.CreateDocument(result, "Sample");

        Assert.Equal(ReportDocument.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Equal("Sample", document.ScenarioName);
        Assert.Equal(2, document.Metrics.TotalPacketsSent);
        Assert.Equal(1, document.Metrics.TotalPacketsDelivered);
        Assert.Equal(1, document.Metrics.TotalPacketsDropped);
        Assert.Equal(2.0, document.Metrics.AverageHopCount);
        Assert.Equal(0.5, document.Metrics.PacketDeliveryRatio);
        Assert.Equal(1, document.Metrics.DropsByReason["NoRouteFound"]);
        Assert.Equal(result.EventLog.Count, document.Events.Count);

        ReportEvent first = document.Events[0];
        Assert.Equal(0.0, first.TimestampMs);
        Assert.Equal("PacketEnqueued", first.EventType);
        Assert.Equal("p1", first.PacketId);
        Assert.Equal("A", first.SourceNodeId);
        Assert.Equal("C", first.DestinationNodeId);
        Assert.Equal("A", first.CurrentNodeId);
        Assert.Equal("None", first.DropReason);

        Assert.Contains(document.Events, e => e.EventType == "PacketDropped" && e.DropReason == "NoRouteFound");
    }

    [Fact]
    public void CreateDocumentOrdersDropReasonsByEnumValue()
    {
        var drops = new Dictionary<PacketDropReason, int>
        {
            [PacketDropReason.TtlExpired] = 1,
            [PacketDropReason.NoRouteFound] = 2
        };
        var result = new SimulationResult(3, 0, 3, 0.0, drops, Array.Empty<SimulationEvent>());

        ReportDocument document = ReportExporter.CreateDocument(result, "Order");

        string[] expectedKeys = ["NoRouteFound", "TtlExpired"];
        Assert.Equal(expectedKeys, document.Metrics.DropsByReason.Keys);
    }

    [Fact]
    public void ToJsonProducesCamelCaseDocumentThatPassesValidation()
    {
        string json = ReportExporter.ToJson(SampleResult(), "Sample");

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"packetDeliveryRatio\": 0.5", json);
        Assert.Contains("\"NoRouteFound\": 1", json);
        Assert.Equal(2, ReportImporter.Parse(json).Metrics.TotalPacketsSent);
    }

    [Fact]
    public void SaveJsonWritesReportAndMatchingChecksumSidecar()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("report.json");

        ReportExporter.SaveJson(path, SampleResult(), "Sample");

        byte[] bytes = File.ReadAllBytes(path);
        string sidecar = File.ReadAllText(ReportChecksum.GetSidecarPath(path));
        Assert.Equal(ReportChecksum.Compute(bytes), sidecar.Trim());
        Assert.Empty(Directory.GetFiles(temp.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public void ExportMethodsValidateArguments()
    {
        SimulationResult result = SampleResult();

        Assert.Throws<ArgumentNullException>(() => ReportExporter.CreateDocument(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => ReportExporter.CreateDocument(result, null!));
        Assert.ThrowsAny<ArgumentException>(() => ReportExporter.SaveJson("  ", result, "x"));
    }
}