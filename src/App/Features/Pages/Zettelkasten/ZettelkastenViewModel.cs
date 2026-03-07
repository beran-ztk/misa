using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelkastenViewModel(ZettelkastenGateway gateway) : ViewModelBase
{
    [ObservableProperty] private string? _topicTitle;
    public async Task InitializeWorkspaceAsync()
    {
        
    }

    [RelayCommand]
    private async Task CreateTopicAsync()
    {
        if (TopicTitle is null) return;
        var request = new CreateTopicRequest(TopicTitle, null);
        await gateway.CreateTopicAsync(request);
    }
}