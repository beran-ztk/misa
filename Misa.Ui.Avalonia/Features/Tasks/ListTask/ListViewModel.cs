using System;
using System.Collections.Generic;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Features.Tasks.Shared;
using Misa.Ui.Avalonia.Presentation.Mapping;
using ReactiveUI;
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

    public async Task LoadAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "items/tasks");
            
            using var response = await Parent.NavigationService.NavigationStore.MisaHttpClient
                .SendAsync(request, CancellationToken.None);

            response.EnsureSuccessStatusCode();
            
            var result = await response.Content
                .ReadFromJsonAsync<Result<List<ListTaskDto>>>(cancellationToken: CancellationToken.None);

            if (result is null)
            {
                return;
            }
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                Parent.Tasks.Clear();
            
                foreach (var task in result.Value ?? [])
                {
                    Parent.Tasks.Add(task);   
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}