namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Колір без залежності від Avalonia (перетворюється на Color лише у візуальному шарі).
/// </summary>
public readonly record struct RgbaColor(byte R, byte G, byte B, byte A = 255)
{
    /// <summary>
    /// Лінійно змішує два кольори; amount обмежується діапазоном 0..1, NaN вважається нулем.
    /// </summary>
    public static RgbaColor Blend(RgbaColor from, RgbaColor to, double amount)
    {
        double t = double.IsNaN(amount) ? 0.0 : Math.Clamp(amount, 0.0, 1.0);

        return new RgbaColor(
            Mix(from.R, to.R, t),
            Mix(from.G, to.G, t),
            Mix(from.B, to.B, t),
            Mix(from.A, to.A, t));
    }

    public RgbaColor WithAlpha(byte alpha)
    {
        return this with { A = alpha };
    }

    private static byte Mix(byte from, byte to, double t)
    {
        return (byte)Math.Round(from + ((to - from) * t));
    }
}