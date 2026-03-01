using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schola;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Schola;

public sealed class ScholaGateway(RemoteProxy remoteProxy)
{
    public async Task<Result> CreateArcAsync(CreateArcRequest requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ScholaRoutes.CreateArc)
        {
            Content = JsonContent.Create(requestBody)
        };

        return await remoteProxy.SendAsync(request);
    }
    public async Task<Result> CreateUnitAsync(CreateUnitRequest requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ScholaRoutes.CreateUnit)
        {
            Content = JsonContent.Create(requestBody)
        };

        return await remoteProxy.SendAsync(request);
    }
    
    public async Task<Result<ScholaDto>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ScholaRoutes.GetSchola);

        return await remoteProxy.SendAsync<ScholaDto>(request);
    }
}
