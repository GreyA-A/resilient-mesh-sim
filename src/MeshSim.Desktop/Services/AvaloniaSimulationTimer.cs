using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;

namespace MeshSim.Desktop.Services;

[ExcludeFromCodeCoverage(Justification = "Тонка обгортка над DispatcherTimer: платформний код без власної логіки, потребує UI-потоку.")]
public sealed class AvaloniaSimulationTimer : ISimulationTimer
{
    private readonly DispatcherTimer _timer = new();

    public AvaloniaSimulationTimer()
    {
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public bool IsRunning => _timer.IsEnabled;

    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public event EventHandler? Tick;

    public void Start()
    {
        _timer.Start();
    }

    public void Pause()
    {
        _timer.Stop();
    }
}