using System;
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

    private void OnDayClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not JournalDayItem day) return;
        if (DataContext is not JournalViewModel vm) return;
        vm.SelectDayCommand.Execute(day);
    }

    private void InputElement_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        if (DataContext is not JournalViewModel vm)
            return;

        if (e.KeySymbol == "[")
        {
            var text = textBox.Text ?? string.Empty;
            var caret = textBox.CaretIndex;

            // insert "]"
            textBox.Text = text.Insert(caret, "]");

            var newText = textBox.Text ?? string.Empty;

            // check if char before is "["
            if (caret > 0 && caret <= newText.Length && newText[caret - 2] == '[')
            {
                // "[" was inserted before
            }

            // Caret back between []
            textBox.CaretIndex = caret;
        }

        vm.ComposerContent = textBox.Text ?? string.Empty;
    }
}
