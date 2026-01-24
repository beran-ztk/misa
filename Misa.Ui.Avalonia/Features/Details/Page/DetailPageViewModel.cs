using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Presentation.Mapping;
using ReactiveUI;

namespace Misa.Ui.Avalonia.Features.Details.Page;

public partial class DetailPageViewModel : ViewModelBase
{
    public IEntityDetailHost EntityDetailHost { get; }

    [ObservableProperty] private DetailedItemDto _detailedItem = new();
    [ObservableProperty] private int _selectedTabIndex;

    public Information.InformationViewModel InformationViewModel { get; }

    private CancellationTokenSource? _loadCts;

    public DetailPageViewModel(IEntityDetailHost entityDetailHost)
    {
        EntityDetailHost = entityDetailHost;
        
        InformationViewModel = new Information.InformationViewModel(this);
        
        this.WhenAnyValue(x => x.EntityDetailHost.ActiveEntityId)
            .Where(id => id != Guid.Empty)
            .DistinctUntilChanged()
            .Subscribe(id =>
            {
                _ = LoadEntityAsync(id);
            });
    }

    public async Task Reload()
    {
        await LoadEntityAsync(EntityDetailHost.ActiveEntityId);
    }
    private async Task LoadEntityAsync(Guid itemId)
    {
        if (_loadCts != null)
        {
            await _loadCts.CancelAsync();
        }
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"items/{itemId}/overview"
            );
            
            using var response = await EntityDetailHost.NavigationService.NavigationStore.MisaHttpClient
                .SendAsync(request, _loadCts.Token);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<Result<DetailedItemDto>>(cancellationToken: _loadCts.Token);

            if (result?.Value is null)
            {
                return;
            }
            
            DetailedItem = result.Value;
            InformationViewModel.Description.Load();
            await InformationViewModel.Session.LoadCurrentSessionAsync();
        }
        catch (OperationCanceledException)
        {
            // expected on fast switching
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
