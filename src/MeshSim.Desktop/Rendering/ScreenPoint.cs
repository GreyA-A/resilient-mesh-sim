namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Точка в екранних координатах (пікселі, вісь Y спрямована вниз).
/// </summary>
public readonly record struct ScreenPoint(double X, double Y);