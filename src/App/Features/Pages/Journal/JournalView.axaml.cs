using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

public partial class JournalView : UserControl
{
    public JournalView()
    {
        InitializeComponent();

    }
    private void OnDayClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not JournalDayItem day)
            return;

        if (DataContext is not JournalViewModel vm)
            return;

        vm.SelectDayCommand.Execute(day);
    }
}