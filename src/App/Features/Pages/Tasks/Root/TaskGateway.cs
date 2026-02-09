using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed class TaskGateway(RemoteProxy remoteProxy)
{
    public async Task<List<TaskDto>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "tasks");
        
        var response = await remoteProxy.SendAsync<List<TaskDto>?>(request);
        
        return response?.Value ?? [];
    }
    
    public async Task<TaskDto?> CreateAsync(AddTaskDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "tasks");
        request.Content = JsonContent.Create(dto);
        
        var response = await remoteProxy.SendAsync<TaskDto?>(request);
            
        return response?.Value;
    }
}