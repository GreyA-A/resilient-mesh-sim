namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Рядок списку запланованих пакетів у редакторі.
/// </summary>
public sealed class PacketRow
{
    public PacketRow(string id, string description)
    {
        Id = id;
        Description = description;
    }

    public string Id { get; }
    public string Description { get; }
}