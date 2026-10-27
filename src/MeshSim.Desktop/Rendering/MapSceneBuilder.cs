using System.Globalization;
using MeshSim.Core;
using MeshSim.Engine;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Перетворює топологію та (необов'язково) останню подію пакета на екранні примітиви.
/// Вузли та лінки впорядковуються за Id, тож результат детермінований.
/// </summary>
public static class MapSceneBuilder
{
    public const double NodeRadiusPx = 9.0;
    public const double MarkerRadiusPx = 15.0;
    public const double SegmentThicknessPx = 5.0;

    public static MapScene Build(NetworkTopology topology, ViewportTransform transform, PacketActivity? activity = null)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(transform);

        MeshNode[] ordered = topology.Nodes.Values
            .OrderBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();

        List<ZoneVisual> zones = [];
        foreach (JammingZone zone in topology.JammingZones)
        {
            zones.Add(new ZoneVisual(
                zone.Id,
                transform.ToScreen(zone.Center),
                transform.ScaleLength(zone.RadiusMeters),
                ZoneStyleCalculator.For(zone)));
        }

        List<NodeVisual> nodes = [];
        foreach (MeshNode node in ordered)
        {
            nodes.Add(new NodeVisual(
                node.Id,
                transform.ToScreen(node.Position),
                NodeRadiusPx,
                transform.ScaleLength(node.TransmissionRangeMeters),
                NodeStyleCalculator.For(node),
                string.Concat(node.BatteryLevelPercent.ToString("0", CultureInfo.InvariantCulture), "%")));
        }

        PacketMarkerVisual? marker = null;
        ActiveSegmentVisual? segment = null;

        if (activity is { } current && topology.Nodes.TryGetValue(current.NodeId, out MeshNode? activeNode))
        {
            RgbaColor color = MarkerStyleCalculator.ForEvent(current.EventType);
            ScreenPoint position = transform.ToScreen(activeNode.Position);

            marker = new PacketMarkerVisual(
                current.PacketId,
                BuildCaption(current),
                position,
                MarkerRadiusPx,
                current.EventType,
                color);
            segment = BuildSegment(topology, transform, current, position, color);
        }

        return new MapScene(BuildLinks(topology, transform, ordered), nodes, zones, marker, segment);
    }

    private static string BuildCaption(PacketActivity activity)
    {
        return activity.EventType == SimulationEventType.PacketDropped
            ? string.Concat(activity.PacketId, ": ", EventTextFormatter.DescribeDropReason(activity.DropReason))
            : activity.PacketId;
    }

    private static ActiveSegmentVisual? BuildSegment(
        NetworkTopology topology,
        ViewportTransform transform,
        PacketActivity activity,
        ScreenPoint end,
        RgbaColor color)
    {
        if (activity.PreviousNodeId is null
            || string.Equals(activity.PreviousNodeId, activity.NodeId, StringComparison.Ordinal)
            || !topology.Nodes.TryGetValue(activity.PreviousNodeId, out MeshNode? previous))
        {
            return null;
        }

        return new ActiveSegmentVisual(transform.ToScreen(previous.Position), end, color, SegmentThicknessPx);
    }

    private static List<LinkVisual> BuildLinks(NetworkTopology topology, ViewportTransform transform, MeshNode[] ordered)
    {
        List<LinkVisual> links = [];

        for (int i = 0; i < ordered.Length; i++)
        {
            for (int j = i + 1; j < ordered.Length; j++)
            {
                MeshNode first = ordered[i];
                MeshNode second = ordered[j];

                // Лінк показується, якщо хоча б один напрямок радіодосяжний (радіус і заряд);
                // завади лише знижують якість, а повне блокування малюється червоним пунктиром.
                if (!first.CanReach(second) && !second.CanReach(first))
                {
                    continue;
                }

                double quality = Math.Max(
                    topology.CalculateLinkQuality(first, second),
                    topology.CalculateLinkQuality(second, first));

                links.Add(new LinkVisual(
                    first.Id,
                    second.Id,
                    transform.ToScreen(first.Position),
                    transform.ToScreen(second.Position),
                    quality,
                    LinkStyleCalculator.ForQuality(quality)));
            }
        }

        return links;
    }
}