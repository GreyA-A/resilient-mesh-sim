namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Готовий до малювання набір примітивів карти.
/// </summary>
public sealed class MapScene
{
    public MapScene(
        IReadOnlyList<LinkVisual> links,
        IReadOnlyList<NodeVisual> nodes,
        IReadOnlyList<ZoneVisual> zones)
    {
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(zones);

        Links = links;
        Nodes = nodes;
        Zones = zones;
    }

    public static MapScene Empty { get; } = new([], [], []);

    public IReadOnlyList<LinkVisual> Links { get; }
    public IReadOnlyList<NodeVisual> Nodes { get; }
    public IReadOnlyList<ZoneVisual> Zones { get; }
}