using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Shared.Results;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;

public interface IItemDetailClient
{
    Task<Result<DetailedItemDto>?> GetDetailsAsync(Guid id, CancellationToken  cancellationToken);
}

public sealed class ItemDetailClient(HttpClient httpClient) : IItemDetailClient
{
    public async Task<Result<DetailedItemDto>?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"items/{id}/details");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<Result<DetailedItemDto>>(cancellationToken); 
    }
}