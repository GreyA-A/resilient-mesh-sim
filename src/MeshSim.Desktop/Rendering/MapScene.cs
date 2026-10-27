namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Готовий до малювання набір примітивів карти, включно з маркером та активним сегментом пакета.
/// </summary>
public sealed class MapScene
{
    public MapScene(
        IReadOnlyList<LinkVisual> links,
        IReadOnlyList<NodeVisual> nodes,
        IReadOnlyList<ZoneVisual> zones,
        PacketMarkerVisual? marker = null,
        ActiveSegmentVisual? segment = null)
    {
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(zones);

        Links = links;
        Nodes = nodes;
        Zones = zones;
        Marker = marker;
        Segment = segment;
    }

    public static MapScene Empty { get; } = new([], [], []);

    public IReadOnlyList<LinkVisual> Links { get; }
    public IReadOnlyList<NodeVisual> Nodes { get; }
    public IReadOnlyList<ZoneVisual> Zones { get; }
    public PacketMarkerVisual? Marker { get; }
    public ActiveSegmentVisual? Segment { get; }
}