using System;
using System.Net.Http;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed class InspectorGateway(RemoteProxy remoteProxy, UserState userState)
{
    public async Task<DetailedItemDto?> GetDetailsAsync(Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"items/{id}/details");
        
        var response = await remoteProxy.SendAsync<DetailedItemDto?>(request);
        
        return response?.Value; 
    }
}