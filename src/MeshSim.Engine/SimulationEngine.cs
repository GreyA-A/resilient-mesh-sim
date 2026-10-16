using MeshSim.Core;

namespace MeshSim.Engine;

/// <summary>
/// Дискретно-подійний симулятор (DES) передавання пакетів у mesh-мережі.
/// </summary>
public sealed class SimulationEngine
{
    public const double DefaultHopDelayMs = 10.0;
    public const double DefaultEnergyPerTransmissionPercent = 1.0;

    private readonly NetworkTopology _topology;
    private readonly IRoutingStrategy _routingStrategy;
    private readonly PriorityQueue<SimulationEvent, (double Timestamp, long Sequence)> _eventQueue = new();
    private readonly List<SimulationEvent> _eventLog = [];
    private readonly Dictionary<PacketDropReason, int> _dropsByReason = [];

    private long _sequenceCounter;
    private int _packetsSent;
    private int _packetsDelivered;
    private int _packetsDropped;
    private int _totalDeliveredHops;

    public double CurrentTimeMs { get; private set; }
    public double HopDelayMs { get; }
    public double EnergyPerTransmissionPercent { get; }

    public SimulationEngine(
        NetworkTopology topology,
        IRoutingStrategy routingStrategy,
        double hopDelayMs = DefaultHopDelayMs,
        double energyPerTransmissionPercent = DefaultEnergyPerTransmissionPercent)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(routingStrategy);

        if (hopDelayMs <= 0.0 || double.IsNaN(hopDelayMs) || double.IsInfinity(hopDelayMs))
        {
            throw new ArgumentOutOfRangeException(nameof(hopDelayMs), "Затримка на стрибок має бути додатним скінченним числом.");
        }

        if (energyPerTransmissionPercent < 0.0 || double.IsNaN(energyPerTransmissionPercent) || double.IsInfinity(energyPerTransmissionPercent))
        {
            throw new ArgumentOutOfRangeException(nameof(energyPerTransmissionPercent), "Витрата енергії має бути невід'ємним скінченним числом.");
        }

        _topology = topology;
        _routingStrategy = routingStrategy;
        HopDelayMs = hopDelayMs;
        EnergyPerTransmissionPercent = energyPerTransmissionPercent;
    }

    /// <summary>
    /// Планує відправку нового пакета у віртуальний час timestampMs.
    /// </summary>
    public void SchedulePacket(NetworkPacket packet, double timestampMs = 0.0)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (timestampMs < CurrentTimeMs)
        {
            throw new ArgumentOutOfRangeException(nameof(timestampMs), "Не можна планувати подію в минулому відносно поточного часу симуляції.");
        }

        var initialEvent = new SimulationEvent(
            timestampMs,
            SimulationEventType.PacketEnqueued,
            packet,
            packet.SourceNodeId);

        _packetsSent++;
        EnqueueEvent(initialEvent);
    }

    /// <summary>
    /// Виконує всі заплановані події в черзі до її вичерпання та повертає підсумкові метрики.
    /// </summary>
    public SimulationResult RunToCompletion()
    {
        while (_eventQueue.Count > 0)
        {
            SimulationEvent currentEvent = _eventQueue.Dequeue();
            CurrentTimeMs = currentEvent.TimestampMs;
            _eventLog.Add(currentEvent);

            ProcessEvent(currentEvent);
        }

        double avgHops = _packetsDelivered == 0
            ? 0.0
            : (double)_totalDeliveredHops / _packetsDelivered;

        return new SimulationResult(
            _packetsSent,
            _packetsDelivered,
            _packetsDropped,
            avgHops,
            new Dictionary<PacketDropReason, int>(_dropsByReason),
            _eventLog.AsReadOnly());
    }

    private void ProcessEvent(SimulationEvent simEvent)
    {
        if (simEvent.EventType is SimulationEventType.PacketDelivered or SimulationEventType.PacketDropped)
        {
            return;
        }

        NetworkPacket packet = simEvent.Packet;
        string currentNodeId = simEvent.CurrentNodeId;

        // Якщо пакет вже на вузлі призначення
        if (string.Equals(currentNodeId, packet.DestinationNodeId, StringComparison.Ordinal))
        {
            _packetsDelivered++;
            _totalDeliveredHops += packet.HopCount;
            EnqueueEvent(new SimulationEvent(CurrentTimeMs, SimulationEventType.PacketDelivered, packet, packet.DestinationNodeId));
            return;
        }

        // Перевірка існування та заряду поточного вузла
        if (!_topology.Nodes.TryGetValue(currentNodeId, out MeshNode? currentNode))
        {
            RecordDrop(packet, currentNodeId, PacketDropReason.NodeNotFound);
            return;
        }

        if (!currentNode.IsActive)
        {
            RecordDrop(packet, currentNodeId, PacketDropReason.NodeBatteryDepleted);
            return;
        }

        // Динамічний пошук маршруту з поточної точки до отримувача
        IReadOnlyList<string> route = _routingStrategy.FindRoute(_topology, currentNodeId, packet.DestinationNodeId);
        if (route.Count < 2)
        {
            RecordDrop(packet, currentNodeId, PacketDropReason.NoRouteFound);
            return;
        }

        string nextHopNodeId = route[1];
        MeshNode nextHopNode = _topology.Nodes[nextHopNodeId];

        // Радіопередача відбулася незалежно від результату: відправник витрачає енергію,
        // навіть якщо канал заглушено. Це навмисна поведінка.
        currentNode.ConsumeEnergy(EnergyPerTransmissionPercent);

        // Перевірка якості каналу (чи не перекритий РЕБ)
        if (!_topology.IsLinkAvailable(currentNode, nextHopNode))
        {
            RecordDrop(packet, currentNodeId, PacketDropReason.JammedInTransit);
            return;
        }

        // Виконуємо стрибок: зменшуємо TTL та збільшуємо HopCount
        bool stillAlive = packet.ForwardHop();

        // Якщо ми досягли вузла призначення — пакет доставлено (навіть якщо TTL став 0 на останньому кроці)
        if (string.Equals(nextHopNodeId, packet.DestinationNodeId, StringComparison.Ordinal))
        {
            _packetsDelivered++;
            _totalDeliveredHops += packet.HopCount;
            EnqueueEvent(new SimulationEvent(CurrentTimeMs + HopDelayMs, SimulationEventType.PacketDelivered, packet, nextHopNodeId));
            return;
        }

        // Якщо це проміжний ретранслятор і TTL вичерпано — пакет відкидається
        if (!stillAlive)
        {
            RecordDrop(packet, nextHopNodeId, PacketDropReason.TtlExpired);
            return;
        }

        // Плануємо наступну подію ретрансляції
        EnqueueEvent(new SimulationEvent(CurrentTimeMs + HopDelayMs, SimulationEventType.PacketForwarded, packet, nextHopNodeId));
    }

    private void RecordDrop(NetworkPacket packet, string nodeId, PacketDropReason reason)
    {
        _packetsDropped++;
        _dropsByReason.TryGetValue(reason, out int count);
        _dropsByReason[reason] = count + 1;

        EnqueueEvent(new SimulationEvent(CurrentTimeMs, SimulationEventType.PacketDropped, packet, nodeId, reason));
    }

    private void EnqueueEvent(SimulationEvent simEvent)
    {
        _sequenceCounter++;
        _eventQueue.Enqueue(simEvent, (simEvent.TimestampMs, _sequenceCounter));
    }
}