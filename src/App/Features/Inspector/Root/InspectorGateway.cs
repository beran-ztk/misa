using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed class InspectorGateway(RemoteProxy remoteProxy)
{
    public async Task<Result<ItemDto?>> GetItemAsync(Guid id)
    {
        var response = await remoteProxy.SendAsync<ItemDto?>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, $"items/{id}/details"),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }
    
    // Archive & Delete
    public async Task<Result> ArchiveAsync(Guid itemId)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ItemRoutes.ArchiveItemRequest(itemId)),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }
    public async Task<Result> DeleteAsync(Guid itemId)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.DeleteItemRequest(itemId)),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }
    
    // Update
    public async Task<Result> UpdateTaskAsync(Guid itemId, UpdateTaskRequest dto)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, TaskRoutes.UpdateTaskRequest(itemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }
    
    // Sessions (Commands)
    public async Task<Result> StartSessionAsync(StartSessionDto dto)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ActivityRoutes.RequestStartSession(dto.ItemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }

    public async Task<Result> PauseSessionAsync(Guid itemId, PauseSessionRequest dto)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, ActivityRoutes.RequestPauseSession(itemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }

    public async Task<Result> ContinueSessionAsync(Guid itemId)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, ActivityRoutes.RequestContinueSession(itemId)),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }

    public async Task<Result> EndSessionAsync(StopSessionDto dto)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, ActivityRoutes.RequestStopSession(dto.ItemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }
    
    // Deadline
    public async Task<Result> UpsertDeadlineAsync(Guid itemId, DateTimeOffset? deadline)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ActivityRoutes.UpsertDeadlineRequest(itemId))
            {
                Content = JsonContent.Create(new UpsertDeadlineRequest(deadline))
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
