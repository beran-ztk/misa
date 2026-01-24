using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Features.Details.Common;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Details.Page;

public partial class DetailPageViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ItemDto _item;
    [ObservableProperty] private IItemExtensionVm? _extension;

    public IEntityDetailHost EntityDetailHost { get; }
    public Information.InformationViewModel InformationViewModel { get; }

    private readonly IItemExtensionVmFactory _extensionFactory;
    private CancellationTokenSource? _loadCts;

    public DetailPageViewModel(IEntityDetailHost entityDetailHost, IItemExtensionVmFactory extensionFactory)
    {
        EntityDetailHost = entityDetailHost;
        _extensionFactory = extensionFactory;

        Item = ItemDto.Empty();
        InformationViewModel = new Information.InformationViewModel(this);
    }

    public Task Reload() => LoadAsync(EntityDetailHost.ActiveEntityId);

    public async Task LoadAsync(Guid itemId)
    {
        CancelLoad();

        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            var http = EntityDetailHost.NavigationService.NavigationStore.MisaHttpClient;

            using var request = new HttpRequestMessage(HttpMethod.Get, $"items/{itemId}/details");
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Result<DetailedItemDto>>(cancellationToken: ct);

            if (result is null || !result.IsSuccess || result.Value is null)
            {
                Item = ItemDto.Empty();
                Extension = null;
                return;
            }

            Item = result.Value.Item;
            Extension = _extensionFactory.Create(result.Value);

            InformationViewModel.Description.Load();
            await InformationViewModel.Session.LoadCurrentSessionAsync();
        }
        catch (OperationCanceledException)
        {
            /* ignore */
        }
    }

    private void CancelLoad()
    {
        if (_loadCts is null) return;

        try { _loadCts.Cancel(); } catch { /* ignore */ }
        _loadCts.Dispose();
        _loadCts = null;
    }

    public void Dispose() => CancelLoad();
}
