namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Визначає, який об'єкт карти розташований під екранною точкою. Вузли мають пріоритет над зонами.
/// Серед вузлів обирається найближчий до точки, серед зон найменша за радіусом із тих, що містять точку
/// (тому маленька зона всередині великої лишається доступною).
/// </summary>
public static class MapHitTester
{
    public static MapTarget? Hit(MapScene scene, ScreenPoint point)
    {
        ArgumentNullException.ThrowIfNull(scene);

        string? nodeId = null;
        double nodeDistance = double.PositiveInfinity;

        foreach (NodeVisual node in scene.Nodes)
        {
            double distance = Distance(node.Center, point);

            if (distance <= node.RadiusPx && distance < nodeDistance)
            {
                nodeId = node.Id;
                nodeDistance = distance;
            }
        }

        if (nodeId is not null)
        {
            return new MapTarget(MapTargetKind.Node, nodeId);
        }

        string? zoneId = null;
        double zoneRadius = double.PositiveInfinity;

        foreach (ZoneVisual zone in scene.Zones)
        {
            if (Distance(zone.Center, point) <= zone.RadiusPx && zone.RadiusPx < zoneRadius)
            {
                zoneId = zone.Id;
                zoneRadius = zone.RadiusPx;
            }
        }

        return zoneId is null ? null : new MapTarget(MapTargetKind.Zone, zoneId);
    }

    private static double Distance(ScreenPoint first, ScreenPoint second)
    {
        double deltaX = first.X - second.X;
        double deltaY = first.Y - second.Y;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }
}