namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Стан керування симуляцією.
/// </summary>
public enum SimulationState
{
    Ready = 0,
    Running = 1,
    Paused = 2,
    Finished = 3
}