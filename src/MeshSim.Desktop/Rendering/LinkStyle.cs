namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Стиль відображення радіоканалу.
/// </summary>
public readonly record struct LinkStyle(RgbaColor Color, double Thickness, bool IsDashed);

/// <summary>
/// Обчислює стиль лінка за якістю каналу (0.0 = заблоковано, 1.0 = чистий канал).
/// </summary>
public static class LinkStyleCalculator
{
    public const double MinThickness = 1.5;
    public const double MaxThickness = 4.0;

    public static LinkStyle ForQuality(double quality)
    {
        if (double.IsNaN(quality) || quality <= 0.0)
        {
            return new LinkStyle(MapPalette.LinkBlocked, MinThickness, true);
        }

        double clamped = Math.Min(quality, 1.0);

        return new LinkStyle(
            RgbaColor.Blend(MapPalette.LinkPoor, MapPalette.LinkGood, clamped),
            MinThickness + ((MaxThickness - MinThickness) * clamped),
            false);
    }
}