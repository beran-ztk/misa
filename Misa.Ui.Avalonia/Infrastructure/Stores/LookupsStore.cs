using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Audit.Lookups;
using Misa.Contract.Items.Lookups;
using Misa.Contract.Main;

namespace Misa.Ui.Avalonia.Stores;

public class LookupsStore
{
    private readonly HttpClient _httpClient;
    private bool _isLoaded;
    
    public IReadOnlyList<PriorityDto> Priorities { get; private set; } = [];
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

        Priorities = dto.Priorities;
        TaskCategories = dto.TaskCategories;
        EfficiencyTypes = dto.EfficiencyTypes;
        ConcentrationTypes = dto.ConcentrationTypes;

        _isLoaded = true;
    }
}