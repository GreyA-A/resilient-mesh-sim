using MeshSim.Engine;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Рядок таблиці втрат за причинами.
/// </summary>
public readonly record struct DropReasonRow(PacketDropReason Reason, string Description, int Count);