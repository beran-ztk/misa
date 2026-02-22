using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed class InspectorGateway(RemoteProxy remoteProxy)
{
    public Task<Result<ItemDto?>> GetItemAsync(Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"items/{id}/details");
        return remoteProxy.SendAsync<ItemDto?>(request);
    }

    // Sessions (Overview)
    public Task<Result<CurrentSessionOverviewDto>> GetCurrentAndLatestSessionAsync(Guid itemId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"items/{itemId}/overview/session");
        return remoteProxy.SendAsync<CurrentSessionOverviewDto>(request);
    }

    // Sessions (Commands)
    public Task<Result<SessionDto>> StartSessionAsync(StartSessionDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"items/{dto.ItemId}/sessions/start")
        {
            Content = JsonContent.Create(dto)
        };

        return remoteProxy.SendAsync<SessionDto>(request);
    }

    public Task<Result<SessionDto>> PauseSessionAsync(PauseSessionDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"items/{dto.ItemId}/sessions/pause")
        {
            Content = JsonContent.Create(dto)
        };

        return remoteProxy.SendAsync<SessionDto>(request);
    }

    public Task<Result> ContinueSessionAsync(Guid itemId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"items/{itemId}/sessions/continue");
        return remoteProxy.SendAsync(request);
    }

    public Task<Result> EndSessionAsync(StopSessionDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"items/{dto.ItemId}/sessions/stop")
        {
            Content = JsonContent.Create(dto)
        };

        return remoteProxy.SendAsync(request);
    }

    public Task UpsertDeadlineAsync(UpsertDeadlineDto dto)
    {
        // var request = new HttpRequestMessage(HttpMethod.Put, "deadlines")
        // {
        //     Content = JsonContent.Create(dto)
        // };
        //
        // return remoteProxy.SendAsync<DeadlineDto>(request);
        return Task.CompletedTask;
    }

    public Task<Result> DeleteDeadlineAsync(Guid targetItemId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"deadlines/{targetItemId}");
        return remoteProxy.SendAsync(request);
    }
}
