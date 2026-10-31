namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Вид об'єкта на карті.
/// </summary>
public enum MapTargetKind
{
    Node = 0,
    Zone = 1
}

/// <summary>
/// Вузол або зона завад на карті (результат hit-testing і поточне виділення).
/// </summary>
public readonly record struct MapTarget(MapTargetKind Kind, string Id);