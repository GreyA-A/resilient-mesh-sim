using System.Globalization;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class MetricsViewModelTests
{
    private static SimulationResult Snapshot()
    {
        var drops = new Dictionary<PacketDropReason, int>
        {
            [PacketDropReason.TtlExpired] = 1,
            [PacketDropReason.NoRouteFound] = 1
        };

        return new SimulationResult(4, 2, 2, 1.5, drops, Array.Empty<SimulationEvent>());
    }

    [Fact]
    public void NewViewModelStartsWithZeroMetrics()
    {
        var metrics = new MetricsViewModel();

        Assert.Equal(0, metrics.TotalPacketsSent);
        Assert.Equal(0, metrics.TotalPacketsDelivered);
        Assert.Equal(0, metrics.TotalPacketsDropped);
        Assert.Equal("0.0 %", metrics.PacketDeliveryRatioText);
        Assert.Equal("0.00", metrics.AverageHopCountText);
        Assert.Equal("0.0 мс", metrics.CurrentTimeText);
        Assert.Empty(metrics.DropsByReason);
    }

    [Fact]
    public void UpdateCopiesAndFormatsSnapshotValues()
    {
        var metrics = new MetricsViewModel();

        metrics.Update(Snapshot(), 125.5);

        Assert.Equal(4, metrics.TotalPacketsSent);
        Assert.Equal(2, metrics.TotalPacketsDelivered);
        Assert.Equal(2, metrics.TotalPacketsDropped);
        Assert.Equal("50.0 %", metrics.PacketDeliveryRatioText);
        Assert.Equal("1.50", metrics.AverageHopCountText);
        Assert.Equal("125.5 мс", metrics.CurrentTimeText);
    }

    [Fact]
    public void UpdateListsDropsOrderedByReasonWithDescriptions()
    {
        var metrics = new MetricsViewModel();

        metrics.Update(Snapshot(), 0.0);

        Assert.Equal(2, metrics.DropsByReason.Count);
        DropReasonRow first = metrics.DropsByReason[0];
        Assert.Equal(PacketDropReason.NoRouteFound, first.Reason);
        Assert.Equal("немає маршруту", first.Description);
        Assert.Equal(1, first.Count);
        Assert.Equal(PacketDropReason.TtlExpired, metrics.DropsByReason[1].Reason);
    }

    [Fact]
    public void UpdateReplacesPreviousDropRows()
    {
        var metrics = new MetricsViewModel();
        metrics.Update(Snapshot(), 0.0);

        metrics.Update(new SimulationResult(1, 1, 0, 1.0, new Dictionary<PacketDropReason, int>(), Array.Empty<SimulationEvent>()), 5.0);

        Assert.Empty(metrics.DropsByReason);
        Assert.Equal("100.0 %", metrics.PacketDeliveryRatioText);
    }

    [Fact]
    public void UpdateRaisesPropertyChangedForChangedValues()
    {
        var metrics = new MetricsViewModel();

        Assert.PropertyChanged(metrics, nameof(MetricsViewModel.TotalPacketsSent), () => metrics.Update(Snapshot(), 1.0));
    }

    [Fact]
    public void UpdateFormatsNumbersWithInvariantCulture()
    {
        var metrics = new MetricsViewModel();
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            metrics.Update(Snapshot(), 1.5);

            Assert.Equal("50.0 %", metrics.PacketDeliveryRatioText);
            Assert.Equal("1.5 мс", metrics.CurrentTimeText);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void UpdateThrowsArgumentNullExceptionForNullSnapshot()
    {
        Assert.Throws<ArgumentNullException>(() => new MetricsViewModel().Update(null!, 0.0));
    }
}