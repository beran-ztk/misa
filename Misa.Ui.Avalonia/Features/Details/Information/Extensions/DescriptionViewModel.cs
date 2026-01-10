using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Details.Information.Extensions;

public partial class DescriptionViewModel(InformationViewModel parent) : ViewModelBase
{
    [ObservableProperty] private string _description;
    public InformationViewModel Parent { get; } = parent;

    private async Task AddDescriptionAsync()
    {
        // try
        // {
        //     var trimmedDescription = Description?.Trim();
        //
        //     if (string.IsNullOrWhiteSpace(trimmedDescription))
        //     {
        //         return;
        //     }
        //     
        //     var dto = new DescriptionResolvedDto()
        //     {
        //         EntityId = Parent.Parent.ItemOverview.Item.Id, 
        //         Content = trimmedDescription
        //     };
        //     
        //     var response = await Parent.Parent.EntityDetailHost.NavigationService.NavigationStore
        //         .MisaHttpClient.PostAsJsonAsync(requestUri: "api/descriptions", dto);
        //     
        //     if (!response.IsSuccessStatusCode)
        //     {
        //         var body = await response.Content.ReadAsStringAsync();
        //         Console.WriteLine($"Server returned {response.StatusCode}: {body}");
        //     }
        //     else
        //     {
        //         await Parent.Parent.Reload();
        //     }
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e);
        // }
    }
}