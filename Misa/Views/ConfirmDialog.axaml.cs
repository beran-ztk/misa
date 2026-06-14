using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Misa.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void OnYesClicked(object? sender, RoutedEventArgs e) => Close(true);
    private void OnNoClicked(object? sender, RoutedEventArgs e) => Close(false);
}
