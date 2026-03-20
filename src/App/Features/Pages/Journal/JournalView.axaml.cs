using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

public partial class JournalView : UserControl
{
    public JournalView()
    {
        InitializeComponent();
    }

    // ── DataContext wiring ────────────────────────────────────────────────────

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is JournalViewModel vm)
            vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(JournalViewModel.IsComposerOpen)
            && DataContext is JournalViewModel { IsComposerOpen: true })
        {
            // Focus the content box when the composer opens.
            ComposerContentBox.Focus();
        }
    }

    // ── Calendar day click ────────────────────────────────────────────────────

    private void OnDayClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not JournalDayItem day) return;
        if (DataContext is not JournalViewModel vm) return;
        vm.SelectDayCommand.Execute(day);
    }

    // ── Composer keyboard handling ────────────────────────────────────────────

    private void OnComposerTitleKeyDown(object? sender, KeyEventArgs e)
    {
        // Enter in title field → move focus to content.
        if (e.Key == Key.Enter)
        {
            ComposerContentBox.Focus();
            e.Handled = true;
        }
        // Escape → close composer.
        if (e.Key == Key.Escape)
        {
            CloseComposerViaKey();
            e.Handled = true;
        }
    }

    private void OnComposerContentKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+Enter → submit.
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.Control)
        {
            if (DataContext is JournalViewModel vm && vm.SubmitComposerCommand.CanExecute(null))
                vm.SubmitComposerCommand.Execute(null);
            e.Handled = true;
        }
        // Escape → close composer.
        if (e.Key == Key.Escape)
        {
            CloseComposerViaKey();
            e.Handled = true;
        }
    }

    private void CloseComposerViaKey()
    {
        if (DataContext is JournalViewModel vm)
            vm.CloseComposerCommand.Execute(null);
    }
}
