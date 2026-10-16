using MeshSim.Core;
using MeshSim.Engine;
using Xunit;

namespace MeshSim.Tests;

public class SimulationEventTests
{
    private static NetworkPacket CreatePacket() => new("p1", "A", "B", 5, 64);

    [Fact]
    public void ConstructorStoresAllProperties()
    {
        NetworkPacket packet = CreatePacket();

        var simEvent = new SimulationEvent(12.5, SimulationEventType.PacketDropped, packet, "B", PacketDropReason.TtlExpired);

        Assert.Equal(12.5, simEvent.TimestampMs);
        Assert.Equal(SimulationEventType.PacketDropped, simEvent.EventType);
        Assert.Same(packet, simEvent.Packet);
        Assert.Equal("B", simEvent.CurrentNodeId);
        Assert.Equal(PacketDropReason.TtlExpired, simEvent.DropReason);
    }

    [Fact]
    public void DropReasonDefaultsToNone()
    {
        var simEvent = new SimulationEvent(0.0, SimulationEventType.PacketEnqueued, CreatePacket(), "A");

        Assert.Equal(PacketDropReason.None, simEvent.DropReason);
    }

    [Fact]
    public void ConstructorAcceptsZeroTimestamp()
    {
        var simEvent = new SimulationEvent(0.0, SimulationEventType.PacketEnqueued, CreatePacket(), "A");

        Assert.Equal(0.0, simEvent.TimestampMs);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void ConstructorThrowsArgumentOutOfRangeExceptionForInvalidTimestamp(double timestamp)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationEvent(timestamp, SimulationEventType.PacketEnqueued, CreatePacket(), "A"));
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionForNullPacket()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SimulationEvent(0.0, SimulationEventType.PacketEnqueued, null!, "A"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorThrowsArgumentExceptionForEmptyNodeId(string nodeId)
    {
        Assert.Throws<ArgumentException>(() =>
            new SimulationEvent(0.0, SimulationEventType.PacketEnqueued, CreatePacket(), nodeId));
    }

    [Fact]
    public void ConstructorThrowsArgumentExceptionForNullNodeId()
    {
        Assert.Throws<ArgumentException>(() =>
            new SimulationEvent(0.0, SimulationEventType.PacketEnqueued, CreatePacket(), null!));
    }

    [Fact]
    public void EventsWithSameValuesAndSamePacketReferenceAreEqual()
    {
        NetworkPacket packet = CreatePacket();

        var first = new SimulationEvent(5.0, SimulationEventType.PacketForwarded, packet, "B");
        var second = new SimulationEvent(5.0, SimulationEventType.PacketForwarded, packet, "B");

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void EventsDifferWhenAnyValueDiffers()
    {
        NetworkPacket packet = CreatePacket();
        var baseline = new SimulationEvent(5.0, SimulationEventType.PacketForwarded, packet, "B");

        Assert.NotEqual(baseline, new SimulationEvent(6.0, SimulationEventType.PacketForwarded, packet, "B"));
        Assert.NotEqual(baseline, new SimulationEvent(5.0, SimulationEventType.PacketDelivered, packet, "B"));
        Assert.NotEqual(baseline, new SimulationEvent(5.0, SimulationEventType.PacketForwarded, packet, "C"));
        Assert.NotEqual(baseline, new SimulationEvent(5.0, SimulationEventType.PacketForwarded, CreatePacket(), "B"));
    }

    // ---------- Фіксація числових значень enum ----------
    // Нові члени завжди додаються в кінець, щоб не зламати збережені дані та порівняння між версіями.

    [Theory]
    [InlineData(PacketDropReason.None, 0)]
    [InlineData(PacketDropReason.NoRouteFound, 1)]
    [InlineData(PacketDropReason.JammedInTransit, 2)]
    [InlineData(PacketDropReason.TtlExpired, 3)]
    [InlineData(PacketDropReason.NodeBatteryDepleted, 4)]
    [InlineData(PacketDropReason.BufferOverflow, 5)]
    [InlineData(PacketDropReason.NodeNotFound, 6)]
    public void PacketDropReasonValuesAreStable(PacketDropReason reason, int expected)
    {
        Assert.Equal(expected, (int)reason);
    }

    [Theory]
    [InlineData(SimulationEventType.PacketEnqueued, 0)]
    [InlineData(SimulationEventType.PacketForwarded, 1)]
    [InlineData(SimulationEventType.PacketDelivered, 2)]
    [InlineData(SimulationEventType.PacketDropped, 3)]
    public void SimulationEventTypeValuesAreStable(SimulationEventType type, int expected)
    {
        Assert.Equal(expected, (int)type);
    }
}