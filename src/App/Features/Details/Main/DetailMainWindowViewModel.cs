using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Details.Common;
using Misa.Ui.Avalonia.Features.Details.Information.Base;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Details.Main;

public partial class DetailMainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ItemDto _item;
    [ObservableProperty] private DeadlineDto _deadline;
    [ObservableProperty] private IItemExtensionVm? _extension;

    public DetailInformationViewModel DetailInformationViewModel { get; }

    private readonly IItemDetailClient _client;
    private readonly IItemExtensionVmFactory _extensionFactory;
    public readonly INavigationService NavigationService;

    public DetailMainWindowViewModel(IItemDetailClient client, IItemExtensionVmFactory extensionFactory, INavigationService navigationService)
    {
        _client = client;
        _extensionFactory = extensionFactory;
        NavigationService = navigationService;

        Item = ItemDto.Empty();
        DetailInformationViewModel = new DetailInformationViewModel(this);
    }

    public Task ResetAsync()
    {
        Item = ItemDto.Empty();
        DetailInformationViewModel.Description.Descriptions.Clear();
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

        DetailInformationViewModel.Description.Load();
        
        await DetailInformationViewModel.Session.LoadCurrentSessionAsync();
    }
}
