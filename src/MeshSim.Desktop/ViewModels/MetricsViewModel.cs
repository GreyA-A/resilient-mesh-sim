using System.Collections.ObjectModel;
using System.Globalization;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Живі метрики симуляції, що оновлюються зі знімка рушія.
/// </summary>
public sealed class MetricsViewModel : ViewModelBase
{
    private int _totalPacketsSent;
    private int _totalPacketsDelivered;
    private int _totalPacketsDropped;
    private string _packetDeliveryRatioText = "0.0 %";
    private string _averageHopCountText = "0.00";
    private string _currentTimeText = "0.0 мс";

    public int TotalPacketsSent
    {
        get => _totalPacketsSent;
        private set => SetProperty(ref _totalPacketsSent, value);
    }

    public int TotalPacketsDelivered
    {
        get => _totalPacketsDelivered;
        private set => SetProperty(ref _totalPacketsDelivered, value);
    }

    public int TotalPacketsDropped
    {
        get => _totalPacketsDropped;
        private set => SetProperty(ref _totalPacketsDropped, value);
    }

    public string PacketDeliveryRatioText
    {
        get => _packetDeliveryRatioText;
        private set => SetProperty(ref _packetDeliveryRatioText, value);
    }

    public string AverageHopCountText
    {
        get => _averageHopCountText;
        private set => SetProperty(ref _averageHopCountText, value);
    }

    public string CurrentTimeText
    {
        get => _currentTimeText;
        private set => SetProperty(ref _currentTimeText, value);
    }

    /// <summary>
    /// Втрати за причинами, упорядковані за значенням PacketDropReason.
    /// </summary>
    public ObservableCollection<DropReasonRow> DropsByReason { get; } = new();

    public void Update(SimulationResult snapshot, double currentTimeMs)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        TotalPacketsSent = snapshot.TotalPacketsSent;
        TotalPacketsDelivered = snapshot.TotalPacketsDelivered;
        TotalPacketsDropped = snapshot.TotalPacketsDropped;
        PacketDeliveryRatioText = string.Concat((snapshot.PacketDeliveryRatio * 100.0).ToString("0.0", CultureInfo.InvariantCulture), " %");
        AverageHopCountText = snapshot.AverageHopCount.ToString("0.00", CultureInfo.InvariantCulture);
        CurrentTimeText = string.Concat(currentTimeMs.ToString("0.0", CultureInfo.InvariantCulture), " мс");

        DropsByReason.Clear();
        foreach (KeyValuePair<PacketDropReason, int> pair in snapshot.DropsByReason.OrderBy(entry => (int)entry.Key))
        {
            DropsByReason.Add(new DropReasonRow(pair.Key, EventTextFormatter.DescribeDropReason(pair.Key), pair.Value));
        }
    }
}