using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed class InspectorGateway(RemoteProxy remoteProxy)
{
    // Details
    public async Task<DetailedItemDto?> GetDetailsAsync(Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"items/{id}/details");
        var response = await remoteProxy.SendAsync<DetailedItemDto?>(request);
        return response?.Value;
    }

    // Sessions (Overview)
    public async Task<CurrentSessionOverviewDto?> GetCurrentSessionOverviewAsync(Guid itemId)
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
}