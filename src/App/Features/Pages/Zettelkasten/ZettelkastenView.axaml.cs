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

    private async void CreateTopic_OnPointerPressed(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            DataContext is ZettelkastenViewModel vm)
        {
            if (button.DataContext is TopicListDto topic)
            {
                await vm.CreateTopicAsync(topic.Id, topic.Title);
            }
            else if (button.DataContext is ZettelkastenViewModel)
            {
                await vm.CreateTopicAsync();
            }
        }
    }
}