using MeshSim.Core;
using MeshSim.Engine;
using Xunit;

namespace MeshSim.Tests;

public partial class SimulationEngineTests
{
    // ================= Покроковий API: Step / HasPendingEvents / GetSnapshot =================

    private static SimulationEvent StepOrFail(SimulationEngine engine)
    {
        SimulationEvent? stepped = engine.Step();
        Assert.True(stepped.HasValue, "Очікувалась подія, але черга порожня.");
        return stepped.GetValueOrDefault();
    }

    private static NetworkTopology MixedTopology()
    {
        NetworkTopology topology = Chain();
        topology.AddNode(Node("D", 1000, 0, range: 100.0));
        return topology;
    }

    private static void ScheduleMixedTraffic(SimulationEngine engine)
    {
        engine.SchedulePacket(Packet("two-hop", "A", "C"));
        engine.SchedulePacket(Packet("one-hop", "A", "B"), 3.0);
        engine.SchedulePacket(Packet("lost", "A", "D"), 7.0);
    }

    private static (double Time, SimulationEventType Type, string PacketId, string NodeId, PacketDropReason Reason)[] Describe(SimulationResult result)
        => result.EventLog
        .Select(e => (e.TimestampMs, e.EventType, e.Packet.Id, e.CurrentNodeId, e.DropReason))
        .ToArray();

    [Fact]
    public void HasPendingEventsTracksSchedulingAndDraining()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        Assert.False(engine.HasPendingEvents);

        engine.SchedulePacket(Packet("p1", "A", "B"));
        Assert.True(engine.HasPendingEvents);

