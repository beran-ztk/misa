using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Features;
using Misa.Ui.Avalonia.Presentation.Mapping;
using ReactiveUI;

namespace Misa.Ui.Avalonia.Features.Details.Information.Extensions.Descriptions;

public partial class DescriptionViewModel(InformationViewModel parent) : ViewModelBase
{
    public void Load()
    {
        Descriptions.Clear();
        Parent.Parent.Item.Entity.Descriptions
            .ToList()
            .ForEach(d => Descriptions.Add(d));
    }
    public ObservableCollection<DescriptionDto> Descriptions { get; } = [];
    [ObservableProperty] private string _description = string.Empty;
    public bool IsDescriptionValid => !string.IsNullOrWhiteSpace(Description); 
    partial void OnDescriptionChanged(string value)
    {
        OnPropertyChanged(nameof(IsDescriptionValid));
    }
    public InformationViewModel Parent { get; } = parent;

    [RelayCommand]
    private async Task AddDescription()
    {
        // try
        // {
        //     var dto = new DescriptionCreateDto(
        //         Parent.Parent.Item.Id,
        //         Description.Trim()
        //     );
        //
        //     using var request = new HttpRequestMessage(
        //         HttpMethod.Post,
        //         "entities/description"
        //     );
        //     request.Content = JsonContent.Create(dto);
        //
        //     var response = await Parent.Parent.EntityDetailHost.NavigationService.NavigationStore
        //         .MisaHttpClient
        //         .SendAsync(request);
        //
        //     response.EnsureSuccessStatusCode();
        //
        //     var result = await response.Content
        //         .ReadFromJsonAsync<Result<DescriptionDto>>();
        //
        //     if (result?.Value is null)
        //     {
        //         return;
        //     }
        //
        //     Descriptions.Add(result.Value);
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e);
        // }
        // finally
        // {
        //     Description = string.Empty;
        // }
    }
}