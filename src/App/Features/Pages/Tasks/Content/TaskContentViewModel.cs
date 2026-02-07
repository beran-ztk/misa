using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using TaskMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.Main.TaskMainWindowViewModel;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Content;

public class TaskContentViewModel : ViewModelBase
{
    public TaskMainWindowViewModel Parent { get; }

    public TaskContentViewModel(TaskMainWindowViewModel vm)
    {
        Parent = vm;
        _ = LoadAsync();
    }
    public async Task LoadAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "tasks");
            
            using var response = await Parent.NavigationService.NavigationStore.HttpClient
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