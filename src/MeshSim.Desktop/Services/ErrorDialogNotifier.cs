using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MeshSim.Desktop.Services;

[ExcludeFromCodeCoverage(Justification = "Побудова діалогу з елементів Avalonia: суто візуальний код без розгалужень.")]
public sealed class ErrorDialogNotifier : IUserNotifier
{
    public void ShowError(string title, string message)
    {
        Dispatcher.UIThread.Post(() => ShowDialog(title, message));
    }

    private static void ShowDialog(string title, string message)
    {
        var okButton = new Button
        {
            Content = "OK",
            MinWidth = 80,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var dialog = new Window
        {
            Title = title,
            Width = 480,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    okButton
                }
            }
        };

        okButton.Click += (_, _) => dialog.Close();
        dialog.Show();
    }
}