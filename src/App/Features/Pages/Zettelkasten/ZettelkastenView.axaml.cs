using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class ZettelkastenView : UserControl
{
    private object? _pendingContext;

    public ZettelkastenView()
    {
        InitializeComponent();
    }

    private void Add_OnPointerPressed(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            _pendingContext = button.DataContext;
            FlyoutBase.ShowAttachedFlyout(button);
        }
    }

    private async void CreateTopicMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;

        if (_pendingContext is TopicListDto topic)
            await vm.CreateTopicAsync(topic.Id, topic.Title);
        else
            await vm.CreateTopicAsync();
    }

    private async void CreateZettelMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;

        if (_pendingContext is TopicListDto topic)
            await vm.CreateZettelAsync(topic.Id, topic.Title);
        else
            await vm.CreateZettelAsync();
    }
}
