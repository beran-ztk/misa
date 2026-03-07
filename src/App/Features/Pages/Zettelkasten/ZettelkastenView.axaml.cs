using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class ZettelkastenView : UserControl
{
    public ZettelkastenView()
    {
        InitializeComponent();
    }

    private async void InputElement_OnPointerPressed(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.DataContext is TopicListDto topic &&
            DataContext is ZettelkastenViewModel vm)
        {
            await vm.CreateTopicAsync(topic.Id);
        }
    }
}