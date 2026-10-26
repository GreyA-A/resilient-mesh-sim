using System.Globalization;
using MeshSim.Core;
using MeshSim.Engine;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Перетворює топологію на екранні примітиви з урахуванням перетворення вікна.
/// Вузли та лінки впорядковуються за Id, тож результат детермінований.
/// </summary>
public static class MapSceneBuilder
{
    public const double NodeRadiusPx = 9.0;

    public static MapScene Build(NetworkTopology topology, ViewportTransform transform)
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

        return new MapScene(BuildLinks(topology, transform, ordered), nodes, zones);
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