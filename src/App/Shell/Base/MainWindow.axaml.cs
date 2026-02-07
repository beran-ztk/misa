using Avalonia.Controls;
using Avalonia.Input;

namespace Misa.Ui.Avalonia.Shell.Base;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void OverlayBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.CloseOverlayCommand.CanExecute(null))
            vm.CloseOverlayCommand.Execute(null);

        e.Handled = true;
    }
}