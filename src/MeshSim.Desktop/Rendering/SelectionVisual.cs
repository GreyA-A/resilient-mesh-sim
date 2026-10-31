namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Кільце виділення навколо вибраного вузла чи зони (екранні координати).
/// </summary>
public readonly record struct SelectionVisual(ScreenPoint Center, double RadiusPx);