namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Варіант швидкості відтворення: підпис і множник (2.0 = вдвічі швидше).
/// </summary>
public sealed class SimulationSpeedOption
{
    public SimulationSpeedOption(string label, double multiplier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        if (!double.IsFinite(multiplier) || multiplier <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "Множник швидкості має бути додатним скінченним числом.");
        }

        Label = label;
        Multiplier = multiplier;
    }

    public string Label { get; }
    public double Multiplier { get; }

    /// <summary>
    /// Інтервал тіку для цього множника (базовий інтервал, поділений на множник; не менше одного такту).
    /// </summary>
    public TimeSpan GetInterval(TimeSpan baseInterval)
    {
        if (baseInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(baseInterval), baseInterval, "Базовий інтервал має бути додатним.");
        }

        return TimeSpan.FromTicks(Math.Max(1L, (long)Math.Round(baseInterval.Ticks / Multiplier)));
    }
}