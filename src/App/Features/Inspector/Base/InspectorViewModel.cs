using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Common;
using Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Base;
using Misa.Ui.Avalonia.Infrastructure.Navigation;

namespace Misa.Ui.Avalonia.Features.Inspector.Base;

public partial class InspectorViewModel : ViewModelBase
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ItemDto _item;
    [ObservableProperty] private DeadlineDto _deadline;
    [ObservableProperty] private IItemExtensionVm? _extension;

    public InspectorOverViewModel InspectorOverViewModel { get; }

    private readonly IInspectorClient _client;
    private readonly IInspectorItemExtensionVmFactory _extensionFactory;
    public readonly INavigationService NavigationService;

    public InspectorViewModel(IInspectorClient client, IInspectorItemExtensionVmFactory extensionFactory, INavigationService navigationService)
    {
        _client = client;
        _extensionFactory = extensionFactory;
        NavigationService = navigationService;

        Item = ItemDto.Empty();
        InspectorOverViewModel = new InspectorOverViewModel(this);
    }

    public Task ResetAsync()
    {
        Item = ItemDto.Empty();
        InspectorOverViewModel.Description.Descriptions.Clear();
        Extension = null;
        SelectedTabIndex = 0;
        return Task.CompletedTask;
    }

    public async Task Reload()
    {
        await LoadAsync(Item.Id, CancellationToken.None);
    }
    public async Task LoadAsync(Guid itemId, CancellationToken ct)
    {
        Result<DetailedItemDto>? result;
        
        try
        {
            result = await _client.GetDetailsAsync(itemId, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        
        if (result is null || !result.IsSuccess || result.Value is null)
        {
            await ResetAsync().ConfigureAwait(false);
            return;
        }
        
        Item = result.Value.Item;
        Deadline = result.Value.Deadline;
        Extension = _extensionFactory.Create(result.Value);

        InspectorOverViewModel.Description.Load();
        
        await InspectorOverViewModel.Session.LoadCurrentSessionAsync();
    }
}
