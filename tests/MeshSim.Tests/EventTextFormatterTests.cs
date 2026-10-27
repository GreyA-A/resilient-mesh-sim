using System.Globalization;
using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class EventTextFormatterTests
{
    private static NetworkPacket Packet() => new("p1", "A", "C", 5, 64);

    [Theory]
    [InlineData(SimulationEventType.PacketEnqueued, "Поставлено в чергу")]
    [InlineData(SimulationEventType.PacketForwarded, "Передано на вузол")]
    [InlineData(SimulationEventType.PacketDelivered, "Доставлено")]
    [InlineData(SimulationEventType.PacketDropped, "Втрачено")]
    public void DescribeEventTypeReturnsUkrainianText(SimulationEventType type, string expected)
    {
        Assert.Equal(expected, EventTextFormatter.DescribeEventType(type));
    }

    [Fact]
    public void DescribeEventTypeThrowsForUnknownValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EventTextFormatter.DescribeEventType((SimulationEventType)99));
    }

    [Theory]
    [InlineData(PacketDropReason.None, "без причини")]
    [InlineData(PacketDropReason.NoRouteFound, "немає маршруту")]
    [InlineData(PacketDropReason.JammedInTransit, "канал заглушено завадами")]
    [InlineData(PacketDropReason.TtlExpired, "вичерпано TTL")]
    [InlineData(PacketDropReason.NodeBatteryDepleted, "розряджено батарею вузла")]
    [InlineData(PacketDropReason.BufferOverflow, "переповнено буфер вузла")]
    [InlineData(PacketDropReason.NodeNotFound, "вузол не знайдено")]
    public void DescribeDropReasonReturnsUkrainianText(PacketDropReason reason, string expected)
    {
        Assert.Equal(expected, EventTextFormatter.DescribeDropReason(reason));
    }

    [Fact]
    public void DescribeDropReasonThrowsForUnknownValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EventTextFormatter.DescribeDropReason((PacketDropReason)99));
    }

    [Fact]
    public void FormatEventOmitsReasonForNonDroppedEvents()
    {
        var simulationEvent = new SimulationEvent(12.5, SimulationEventType.PacketForwarded, Packet(), "B");

        Assert.Equal("12.5 мс | p1 | Передано на вузол | вузол B", EventTextFormatter.FormatEvent(simulationEvent));
    }

    [Fact]
    public void FormatEventAppendsReasonForDroppedEvents()
    {
        var simulationEvent = new SimulationEvent(0.0, SimulationEventType.PacketDropped, Packet(), "A", PacketDropReason.NoRouteFound);

        Assert.Equal("0.0 мс | p1 | Втрачено | вузол A | немає маршруту", EventTextFormatter.FormatEvent(simulationEvent));
    }

    [Fact]
    public void FormatEventUsesInvariantCultureForTimestamp()
    {
        var simulationEvent = new SimulationEvent(12.5, SimulationEventType.PacketEnqueued, Packet(), "A");
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.StartsWith("12.5 мс", EventTextFormatter.FormatEvent(simulationEvent), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}