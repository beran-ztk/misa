using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Items.Lookups;
using Misa.Contract.Main;

namespace Misa.Ui.Avalonia.Stores;

public class LookupsStore
{
    private readonly HttpClient _httpClient;
    private bool _isLoaded;
    
    public IReadOnlyList<StateDto> States { get; private set; } = [];
    public IReadOnlyList<PriorityDto> Priorities { get; private set; } = [];
    public IReadOnlyList<CategoryDto> TaskCategories { get; private set; } = [];
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

        States = dto.States;
        Priorities = dto.Priorities;
        TaskCategories = dto.TaskCategories;

        _isLoaded = true;
    }
}