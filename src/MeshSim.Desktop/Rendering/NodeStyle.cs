using MeshSim.Core;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Стиль вузла: колір заливки, колір і частка індикатора заряду, ознака активності.
/// </summary>
public readonly record struct NodeStyle(RgbaColor Fill, RgbaColor BatteryColor, double BatteryFraction, bool IsActive);

/// <summary>
/// Обчислює стиль вузла за станом заряду батареї.
/// </summary>
public static class NodeStyleCalculator
{
    public static NodeStyle For(MeshNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        double fraction = Math.Clamp(node.BatteryLevelPercent / 100.0, 0.0, 1.0);
        RgbaColor battery = BatteryColor(fraction);

        return new NodeStyle(node.IsActive ? battery : MapPalette.InactiveNode, battery, fraction, node.IsActive);
    }

    /// <summary>
    /// Градієнт червоний, жовтий, зелений за часткою заряду (0..1).
    /// </summary>
    public static RgbaColor BatteryColor(double fraction)
    {
        return fraction <= 0.5
            ? RgbaColor.Blend(MapPalette.BatteryLow, MapPalette.BatteryMid, fraction / 0.5)
            : RgbaColor.Blend(MapPalette.BatteryMid, MapPalette.BatteryFull, (fraction - 0.5) / 0.5);
    }
}