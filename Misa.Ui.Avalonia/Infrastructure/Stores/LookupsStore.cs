using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Features.Lookups;

namespace Misa.Ui.Avalonia.Infrastructure.Stores;

public class LookupsStore
{
    private readonly HttpClient _httpClient;
    private bool _isLoaded;
    
    public IReadOnlyList<CategoryDto> TaskCategories { get; private set; } = [];
    public IReadOnlyList<SessionEfficiencyTypeDto> EfficiencyTypes { get; private set; } = [];
    public IReadOnlyList<SessionConcentrationTypeDto> ConcentrationTypes { get; private set; } = [];
    public LookupsStore(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _ = EnsureLoadedAsync();
    }
    public async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        var dto = await _httpClient.GetFromJsonAsync<LookupsDto>("api/lookups");
        if (dto is null) return;

        TaskCategories = dto.TaskCategories;
        EfficiencyTypes = dto.EfficiencyTypes;
        ConcentrationTypes = dto.ConcentrationTypes;

        _isLoaded = true;
    }
}