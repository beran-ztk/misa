using System.Net.Http;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Utilities.Dev;

public sealed class DevGateway(RemoteProxy remoteProxy)
{
    public async Task<Result> SeedDataAsync()
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, DevRoutes.SeedData),
            retry: RetryOptions.None);
    }

    public async Task<Result> SeedStressDataAsync()
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, DevRoutes.SeedStressData),
            retry: RetryOptions.None);
    }

    public async Task<Result> DeleteDataAsync()
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, DevRoutes.DeleteData),
            retry: RetryOptions.None);
    }
}
