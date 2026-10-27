namespace MeshSim.Desktop.Services;

/// <summary>
/// Абстракція таймера симуляції: дає змогу тестувати Play/Pause без UI-потоку.
/// </summary>
public interface ISimulationTimer
{
    bool IsRunning { get; }
    TimeSpan Interval { get; set; }
    event EventHandler? Tick;
    void Start();
    void Pause();
}