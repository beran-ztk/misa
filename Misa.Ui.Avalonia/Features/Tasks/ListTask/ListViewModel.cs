using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Presentation.Mapping;
using PageViewModel = Misa.Ui.Avalonia.Features.Tasks.Page.PageViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.ListTask;

public class ListViewModel : ViewModelBase
{
    public PageViewModel Parent { get; }

    public ListViewModel(PageViewModel vm)
    {
        Parent = vm;
        _ = LoadAsync();
    }
    private async Task LoadAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "tasks");
            
            using var response = await Parent.NavigationService.NavigationStore.MisaHttpClient
                .SendAsync(request, CancellationToken.None);

            response.EnsureSuccessStatusCode();
            
            var result = await response.Content
                .ReadFromJsonAsync<Result<List<TaskDto>>>(cancellationToken: CancellationToken.None);

            Parent.Tasks.Clear();
            await Parent.AddToCollection(result?.Value ?? []);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}