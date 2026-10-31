namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Готовий до малювання набір примітивів карти: вузли, лінки, зони, маркер і сегмент пакета, кільце виділення.
/// </summary>
public sealed class MapScene
{
    public MapScene(
        IReadOnlyList<LinkVisual> links,
        IReadOnlyList<NodeVisual> nodes,
        IReadOnlyList<ZoneVisual> zones,
        PacketMarkerVisual? marker = null,
        ActiveSegmentVisual? segment = null,
        SelectionVisual? selection = null)
    {
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(zones);

        Links = links;
        Nodes = nodes;
        Zones = zones;
        Marker = marker;
        Segment = segment;
        Selection = selection;
    }

    public static MapScene Empty { get; } = new([], [], []);

    public IReadOnlyList<LinkVisual> Links { get; }
    public IReadOnlyList<NodeVisual> Nodes { get; }
    public IReadOnlyList<ZoneVisual> Zones { get; }
    public PacketMarkerVisual? Marker { get; }
    public ActiveSegmentVisual? Segment { get; }
    public SelectionVisual? Selection { get; }
}