namespace MeshSim.Core;

/// <summary>
/// Представляє автономний вузол бездротової mesh-мережі.
/// </summary>
public sealed class MeshNode
{
    public string Id { get; }
    public Position Position { get; set; }
    public double TransmissionRangeMeters { get; }
    public double BatteryLevelPercent { get; private set; }

    public bool IsActive => BatteryLevelPercent > 0.0;

    public MeshNode(string id, Position position, double transmissionRangeMeters, double initialBatteryLevelPercent = 100.0)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Ідентифікатор вузла не може бути порожнім.", nameof(id));
        }
        if (transmissionRangeMeters <= 0.0 || double.IsNaN(transmissionRangeMeters) || double.IsInfinity(transmissionRangeMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(transmissionRangeMeters), "Радіус передачі має бути додатним дійсним числом.");
        }
        if (initialBatteryLevelPercent < 0.0 || initialBatteryLevelPercent > 100.0 || double.IsNaN(initialBatteryLevelPercent))
        {
            throw new ArgumentOutOfRangeException(nameof(initialBatteryLevelPercent), "Рівень заряду має бути в межах від 0 до 100%.");
        }
        Id = id;
        Position = position;
        TransmissionRangeMeters = transmissionRangeMeters;
        BatteryLevelPercent = initialBatteryLevelPercent;
    }

    /// <summary>
    /// Перевіряє, чи може поточний вузол передати сигнал до іншого вузла з урахуванням радіуса та заряду.
    /// </summary>
    public bool CanReach(MeshNode targetNode)
    {
        ArgumentNullException.ThrowIfNull(targetNode);

        if(!IsActive || !targetNode.IsActive)
        {
            return false;
        }
        
        if (string.Equals(Id, targetNode.Id, StringComparison.Ordinal))
        {
            return true;
        }

        double distance = Position.DistanceTo(targetNode.Position);
        return distance <= TransmissionRangeMeters;
    }

    /// <summary>
    /// Зменшує заряд батареї вузла на вказану величину.
    /// </summary>
    public void ConsumeEnergy(double amountPercent)
    {
        if (amountPercent < 0.0 || double.IsNaN(amountPercent) || double.IsInfinity(amountPercent))
        {
            throw new ArgumentOutOfRangeException(nameof(amountPercent), "Кількість витраченої енергії не може бути від'ємною або некоректною.");
        }
        
        BatteryLevelPercent = Math.Max(0.0, BatteryLevelPercent - amountPercent);
    }
}