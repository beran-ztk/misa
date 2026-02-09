using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed class TaskGateway(HttpClient httpClient)
{
    public async Task<List<TaskDto>?> GetAllTasksAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "tasks");

            using var response = await httpClient.SendAsync(request, CancellationToken.None);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<Result<List<TaskDto>>>(cancellationToken: CancellationToken.None);

            return result?.Value ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        
        return null;
    }
    
    public async Task<TaskDto?> CreateTaskAsync(AddTaskDto dto)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "tasks");
            request.Content = JsonContent.Create(dto);
            
            using var response = await httpClient.SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
        
            var createdTask = await response.Content.ReadFromJsonAsync<Result<TaskDto>>(CancellationToken.None);
            
            return createdTask?.Value;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }
}