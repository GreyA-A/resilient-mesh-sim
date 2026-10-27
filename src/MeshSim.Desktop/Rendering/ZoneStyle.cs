using MeshSim.Engine;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Стиль зони радіозавад.
/// </summary>
public readonly record struct ZoneStyle(RgbaColor Fill, RgbaColor Stroke, bool IsDashed, bool IsActive);

/// <summary>
/// Обчислює стиль зони: прозорість заливки зростає з інтенсивністю, вимкнена зона сіра й пунктирна.
/// </summary>
public static class ZoneStyleCalculator
{
    public const byte MinFillAlpha = 40;
    public const byte MaxFillAlpha = 160;
    public const byte InactiveFillAlpha = 24;
    public const byte ActiveStrokeAlpha = 210;
    public const byte InactiveStrokeAlpha = 120;

    public static ZoneStyle For(JammingZone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        if (!zone.IsActive)
        {
            return new ZoneStyle(
                MapPalette.ZoneInactive.WithAlpha(InactiveFillAlpha),
                MapPalette.ZoneInactive.WithAlpha(InactiveStrokeAlpha),
                true,
                false);
        }

        double intensity = Math.Clamp(zone.JammingIntensity, 0.0, 1.0);
        byte alpha = (byte)Math.Round(MinFillAlpha + ((MaxFillAlpha - MinFillAlpha) * intensity));

        return new ZoneStyle(
            MapPalette.ZoneActive.WithAlpha(alpha),
            MapPalette.ZoneActive.WithAlpha(ActiveStrokeAlpha),
            false,
            true);
    }
}