using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed class TaskGateway(RemoteProxy remoteProxy)
{
    public async Task<List<TaskDto>?> GetAllAsync()
    {
        var response = await remoteProxy.SendAsync<List<TaskDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, TaskRoutes.GetTasks),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);
        
        return response.Value;
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskRequest requestBody)
    {
        
        var response = await remoteProxy.SendAsync<TaskDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, TaskRoutes.CreateTask),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);
        
        return response;
    }
}
