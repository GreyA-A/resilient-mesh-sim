namespace MeshSim.Desktop.Services;

/// <summary>
/// Показує користувачеві повідомлення про помилку (діалог, тост тощо).
/// </summary>
public interface IUserNotifier
{
    void ShowError(string title, string message);
}