using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed class TaskGateway(RemoteProxy remoteProxy)
{
    public async Task<Result<List<TaskDto>>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "tasks");

        return await remoteProxy.SendAsync<List<TaskDto>>(request);
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "tasks")
        {
            Content = JsonContent.Create(dto)
        };

        return await remoteProxy.SendAsync<TaskDto>(request);
    }
}
