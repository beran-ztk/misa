using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Routes;
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
    public Task<Result> StartSessionAsync(StartSessionDto dto)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            ActivityRoutes.RequestStartSession(dto.ItemId))
        {
            Content = JsonContent.Create(dto)
        };

        return remoteProxy.SendAsync(request);
    }

    public Task<Result> PauseSessionAsync(Guid itemId, PauseSessionRequest dto)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            ActivityRoutes.RequestPauseSession(itemId))
        {
            Content = JsonContent.Create(dto)
        };

        return remoteProxy.SendAsync(request);
    }

    public Task<Result> ContinueSessionAsync(Guid itemId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            ActivityRoutes.RequestContinueSession(itemId));

        return remoteProxy.SendAsync(request);
    }

    public Task<Result> EndSessionAsync(StopSessionDto dto)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            ActivityRoutes.RequestStopSession(dto.ItemId))
        {
            Content = JsonContent.Create(dto)
        };

        return remoteProxy.SendAsync(request);
    }
    
    // Deadline

    public async Task<Result> UpsertDeadlineAsync(Guid itemId, DateTimeOffset? deadline)
    {
        var dto = new UpsertDeadlineRequest(deadline);
        
        var request = new HttpRequestMessage(HttpMethod.Patch, ActivityRoutes.UpsertDeadlineRequest(itemId))
        {
            Content = JsonContent.Create(dto)
        };
        
        return await remoteProxy.SendAsync(request);
    }
}
