using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Features.Details.Common;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Details.Page;

public partial class DetailPageViewModel : ViewModelBase
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ItemDto _item;
    [ObservableProperty] private IItemExtensionVm? _extension;

    public Information.InformationViewModel InformationViewModel { get; }

    private readonly IItemDetailClient _client;
    private readonly IItemExtensionVmFactory _extensionFactory;

    public DetailPageViewModel(IItemDetailClient client, IItemExtensionVmFactory extensionFactory)
    {
        _client = client;
        _extensionFactory = extensionFactory;

        Item = ItemDto.Empty();
        InformationViewModel = new Information.InformationViewModel(this);
    }

    public Task ResetAsync()
    {
        Item = ItemDto.Empty();
        Extension = null;
        SelectedTabIndex = 0;
        return Task.CompletedTask;
    }

    public async Task LoadAsync(Guid itemId, CancellationToken ct)
    {
        Result<DetailedItemDto>? result;
        
        try
        {
            result = await _client.GetDetailsAsync(itemId, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (ct.IsCancellationRequested) return;
        
        if (result is null || !result.IsSuccess || result.Value is null)
        {
            await ResetAsync().ConfigureAwait(false);
            return;
        }

        Item = result.Value.Item;
        Extension = _extensionFactory.Create(result.Value);

        InformationViewModel.Description.Load();
        await InformationViewModel.Session.LoadCurrentSessionAsync();
    }
}
