using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed class TaskGateway(RemoteProxy remoteProxy)
{
    public async Task<List<TaskDto>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, TaskRoutes.GetTasks);

        var response = await remoteProxy.SendAsync<List<TaskDto>>(request);
        return response.Value
               ?? throw new Exception("No Data");
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskRequest requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, TaskRoutes.CreateTask)
        {
            Content = JsonContent.Create(requestBody)
        };

        return await remoteProxy.SendAsync<TaskDto>(request);
    }
}
