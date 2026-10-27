namespace MeshSim.Desktop;

/// <summary>
/// Спільні перевірки аргументів числових параметрів.
/// </summary>
internal static class Guard
{
    internal static void RequireNonNegativeFinite(double value, string paramName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Значення має бути невід'ємним скінченним числом.");
        }
    }
}