using MeshSim.Core;
using MeshSim.Engine;
using Xunit;

namespace MeshSim.Tests;

public class SimulationResultTests
{
    private static readonly IReadOnlyList<SimulationEvent> EmptyLog = Array.Empty<SimulationEvent>();

    private static Dictionary<PacketDropReason, int> Drops(params (PacketDropReason Reason, int Count)[] entries)
    {
        var dictionary = new Dictionary<PacketDropReason, int>();
        foreach ((PacketDropReason reason, int count) in entries)
        {
            dictionary[reason] = count;
        }

        return dictionary;
    }

    // ---------- Властивості та PDR ----------

    [Fact]
    public void ConstructorStoresAllValues()
    {
        var packet = new NetworkPacket("p1", "A", "B", 5, 64);
        var log = new List<SimulationEvent>
        {
            new(0.0, SimulationEventType.PacketEnqueued, packet, "A")
        };
        Dictionary<PacketDropReason, int> drops = Drops((PacketDropReason.NoRouteFound, 1));

        var result = new SimulationResult(4, 2, 1, 1.5, drops, log);

        Assert.Equal(4, result.TotalPacketsSent);
        Assert.Equal(2, result.TotalPacketsDelivered);
        Assert.Equal(1, result.TotalPacketsDropped);
        Assert.Equal(1.5, result.AverageHopCount);
        Assert.Equal(1, result.DropsByReason[PacketDropReason.NoRouteFound]);
        Assert.Single(result.EventLog);
    }

    [Fact]
    public void PacketDeliveryRatioIsDeliveredDividedBySent()
    {
        var result = new SimulationResult(4, 3, 1, 1.0, Drops((PacketDropReason.TtlExpired, 1)), EmptyLog);

        Assert.Equal(0.75, result.PacketDeliveryRatio);
    }

    [Fact]
    public void PacketDeliveryRatioIsZeroWhenNothingWasSent()
    {
        var result = new SimulationResult(0, 0, 0, 0.0, Drops(), EmptyLog);

        Assert.Equal(0.0, result.PacketDeliveryRatio);
    }

    [Fact]
    public void PacketDeliveryRatioIsOneWhenEverythingIsDelivered()
    {
        var result = new SimulationResult(5, 5, 0, 2.0, Drops(), EmptyLog);

        Assert.Equal(1.0, result.PacketDeliveryRatio);
    }

    [Fact]
    public void ConstructorAcceptsPartialRunWhereSomePacketsAreStillInFlight()
    {
        var exception = Record.Exception(() =>
            new SimulationResult(5, 1, 1, 1.0, Drops((PacketDropReason.TtlExpired, 1)), EmptyLog));

        Assert.Null(exception);
    }

    // ---------- Базова валідація ----------

    [Fact]
    public void ConstructorThrowsArgumentOutOfRangeExceptionForInfiniteAverageHopCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationResult(0, 0, 0, double.PositiveInfinity, Drops(), EmptyLog));
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionForNullCollections()
    {
        Assert.Throws<ArgumentNullException>(() => new SimulationResult(0, 0, 0, 0.0, null!, EmptyLog));
        Assert.Throws<ArgumentNullException>(() => new SimulationResult(0, 0, 0, 0.0, Drops(), null!));
    }

    // ---------- Перевірки узгодженості ----------

    [Fact]
    public void ConstructorThrowsWhenDeliveredPlusDroppedExceedsSent()
    {
        // Решта умов дотримано: сума втрат за причинами = 1 = TotalPacketsDropped.
        Assert.ThrowsAny<ArgumentException>(() =>
            new SimulationResult(2, 2, 1, 1.0, Drops((PacketDropReason.NoRouteFound, 1)), EmptyLog));
    }

    [Fact]
    public void ConstructorThrowsWhenAnyDropCountIsNegative()
    {
        // Сума (2 + (-1)) = 1 збігається з TotalPacketsDropped, тож порушена лише умова невід'ємності.
        Assert.ThrowsAny<ArgumentException>(() =>
            new SimulationResult(
                2, 0, 1, 1.0,
                Drops((PacketDropReason.NoRouteFound, 2), (PacketDropReason.TtlExpired, -1)),
                EmptyLog));
    }

    [Fact]
    public void ConstructorThrowsWhenDropBreakdownDoesNotMatchTotalDropped()
    {
        // 1 + 2 + 0 <= 3, але розбивка дає 1, а TotalPacketsDropped = 2.
        Assert.ThrowsAny<ArgumentException>(() =>
            new SimulationResult(3, 1, 2, 1.0, Drops((PacketDropReason.NoRouteFound, 1)), EmptyLog));
    }

    [Fact]
    public void ConstructorAcceptsBreakdownThatMatchesTotalDroppedAcrossSeveralReasons()
    {
        var exception = Record.Exception(() =>
            new SimulationResult(
                5, 2, 3, 1.0,
                Drops((PacketDropReason.NoRouteFound, 1), (PacketDropReason.TtlExpired, 2)),
                EmptyLog));

        Assert.Null(exception);
    }

    [Fact]
    public void ConstructorAcceptsDeliveredPlusDroppedEqualToSent()
    {
        var exception = Record.Exception(() =>
            new SimulationResult(3, 2, 1, 1.0, Drops((PacketDropReason.JammedInTransit, 1)), EmptyLog));

        Assert.Null(exception);
    }
}