        engine.RunToCompletion();
        Assert.False(engine.HasPendingEvents);
    }

    [Fact]
    public void StepOnEmptyQueueReturnsNullAndKeepsStateUntouched()
    {
        var engine = new SimulationEngine(new NetworkTopology(), new DijkstraRoutingStrategy());

        Assert.Null(engine.Step());

        Assert.Equal(0.0, engine.CurrentTimeMs);
        Assert.False(engine.HasPendingEvents);
        Assert.Empty(engine.GetSnapshot().EventLog);
    }

    [Fact]
    public void StepProcessesExactlyOneEventAndLeavesTheRestPending()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"));

        SimulationEvent first = StepOrFail(engine);

        AssertEvent(first, 0.0, SimulationEventType.PacketEnqueued, "p1", "A");
        Assert.True(engine.HasPendingEvents);
        Assert.Equal(0.0, engine.CurrentTimeMs);
        Assert.Single(engine.GetSnapshot().EventLog);
    }

    [Fact]
    public void StepAdvancesCurrentTimeToEachProcessedEventTimestamp()
    {
        var engine = new SimulationEngine(Chain(), new DijkstraRoutingStrategy(), hopDelayMs: 10.0);
        engine.SchedulePacket(Packet("p1", "A", "C"));

        SimulationEvent first = StepOrFail(engine);
        Assert.Equal(0.0, engine.CurrentTimeMs);
        AssertEvent(first, 0.0, SimulationEventType.PacketEnqueued, "p1", "A");

        SimulationEvent second = StepOrFail(engine);
        Assert.Equal(10.0, engine.CurrentTimeMs);
        AssertEvent(second, 10.0, SimulationEventType.PacketForwarded, "p1", "B");

        SimulationEvent third = StepOrFail(engine);
        Assert.Equal(20.0, engine.CurrentTimeMs);
        AssertEvent(third, 20.0, SimulationEventType.PacketDelivered, "p1", "C");

        Assert.False(engine.HasPendingEvents);
        Assert.Null(engine.Step());
    }

    [Fact]
    public void SteppingToTheEndProducesSameResultAsRunToCompletion()
    {
        NetworkTopology steppedTopology = MixedTopology();
        NetworkTopology batchTopology = MixedTopology();
        var stepped = new SimulationEngine(steppedTopology, new DijkstraRoutingStrategy());
        var batch = new SimulationEngine(batchTopology, new DijkstraRoutingStrategy());
        ScheduleMixedTraffic(stepped);
        ScheduleMixedTraffic(batch);

        while (stepped.HasPendingEvents)
        {
            stepped.Step();
        }

        SimulationResult steppedResult = stepped.GetSnapshot();
        SimulationResult batchResult = batch.RunToCompletion();

        Assert.Equal(Describe(batchResult), Describe(steppedResult));
        Assert.Equal(batchResult.TotalPacketsSent, steppedResult.TotalPacketsSent);
        Assert.Equal(batchResult.TotalPacketsDelivered, steppedResult.TotalPacketsDelivered);
        Assert.Equal(batchResult.TotalPacketsDropped, steppedResult.TotalPacketsDropped);
        Assert.Equal(batchResult.AverageHopCount, steppedResult.AverageHopCount);
        Assert.Equal(batchResult.DropsByReason, steppedResult.DropsByReason);
        Assert.Equal(batch.CurrentTimeMs, stepped.CurrentTimeMs);
        Assert.Equal(batchTopology.Nodes["A"].BatteryLevelPercent, steppedTopology.Nodes["A"].BatteryLevelPercent);
    }

    [Fact]
    public void DeliveryIsCountedWhenFinalHopIsTransmittedNotWhenDeliveredEventIsLogged()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"));

        StepOrFail(engine);
        SimulationResult afterFirstStep = engine.GetSnapshot();

        // Фіксує поточну семантику: лічильник зріс, а подія PacketDelivered ще не в журналі.
        Assert.Equal(1, afterFirstStep.TotalPacketsDelivered);
        Assert.Single(afterFirstStep.EventLog);

        StepOrFail(engine);
        SimulationResult afterSecondStep = engine.GetSnapshot();

        Assert.Equal(1, afterSecondStep.TotalPacketsDelivered);
        Assert.Equal(2, afterSecondStep.EventLog.Count);
        Assert.Equal(SimulationEventType.PacketDelivered, afterSecondStep.EventLog[1].EventType);
    }

    [Fact]
    public void GetSnapshotOnNewEngineReturnsZeroMetrics()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());

        SimulationResult snapshot = engine.GetSnapshot();

        Assert.Equal(0, snapshot.TotalPacketsSent);
        Assert.Equal(0, snapshot.TotalPacketsDelivered);
        Assert.Equal(0, snapshot.TotalPacketsDropped);
        Assert.Equal(0.0, snapshot.AverageHopCount);
        Assert.Empty(snapshot.DropsByReason);
        Assert.Empty(snapshot.EventLog);
    }

    [Fact]
    public void GetSnapshotTakenMidRunIsNotAffectedByLaterSteps()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "GHOST"));

        StepOrFail(engine);
        SimulationResult early = engine.GetSnapshot();

        StepOrFail(engine);
        SimulationResult late = engine.GetSnapshot();

        Assert.Single(early.EventLog);
        Assert.Equal(1, early.TotalPacketsDropped);
        Assert.Equal(1, early.DropsByReason[PacketDropReason.NoRouteFound]);
        Assert.Equal(2, late.EventLog.Count);
    }

    [Fact]
    public void RunToCompletionResultIsNotAffectedByLaterScheduling()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"));
        SimulationResult first = engine.RunToCompletion();

        engine.SchedulePacket(Packet("p2", "A", "B"), 100.0);
        SimulationResult second = engine.RunToCompletion();

        Assert.Equal(2, first.EventLog.Count);
        Assert.Equal(1, first.TotalPacketsSent);
        Assert.Equal(4, second.EventLog.Count);
        Assert.Equal(2, second.TotalPacketsSent);
    }

    [Fact]
    public void SchedulePacketBetweenStepsInterleavesEventsByTimestamp()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"), 0.0);
        StepOrFail(engine);

        engine.SchedulePacket(Packet("p2", "A", "B"), 5.0);
        while (engine.HasPendingEvents)
        {
            engine.Step();
        }

        SimulationResult result = engine.GetSnapshot();

        double[] expectedTimestamps = [0.0, 5.0, 10.0, 15.0];
        string[] expectedPacketOrder = ["p1", "p2", "p1", "p2"];
        Assert.Equal(expectedTimestamps, result.EventLog.Select(e => e.TimestampMs));
        Assert.Equal(expectedPacketOrder, result.EventLog.Select(e => e.Packet.Id));
        Assert.Equal(2, result.TotalPacketsDelivered);
    }

    [Fact]
    public void SchedulePacketAfterStepsThrowsWhenTimestampIsInThePast()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"));
        StepOrFail(engine);
        StepOrFail(engine);
        Assert.Equal(10.0, engine.CurrentTimeMs);

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.SchedulePacket(Packet("p2", "A", "B"), 9.0));

        Exception? atCurrentTime = Record.Exception(() => engine.SchedulePacket(Packet("p3", "A", "B"), 10.0));
        Assert.Null(atCurrentTime);
    }
}