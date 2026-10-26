using MeshSim.Core;
using MeshSim.Engine;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Прямокутні межі області у світових координатах (метри).
/// </summary>
public readonly record struct WorldBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public static WorldBounds Default => new(0.0, 0.0, 100.0, 100.0);

    public double Width => MaxX - MinX;

    public double Height => MaxY - MinY;

    public double CenterX => (MinX + MaxX) / 2.0;

    public double CenterY => (MinY + MaxY) / 2.0;

    public bool IsFinite =>
        double.IsFinite(MinX) && double.IsFinite(MinY) && double.IsFinite(MaxX) && double.IsFinite(MaxY);

    /// <summary>
    /// Межі, що охоплюють усі вузли та всі зони завад (разом із їхніми радіусами).
    /// Для порожньої топології повертає <see cref="Default"/>.
    /// </summary>
    public static WorldBounds FromTopology(NetworkTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;
        bool any = false;

        foreach (MeshNode node in topology.Nodes.Values)
        {
            minX = Math.Min(minX, node.Position.X);
            minY = Math.Min(minY, node.Position.Y);
            maxX = Math.Max(maxX, node.Position.X);
            maxY = Math.Max(maxY, node.Position.Y);
            any = true;
        }

        foreach (JammingZone zone in topology.JammingZones)
        {
            minX = Math.Min(minX, zone.Center.X - zone.RadiusMeters);
            minY = Math.Min(minY, zone.Center.Y - zone.RadiusMeters);
            maxX = Math.Max(maxX, zone.Center.X + zone.RadiusMeters);
            maxY = Math.Max(maxY, zone.Center.Y + zone.RadiusMeters);
            any = true;
        }

        return any ? new WorldBounds(minX, minY, maxX, maxY) : Default;
    }
}