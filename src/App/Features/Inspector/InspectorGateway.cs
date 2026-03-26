using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Items.Components.Relations;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Inspector;

public sealed class InspectorGateway(RemoteProxy remoteProxy)
{
    public async Task<Result<ItemDto?>> GetItemAsync(Guid id)
    {
        return await remoteProxy.SendAsync<ItemDto?>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, $"items/{id}/details"),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    // Archive & Delete
    public async Task<Result> ArchiveAsync(Guid itemId)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ItemRoutes.ArchiveItemRequest(itemId)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> DeleteAsync(Guid itemId)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.DeleteItemRequest(itemId)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    // Update
    public async Task<Result> UpdateTaskAsync(Guid itemId, UpdateTaskRequest dto)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, TaskRoutes.UpdateTaskRequest(itemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> UpdateScheduleAsync(Guid itemId, UpdateScheduleRequest dto)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, ScheduleRoutes.UpdateScheduleRequest(itemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    // Sessions
    public async Task<Result> StartSessionAsync(StartSessionDto dto)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ActivityRoutes.RequestStartSession(dto.ItemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> PauseSessionAsync(Guid itemId, PauseSessionRequest dto)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, ActivityRoutes.RequestPauseSession(itemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> ContinueSessionAsync(Guid itemId)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, ActivityRoutes.RequestContinueSession(itemId)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> EndSessionAsync(StopSessionDto dto)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, ActivityRoutes.RequestStopSession(dto.ItemId))
            {
                Content = JsonContent.Create(dto)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    // Activity State
    public async Task<Result> ChangeActivityStateAsync(Guid itemId, ChangeActivityStateRequest request)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ActivityRoutes.ChangeStateRequest(itemId))
            {
                Content = JsonContent.Create(request)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    // Deadline
    public async Task<Result> UpsertDeadlineAsync(Guid itemId, DateTimeOffset? deadline)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ActivityRoutes.UpsertDeadlineRequest(itemId))
            {
                Content = JsonContent.Create(new UpsertDeadlineRequest(deadline))
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    // Relations
    public async Task<Result<List<ItemRelationDto>?>> GetRelationsAsync(Guid itemId)
    {
        return await remoteProxy.SendAsync<List<ItemRelationDto>?>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, RelationRoutes.GetRelationsForItemUrl(itemId)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result<List<ItemLookupDto>?>> GetItemsForLookupAsync()
    {
        return await remoteProxy.SendAsync<List<ItemLookupDto>?>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, RelationRoutes.GetItemsForLookup),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> CreateRelationAsync(CreateRelationRequest request)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, RelationRoutes.CreateRelation)
            {
                Content = JsonContent.Create(request)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> UpdateRelationAsync(Guid relationId, UpdateRelationRequest request)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, RelationRoutes.UpdateRelationUrl(relationId))
            {
                Content = JsonContent.Create(request)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> DeleteRelationAsync(Guid relationId)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, RelationRoutes.DeleteRelationUrl(relationId)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }
}
