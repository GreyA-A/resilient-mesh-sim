using MeshSim.Engine;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

internal static class ReportTestData
{
    /// <summary>
    /// Результат прогону: p1 доставлено за 2 стрибки (A-B-C), p2 втрачено (вузол D ізольований).
    /// Журнал: 0 Enq p1, 0 Enq p2, 0 Drop p2, 10 Fwd p1, 20 Del p1.
    /// </summary>
    public static SimulationResult SampleResult()
    {
        ScenarioDefinition scenario = Scenario(
            nodes: new[] { Node("A"), Node("B", x: 80.0), Node("C", x: 160.0), Node("D", x: 1000.0) },
            packets: new[] { Packet("p1", "A", "C"), Packet("p2", "A", "D") });

        return ScenarioFactory.Build(scenario).Simulation.RunToCompletion();
    }

    public static ReportEvent EventOf(
        double timestampMs = 0.0,
        string type = "PacketEnqueued",
        string reason = "None",
        string packetId = "p1",
        string source = "A",
        string destination = "B",
        string node = "A")
        => new()
        {
            TimestampMs = timestampMs,
            EventType = type,
            DropReason = reason,
            PacketId = packetId,
            SourceNodeId = source,
            DestinationNodeId = destination,
            CurrentNodeId = node
        };

    public static ReportMetrics Metrics(
        int sent = 2,
        int delivered = 1,
        int dropped = 1,
        double hops = 2.0,
        double ratio = 0.5,
        IReadOnlyDictionary<string, int>? drops = null)
        => new()
        {
            TotalPacketsSent = sent,
            TotalPacketsDelivered = delivered,
            TotalPacketsDropped = dropped,
            AverageHopCount = hops,
            PacketDeliveryRatio = ratio,
            DropsByReason = drops ?? new Dictionary<string, int> { ["NoRouteFound"] = 1 }
        };

    public static ReportDocument Document(
        ReportMetrics? metrics = null,
        IReadOnlyList<ReportEvent>? events = null,
        int schemaVersion = ReportDocument.CurrentSchemaVersion)
        => new()
        {
            SchemaVersion = schemaVersion,
            ScenarioName = "Test",
            Metrics = metrics ?? Metrics(),
            Events = events ?? new[] { EventOf(0.0), EventOf(10.0, "PacketDelivered", node: "B") }
        };
}