using System.Globalization;
using MeshSim.Core;
using MeshSim.Engine;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ReportTestData;

namespace MeshSim.Tests;

public class CsvReportExporterTests
{
    private static SimulationResult ResultWithEvent(SimulationEvent logged)
    {
        return new SimulationResult(1, 0, 0, 0.0, new Dictionary<PacketDropReason, int>(), new[] { logged });
    }

    [Fact]
    public void ToCsvWritesHeaderAndOneRowPerEvent()
    {
        SimulationResult result = SampleResult();

        string[] lines = CsvReportExporter.ToCsv(result).Split("\r\n");

        Assert.Equal("timestampMs,eventType,packetId,sourceNodeId,destinationNodeId,currentNodeId,dropReason", lines[0]);
        // заголовок + рядки подій + порожній елемент після останнього розриву рядка
        Assert.Equal(result.EventLog.Count + 2, lines.Length);
        Assert.Equal("0,PacketEnqueued,p1,A,C,A,None", lines[1]);
        Assert.Contains("0,PacketDropped,p2,A,D,A,NoRouteFound", lines);
        Assert.Contains("20,PacketDelivered,p1,A,C,C,None", lines);
    }

    [Fact]
    public void ToCsvUsesInvariantCultureForNumbers()
    {
        var packet = new NetworkPacket("p1", "A", "B", 3, 10);
        SimulationResult result = ResultWithEvent(new SimulationEvent(12.5, SimulationEventType.PacketEnqueued, packet, "A"));
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            string csv = CsvReportExporter.ToCsv(result);

            Assert.Contains("12.5,PacketEnqueued,p1,A,B,A,None", csv);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ToCsvNeutralizesFormulaInPacketIdentifier()
    {
        var packet = new NetworkPacket("=cmd", "A", "B", 3, 10);
        SimulationResult result = ResultWithEvent(new SimulationEvent(0.0, SimulationEventType.PacketEnqueued, packet, "A"));

        string csv = CsvReportExporter.ToCsv(result);

        Assert.Contains("0,PacketEnqueued,'=cmd,A,B,A,None", csv);
    }

    [Fact]
    public void SaveCsvWritesFileAndMatchingChecksumSidecar()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("events.csv");
        SimulationResult result = SampleResult();

        CsvReportExporter.SaveCsv(path, result);

        Assert.Equal(CsvReportExporter.ToCsv(result), File.ReadAllText(path));
        string sidecar = File.ReadAllText(ReportChecksum.GetSidecarPath(path));
        Assert.Equal(ReportChecksum.Compute(File.ReadAllBytes(path)), sidecar.Trim());
    }

    [Fact]
    public void ExportMethodsValidateArguments()
    {
        Assert.Throws<ArgumentNullException>(() => CsvReportExporter.ToCsv(null!));
        Assert.ThrowsAny<ArgumentException>(() => CsvReportExporter.SaveCsv("  ", SampleResult()));
    }
}