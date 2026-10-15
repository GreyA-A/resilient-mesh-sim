using MeshSim.Core;

namespace MeshSim.Engine;

/// <summary>
/// Представляє круглу зону радіоелектронних завад (РЕБ) у симуляції.
/// </summary>
public sealed class JammingZone
{
    public string Id { get; }
    public Position Center { get; private set; }
    public double RadiusMeters { get; }
    public double JammingIntensity { get; }
    public bool IsActive { get; set; }

    public JammingZone(string id, Position center, double radiusMeters, double jammingIntensity, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Ідентифікатор зони завад не може бути порожнім.", nameof(id));
        }

        if (radiusMeters <= 0.0 || double.IsNaN(radiusMeters) || double.IsInfinity(radiusMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(radiusMeters), "Радіус зони має бути додатним скінченним числом.");
        }

        if (jammingIntensity < 0.0 || jammingIntensity > 1.0 || double.IsNaN(jammingIntensity))
        {
            throw new ArgumentOutOfRangeException(nameof(jammingIntensity), "Інтенсивність завад має бути в межах від 0.0 до 1.0.");
        }

        Id = id;
        Center = center;
        RadiusMeters = radiusMeters;
        JammingIntensity = jammingIntensity;
        IsActive = isActive;
    }

    /// <summary>
    /// Перевіряє, чи потрапляє конкретна позиція у зону дії активної завади.
    /// </summary>
    public bool Contains(Position position)
    {
        if (!IsActive)
        {
            return false;
        }

        return Center.DistanceTo(position) <= RadiusMeters;
    }

    /// <summary>
    /// Перевіряє, чи перетинає пряма лінія зв'язку між двома точками активну зону завад.
    /// </summary>
    public bool IntersectsLink(Position start, Position end)
    {
        if (!IsActive)
        {
            return false;
        }

        if (Contains(start) || Contains(end))
        {
            return true;
        }

        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double lengthSquared = (dx * dx) + (dy * dy);

        if (lengthSquared <= double.Epsilon)
        {
            return Contains(start);
        }

        double t = (((Center.X - start.X) * dx) + ((Center.Y - start.Y) * dy)) / lengthSquared;
        t = Math.Clamp(t, 0.0, 1.0);

        var nearestPoint = new Position(start.X + (t * dx), start.Y + (t * dy));
        return Center.DistanceTo(nearestPoint) <= RadiusMeters;
    }

    /// <summary>
    /// Переміщує центр зони завад у нову позицію.
    /// </summary>
    public void MoveTo(Position newCenter)
    {
        Center = newCenter;
    }
}