using MeshSim.Core;
using MeshSim.Engine;
using Xunit;

namespace MeshSim.Tests;

public partial class SimulationEngineTests
{
    private static MeshNode Node(string id, double x, double y, double range = 120.0, double battery = 100.0)
        => new(id, new Position(x, y), range, battery);

    private static NetworkPacket Packet(string id, string src, string dst, int ttl = 5)
        => new(id, src, dst, ttl, payloadBytes: 64);

    /// <summary>
    /// Стратегія-заглушка, що завжди повертає заздалегідь заданий маршрут і ігнорує завади.
    /// Потрібна, щоб досягти JammedInTransit: Dijkstra сам обходить заглушені лінки.
    /// </summary>
    private sealed class FixedRouteStrategy : IRoutingStrategy
    {
        private readonly IReadOnlyList<string> _route;

        public FixedRouteStrategy(params string[] route) => _route = route;

        public IReadOnlyList<string> FindRoute(NetworkTopology topology, string sourceNodeId, string destinationNodeId) => _route;
    }

    private static NetworkTopology Chain()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 100.0));
        topology.AddNode(Node("B", 80, 0, range: 100.0));
        topology.AddNode(Node("C", 160, 0, range: 100.0));
        return topology;
    }

    private static NetworkTopology Pair()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 100.0));
        topology.AddNode(Node("B", 50, 0, range: 100.0));
        return topology;
    }

    private static void AssertEvent(
        SimulationEvent actual,
        double timestamp,
        SimulationEventType type,
        string packetId,
        string nodeId,
        PacketDropReason reason = PacketDropReason.None)
    {
        Assert.Equal(timestamp, actual.TimestampMs);
        Assert.Equal(type, actual.EventType);
        Assert.Equal(packetId, actual.Packet.Id);
        Assert.Equal(nodeId, actual.CurrentNodeId);
        Assert.Equal(reason, actual.DropReason);
    }

    // ================= Оригінальні тести =================

    [Fact]
    public void RunToCompletionDeliversPacketAcrossMultiHopRouteAndCalculatesPdr()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 100.0));
        topology.AddNode(Node("B", 80, 0, range: 100.0));
        topology.AddNode(Node("C", 160, 0, range: 100.0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy(), hopDelayMs: 15.0, energyPerTransmissionPercent: 2.0);
        engine.SchedulePacket(new NetworkPacket("pkt-1", "A", "C", ttl: 5, payloadBytes: 64), timestampMs: 0.0);

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.TotalPacketsSent);
        Assert.Equal(1, result.TotalPacketsDelivered);
        Assert.Equal(0, result.TotalPacketsDropped);
        Assert.Equal(1.0, result.PacketDeliveryRatio);
        Assert.Equal(2.0, result.AverageHopCount);
        Assert.Equal(30.0, engine.CurrentTimeMs);
        Assert.Equal(98.0, topology.Nodes["A"].BatteryLevelPercent);
        Assert.Equal(98.0, topology.Nodes["B"].BatteryLevelPercent);
    }

    [Fact]
    public void RunToCompletionReroutesAroundJammingZoneAndMaintainsDelivery()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 120.0));
        topology.AddNode(Node("B", 50, 60, range: 120.0));
        topology.AddNode(Node("C", 100, 0, range: 120.0));
        topology.AddJammingZone(new JammingZone("jam-1", new Position(50, 0), 25.0, 1.0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        engine.SchedulePacket(new NetworkPacket("pkt-1", "A", "C", ttl: 5, payloadBytes: 128));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1.0, result.PacketDeliveryRatio);
        Assert.Equal(2.0, result.AverageHopCount);
        Assert.Empty(result.DropsByReason);
    }

    [Fact]
    public void RunToCompletionDropsPacketWithTtlExpiredWhenRouteIsLongerThanTtl()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 100.0));
        topology.AddNode(Node("B", 80, 0, range: 100.0));
        topology.AddNode(Node("C", 160, 0, range: 100.0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        // Маршрут A -> B -> C вимагає 2 стрибки, але TTL = 1
        engine.SchedulePacket(new NetworkPacket("pkt-ttl", "A", "C", ttl: 1, payloadBytes: 64));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(0, result.TotalPacketsDelivered);
        Assert.Equal(1, result.TotalPacketsDropped);
        Assert.Equal(0.0, result.PacketDeliveryRatio);
        Assert.Equal(1, result.DropsByReason[PacketDropReason.TtlExpired]);
    }

    [Fact]
    public void RunToCompletionDropsPacketWithNoRouteFoundWhenDestinationIsIsolated()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 50.0));
        topology.AddNode(Node("B", 500, 0, range: 50.0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        engine.SchedulePacket(new NetworkPacket("pkt-iso", "A", "B", ttl: 5, payloadBytes: 64));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(0.0, result.PacketDeliveryRatio);
        Assert.Equal(1, result.DropsByReason[PacketDropReason.NoRouteFound]);
    }

    [Fact]
    public void RunToCompletionDropsPacketWithNodeBatteryDepletedWhenSourceIsInactive()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 100.0, battery: 0.0));
        topology.AddNode(Node("B", 50, 0, range: 100.0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        engine.SchedulePacket(new NetworkPacket("pkt-dead", "A", "B", ttl: 5, payloadBytes: 64));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.DropsByReason[PacketDropReason.NodeBatteryDepleted]);
    }

    [Fact]
    public void RunToCompletionDropsPacketWithJammedInTransitWhenSenderDrainsBeforeLinkCheck()
    {
        var topology = new NetworkTopology();
        // Вузол A має заряд 1%, а передача списує 5% -> під час передачі вузол гасне і лінк стає недоступним
        topology.AddNode(Node("A", 0, 0, range: 100.0, battery: 1.0));
        topology.AddNode(Node("B", 50, 0, range: 100.0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy(), energyPerTransmissionPercent: 5.0);
        engine.SchedulePacket(new NetworkPacket("pkt-drain", "A", "B", ttl: 5, payloadBytes: 64));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.DropsByReason[PacketDropReason.JammedInTransit]);
    }

    [Fact]
    public void RunToCompletionDeliversImmediatelyWhenSourceEqualsDestination()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        engine.SchedulePacket(new NetworkPacket("pkt-self", "A", "A", ttl: 3, payloadBytes: 32));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.TotalPacketsDelivered);
        Assert.Equal(0.0, result.AverageHopCount);
    }

    [Fact]
    public void RunToCompletionWithEmptyQueueReturnsZeroMetrics()
    {
        var engine = new SimulationEngine(new NetworkTopology(), new DijkstraRoutingStrategy());

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(0, result.TotalPacketsSent);
        Assert.Equal(0.0, result.PacketDeliveryRatio);
        Assert.Empty(result.EventLog);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(-5.0, 1.0)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(double.PositiveInfinity, 1.0)]
    [InlineData(10.0, -1.0)]
    [InlineData(10.0, double.NaN)]
    [InlineData(10.0, double.PositiveInfinity)]
    public void EngineConstructorThrowsArgumentOutOfRangeExceptionForInvalidNumericParameters(double delay, double energy)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationEngine(new NetworkTopology(), new DijkstraRoutingStrategy(), delay, energy));
    }

    [Fact]
    public void SimulationEventAndResultValidateConstructorArguments()
    {
        var packet = new NetworkPacket("p1", "A", "B", 3, 64);

        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationEvent(-1.0, SimulationEventType.PacketEnqueued, packet, "A"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationEvent(double.NaN, SimulationEventType.PacketEnqueued, packet, "A"));
        Assert.Throws<ArgumentNullException>(() => new SimulationEvent(0.0, SimulationEventType.PacketEnqueued, null!, "A"));
        Assert.Throws<ArgumentException>(() => new SimulationEvent(0.0, SimulationEventType.PacketEnqueued, packet, "  "));

        var emptyDrops = new Dictionary<PacketDropReason, int>();
        var emptyLog = Array.Empty<SimulationEvent>();

        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationResult(-1, 0, 0, 0.0, emptyDrops, emptyLog));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationResult(0, -1, 0, 0.0, emptyDrops, emptyLog));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationResult(0, 0, -1, 0.0, emptyDrops, emptyLog));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationResult(0, 0, 0, -1.0, emptyDrops, emptyLog));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationResult(0, 0, 0, double.NaN, emptyDrops, emptyLog));
    }

    // ================= Конструктор рушія =================

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenTopologyOrStrategyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SimulationEngine(null!, new DijkstraRoutingStrategy()));
        Assert.Throws<ArgumentNullException>(() => new SimulationEngine(new NetworkTopology(), null!));
    }

    [Fact]
    public void ConstructorUsesDocumentedDefaults()
    {
        var engine = new SimulationEngine(new NetworkTopology(), new DijkstraRoutingStrategy());

        Assert.Equal(SimulationEngine.DefaultHopDelayMs, engine.HopDelayMs);
        Assert.Equal(SimulationEngine.DefaultEnergyPerTransmissionPercent, engine.EnergyPerTransmissionPercent);
        Assert.Equal(10.0, engine.HopDelayMs);
        Assert.Equal(1.0, engine.EnergyPerTransmissionPercent);
        Assert.Equal(0.0, engine.CurrentTimeMs);
    }

    [Fact]
    public void ConstructorStoresCustomParametersAndAllowsZeroEnergy()
    {
        var engine = new SimulationEngine(new NetworkTopology(), new DijkstraRoutingStrategy(), 25.0, 0.0);

        Assert.Equal(25.0, engine.HopDelayMs);
        Assert.Equal(0.0, engine.EnergyPerTransmissionPercent);
    }

    // ================= SchedulePacket =================

    [Fact]
    public void SchedulePacketThrowsArgumentNullExceptionForNullPacket()
    {
        var engine = new SimulationEngine(new NetworkTopology(), new DijkstraRoutingStrategy());

        Assert.Throws<ArgumentNullException>(() => engine.SchedulePacket(null!));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1.0)]
    public void SchedulePacketThrowsAndDoesNotCountPacketWhenTimestampIsInvalid(double timestamp)
    {
        var engine = new SimulationEngine(new NetworkTopology(), new DijkstraRoutingStrategy());

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.SchedulePacket(Packet("p1", "A", "B"), timestamp));

        Assert.Equal(0, engine.RunToCompletion().TotalPacketsSent);
    }

    [Fact]
    public void SchedulePacketThrowsWhenTimestampIsEarlierThanCurrentTime()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"));
        engine.RunToCompletion();
        Assert.Equal(10.0, engine.CurrentTimeMs);

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.SchedulePacket(Packet("p2", "A", "B"), 5.0));
    }

    [Fact]
    public void SchedulePacketAfterRunAtFutureTimeIsProcessedByNextRun()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"));
        engine.RunToCompletion();

        engine.SchedulePacket(Packet("p2", "A", "B"), 100.0);
        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(2, result.TotalPacketsSent);
        Assert.Equal(2, result.TotalPacketsDelivered);
        Assert.Equal(4, result.EventLog.Count);
        Assert.Equal(110.0, engine.CurrentTimeMs);
    }

    [Fact]
    public void RunToCompletionCalledTwiceDoesNotDoubleCountResults()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"));

        SimulationResult first = engine.RunToCompletion();
        SimulationResult second = engine.RunToCompletion();

        Assert.Equal(first.TotalPacketsSent, second.TotalPacketsSent);
        Assert.Equal(first.TotalPacketsDelivered, second.TotalPacketsDelivered);
        Assert.Equal(first.EventLog.Count, second.EventLog.Count);
    }

    // ================= Журнал подій =================

    [Fact]
    public void EventLogRecordsEnqueuedForwardedAndDeliveredEventsWithTimestamps()
    {
        var engine = new SimulationEngine(Chain(), new DijkstraRoutingStrategy(), hopDelayMs: 10.0);
        engine.SchedulePacket(Packet("p1", "A", "C"));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(3, result.EventLog.Count);
        AssertEvent(result.EventLog[0], 0.0, SimulationEventType.PacketEnqueued, "p1", "A");
        AssertEvent(result.EventLog[1], 10.0, SimulationEventType.PacketForwarded, "p1", "B");
        AssertEvent(result.EventLog[2], 20.0, SimulationEventType.PacketDelivered, "p1", "C");
    }

    [Fact]
    public void DirectDeliveryLogsEnqueuedThenDeliveredAndChargesOnlyTheSender()
    {
        NetworkTopology topology = Pair();
        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(2, result.EventLog.Count);
        AssertEvent(result.EventLog[0], 0.0, SimulationEventType.PacketEnqueued, "p1", "A");
        AssertEvent(result.EventLog[1], 10.0, SimulationEventType.PacketDelivered, "p1", "B");
        Assert.Equal(1.0, result.AverageHopCount);
        Assert.Equal(10.0, engine.CurrentTimeMs);
        Assert.Equal(99.0, topology.Nodes["A"].BatteryLevelPercent);
        Assert.Equal(100.0, topology.Nodes["B"].BatteryLevelPercent);
    }

    [Fact]
    public void SourceEqualsDestinationLogsDeliveryAtSameTimestampWithoutEnergyCost()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "A"), 7.0);

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(2, result.EventLog.Count);
        AssertEvent(result.EventLog[0], 7.0, SimulationEventType.PacketEnqueued, "p1", "A");
        AssertEvent(result.EventLog[1], 7.0, SimulationEventType.PacketDelivered, "p1", "A");
        Assert.Equal(100.0, topology.Nodes["A"].BatteryLevelPercent);
    }

    [Fact]
    public void EventsAreProcessedInTimestampOrderRegardlessOfSchedulingOrder()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("late", "A", "B"), 50.0);
        engine.SchedulePacket(Packet("early", "A", "B"), 5.0);

        SimulationResult result = engine.RunToCompletion();

        double[] expectedTimestamps = [5.0, 15.0, 50.0, 60.0];
        Assert.Equal(expectedTimestamps, result.EventLog.Select(e => e.TimestampMs));
        Assert.Equal("early", result.EventLog[0].Packet.Id);
        Assert.Equal("late", result.EventLog[2].Packet.Id);
        Assert.Equal(60.0, engine.CurrentTimeMs);
    }

    [Fact]
    public void EventsWithEqualTimestampsAreProcessedInSchedulingOrder()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "B"), 0.0);
        engine.SchedulePacket(Packet("p2", "A", "B"), 0.0);

        SimulationResult result = engine.RunToCompletion();

        string[] expectedPacketOrder = ["p1", "p2", "p1", "p2"];
        Assert.Equal(expectedPacketOrder, result.EventLog.Select(e => e.Packet.Id));
        Assert.Equal(SimulationEventType.PacketEnqueued, result.EventLog[0].EventType);
        Assert.Equal(SimulationEventType.PacketEnqueued, result.EventLog[1].EventType);
        Assert.Equal(SimulationEventType.PacketDelivered, result.EventLog[2].EventType);
        Assert.Equal(SimulationEventType.PacketDelivered, result.EventLog[3].EventType);
    }

    // ================= TTL =================

    [Fact]
    public void PacketWhoseTtlEqualsHopCountIsStillDelivered()
    {
        var engine = new SimulationEngine(Chain(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "C", ttl: 2));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.TotalPacketsDelivered);
        Assert.Equal(2.0, result.AverageHopCount);
        Assert.Empty(result.DropsByReason);
    }

    [Fact]
    public void TtlExpiredDropIsLoggedAtTheRelayWhereTtlRanOut()
    {
        var engine = new SimulationEngine(Chain(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "C", ttl: 1));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(2, result.EventLog.Count);
        AssertEvent(result.EventLog[0], 0.0, SimulationEventType.PacketEnqueued, "p1", "A");
        AssertEvent(result.EventLog[1], 0.0, SimulationEventType.PacketDropped, "p1", "B", PacketDropReason.TtlExpired);
    }

    // ================= Причини втрат =================

    [Fact]
    public void JammedTransmissionStillConsumesSenderEnergy()
    {
        var topology = new NetworkTopology();
        MeshNode a = Node("A", 0, 0, range: 100.0);
        topology.AddNode(a);
        topology.AddNode(Node("B", 50, 0, range: 100.0));
        topology.AddJammingZone(new JammingZone("jam", new Position(25, 0), 10.0, 1.0));

        var engine = new SimulationEngine(topology, new FixedRouteStrategy("A", "B"), energyPerTransmissionPercent: 5.0);
        engine.SchedulePacket(Packet("p1", "A", "B"));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.DropsByReason[PacketDropReason.JammedInTransit]);
        Assert.Equal(0, result.TotalPacketsDelivered);
        // Навмисна поведінка: радіопередача відбулася, тож відправник витратив енергію.
        Assert.Equal(95.0, a.BatteryLevelPercent);
        AssertEvent(result.EventLog[^1], 0.0, SimulationEventType.PacketDropped, "p1", "A", PacketDropReason.JammedInTransit);
    }

    [Fact]
    public void PacketFromUnknownNodeIsDroppedAsNodeNotFound()
    {
        var engine = new SimulationEngine(new NetworkTopology(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "GHOST", "B"));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.DropsByReason[PacketDropReason.NodeNotFound]);
        Assert.False(result.DropsByReason.ContainsKey(PacketDropReason.NodeBatteryDepleted));
        AssertEvent(result.EventLog[^1], 0.0, SimulationEventType.PacketDropped, "p1", "GHOST", PacketDropReason.NodeNotFound);
    }

    [Fact]
    public void PacketToUnknownDestinationIsDroppedAsNoRouteFound()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "GHOST"));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.DropsByReason[PacketDropReason.NoRouteFound]);
        AssertEvent(result.EventLog[^1], 0.0, SimulationEventType.PacketDropped, "p1", "A", PacketDropReason.NoRouteFound);
    }

    [Fact]
    public void RelayDrainedByEarlierPacketCausesJamDropThenBatteryDepletedDrop()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 100.0));
        topology.AddNode(Node("B", 80, 0, range: 100.0, battery: 1.0));
        topology.AddNode(Node("C", 160, 0, range: 100.0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("p1", "A", "C"));
        engine.SchedulePacket(Packet("p2", "A", "C"));

        SimulationResult result = engine.RunToCompletion();

        // p1 доходить до B першим: передача виснажує B до нуля, тож лінк B->C недоступний.
        // p2 приходить на вже "мертвий" B.
        Assert.Equal(0, result.TotalPacketsDelivered);
        Assert.Equal(2, result.TotalPacketsDropped);
        Assert.Equal(1, result.DropsByReason[PacketDropReason.JammedInTransit]);
        Assert.Equal(1, result.DropsByReason[PacketDropReason.NodeBatteryDepleted]);
    }

    // ================= Метрики =================

    [Fact]
    public void MixedOutcomesProduceCorrectPdrAverageHopsAndDropBreakdown()
    {
        NetworkTopology topology = Chain();
        topology.AddNode(Node("D", 1000, 0, range: 100.0));

        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy());
        engine.SchedulePacket(Packet("one-hop", "A", "B"));
        engine.SchedulePacket(Packet("two-hop", "A", "C"));
        engine.SchedulePacket(Packet("lost", "A", "D"));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(3, result.TotalPacketsSent);
        Assert.Equal(2, result.TotalPacketsDelivered);
        Assert.Equal(1, result.TotalPacketsDropped);
        Assert.Equal(2.0 / 3.0, result.PacketDeliveryRatio, precision: 9);
        // Середнє рахується лише по доставлених пакетах: (1 + 2) / 2
        Assert.Equal(1.5, result.AverageHopCount, precision: 9);
        Assert.Equal(1, result.DropsByReason[PacketDropReason.NoRouteFound]);
    }

    [Fact]
    public void ZeroEnergyPerTransmissionKeepsBatteriesUntouched()
    {
        NetworkTopology topology = Chain();
        var engine = new SimulationEngine(topology, new DijkstraRoutingStrategy(), energyPerTransmissionPercent: 0.0);
        engine.SchedulePacket(Packet("p1", "A", "C"));

        SimulationResult result = engine.RunToCompletion();

        Assert.Equal(1, result.TotalPacketsDelivered);
        Assert.All(topology.Nodes.Values, node => Assert.Equal(100.0, node.BatteryLevelPercent));
    }

    [Fact]
    public void CustomHopDelayDeterminesDeliveryTime()
    {
        var engine = new SimulationEngine(Pair(), new DijkstraRoutingStrategy(), hopDelayMs: 25.0);
        engine.SchedulePacket(Packet("p1", "A", "B"), 5.0);

        engine.RunToCompletion();

        Assert.Equal(30.0, engine.CurrentTimeMs);
    }
}