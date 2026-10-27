using MeshSim.Core;

namespace MeshSim.Desktop.Rendering;

/// <summary>
/// Перетворення світових координат (метри, Y вгору) в екранні (пікселі, Y вниз) із рівномірним масштабом.
/// </summary>
public sealed class ViewportTransform
{
    /// <summary>
    /// Мінімальний розмір області у світі, щоб одна точка або лінія не давали нескінченний масштаб.
    /// </summary>
    public const double MinWorldExtentMeters = 1.0;

    /// <summary>
    /// Мінімальний розмір корисної області в пікселях, щоб крихітне вікно не давало нульовий масштаб.
    /// </summary>
    public const double MinContentPixels = 1.0;

    private ViewportTransform(double scale, double offsetX, double offsetY, double viewportWidth, double viewportHeight)
    {
        Scale = scale;
        OffsetX = offsetX;
        OffsetY = offsetY;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    /// <summary>
    /// Пікселів на метр.
    /// </summary>
    public double Scale { get; }
    public double OffsetX { get; }
    public double OffsetY { get; }
    public double ViewportWidth { get; }
    public double ViewportHeight { get; }

    /// <summary>
    /// Підбирає масштаб і зсув так, щоб межі світу вмістились у вікно з відступом і були по центру.
    /// </summary>
    public static ViewportTransform Fit(WorldBounds bounds, double viewportWidth, double viewportHeight, double paddingPixels)
    {
        if (!bounds.IsFinite)
        {
            throw new ArgumentException("Межі світу мають бути скінченними числами.", nameof(bounds));
        }

        Guard.RequireNonNegativeFinite(viewportWidth, nameof(viewportWidth));
        Guard.RequireNonNegativeFinite(viewportHeight, nameof(viewportHeight));
        Guard.RequireNonNegativeFinite(paddingPixels, nameof(paddingPixels));

        double availableWidth = Math.Max(viewportWidth - (2.0 * paddingPixels), MinContentPixels);
        double availableHeight = Math.Max(viewportHeight - (2.0 * paddingPixels), MinContentPixels);
        double worldWidth = Math.Max(bounds.Width, MinWorldExtentMeters);
        double worldHeight = Math.Max(bounds.Height, MinWorldExtentMeters);

        double scale = Math.Min(availableWidth / worldWidth, availableHeight / worldHeight);
        double offsetX = (viewportWidth / 2.0) - (bounds.CenterX * scale);
        double offsetY = (viewportHeight / 2.0) + (bounds.CenterY * scale);

        return new ViewportTransform(scale, offsetX, offsetY, viewportWidth, viewportHeight);
    }

    public ScreenPoint ToScreen(Position world)
    {
        return new ScreenPoint(OffsetX + (world.X * Scale), OffsetY - (world.Y * Scale));
    }

    public Position ToWorld(ScreenPoint screen)
    {
        return new Position((screen.X - OffsetX) / Scale, (OffsetY - screen.Y) / Scale);
    }

    /// <summary>
    /// Перетворює довжину в метрах на довжину в пікселях.
    /// </summary>
    public double ScaleLength(double meters)
    {
        return meters * Scale;
    }
}