using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Features.Entities.Features;
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
    // Descriptions
    public async Task<DescriptionDto?> CreateDescriptionAsync(DescriptionCreateDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "entities/description")
        {
            Content = JsonContent.Create(dto)
        };

        var response = await remoteProxy.SendAsync<DescriptionDto?>(request);
        return response?.Value;
    }

    public async Task UpdateDescriptionAsync(DescriptionUpdateDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "entities/description")
        {
            Content = JsonContent.Create(dto)
        };

        await remoteProxy.SendAsync(request);
    }

    public async Task DeleteDescriptionAsync(Guid descriptionId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"entities/description/{descriptionId}");
        await remoteProxy.SendAsync(request);
    }
}