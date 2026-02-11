using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Features.Entities.Features;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed class InspectorGateway(RemoteProxy remoteProxy)
{
    public async Task<DetailedItemDto?> GetDetailsAsync(Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"items/{id}/details");
        var response = await remoteProxy.SendAsync<DetailedItemDto?>(request);
        return response?.Value;
    }

    // Sessions (Overview)
    public async Task<CurrentSessionOverviewDto?> GetCurrentAndLatestSessionAsync(Guid itemId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"items/{itemId}/overview/session");
        var response = await remoteProxy.SendAsync<CurrentSessionOverviewDto?>(request);
        return response?.Value;
    }

    // Sessions (Commands)
    public async Task StartSessionAsync(Guid itemId, StartSessionDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"items/{itemId}/sessions/start")
        {
            Content = JsonContent.Create(dto)
        };

        await remoteProxy.SendAsync(request);
    }

    public async Task PauseSessionAsync(Guid itemId, PauseSessionDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"items/{itemId}/sessions/pause")
        {
            Content = JsonContent.Create(dto)
        };

        await remoteProxy.SendAsync(request);
    }

    public async Task ContinueSessionAsync(Guid itemId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"items/{itemId}/sessions/continue");
        await remoteProxy.SendAsync(request);
    }

    public async Task StopSessionAsync(Guid itemId, StopSessionDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"items/{itemId}/sessions/stop")
        {
            Content = JsonContent.Create(dto)
        };

        await remoteProxy.SendAsync(request);
    }
    public async Task UpsertDeadlineAsync(UpsertDeadlineDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "scheduling/once")
        {
            Content = JsonContent.Create(dto)
        };

        await remoteProxy.SendAsync<Result>(request);
    }

    public async Task DeleteDeadlineAsync(Guid targetItemId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"scheduling/once/{targetItemId}");
        await remoteProxy.SendAsync<Result>(request);
    }
}