using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks;

public sealed class TaskGateway(RemoteProxy remoteProxy)
{
    public async Task<List<TaskDto>?> GetAllAsync()
    {
        var response = await remoteProxy.SendAsync<List<TaskDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, TaskRoutes.GetTasks),
            retry: new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(500) },
            cancellationToken: CancellationToken.None);
        return response.Value;
    }

    public async Task<List<TaskDto>?> GetArchivedAsync()
    {
        var response = await remoteProxy.SendAsync<List<TaskDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, TaskRoutes.GetArchivedTasks),
            retry: new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(500) },
            cancellationToken: CancellationToken.None);
        return response.Value;
    }

    public async Task<List<TaskDto>?> GetDeletedAsync()
    {
        var response = await remoteProxy.SendAsync<List<TaskDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, TaskRoutes.GetDeletedTasks),
            retry: new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(500) },
            cancellationToken: CancellationToken.None);
        return response.Value;
    }

    public async Task<Result> RestoreAsync(Guid itemId)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ItemRoutes.RestoreItemRequest(itemId)),
            retry: new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(500) },
            cancellationToken: CancellationToken.None);
        return response;
    }

    public async Task<Result> HardDeleteAsync(Guid itemId)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.HardDeleteItemRequest(itemId)),
            retry: new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(500) },
            cancellationToken: CancellationToken.None);
        return response;
    }

    public async Task<TaskDto?> GetByIdAsync(Guid itemId)
    {
        var response = await remoteProxy.SendAsync<TaskDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, TaskRoutes.GetTaskRequest(itemId)),
            retry: new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(500) },
            cancellationToken: CancellationToken.None);
        return response.Value;
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskRequest requestBody)
    {
        
        var response = await remoteProxy.SendAsync<TaskDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, TaskRoutes.CreateTask)
            {
                Content = JsonContent.Create(requestBody)
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);
        
        return response;
    }
}
