using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace MeshSim.Desktop.Views;

[ExcludeFromCodeCoverage(Justification = "Суто візуальний код-behind вікна: InitializeComponent генерується компілятором XAML.")]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}