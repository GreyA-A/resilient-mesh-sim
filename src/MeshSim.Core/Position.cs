namespace MeshSim.Core;

/// <summary>
/// Представляє двовимірні просторові координати вузла в симуляції.
/// Використовує стек для збереження пам'яті (ADR-005).
/// </summary>
public readonly record struct Position
{
    public double X { get; }
    public double Y { get; }

    public Position(double x, double y)
    {
        if (double.IsNaN(x) || double.IsInfinity(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Координата X має бути скінченним дійсним числом.");
        }

        if (double.IsNaN(y) || double.IsInfinity(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Координата Y має бути скінченним дійсним числом.");
        }

        X = x;
        Y = y;
    }

    /// <summary>
    /// Розраховує евклідову відстань до іншої точки.
    /// </summary>
    public double DistanceTo(Position other)
    {
        double deltaX = other.X - X;
        double deltaY = other.Y - Y;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }
}