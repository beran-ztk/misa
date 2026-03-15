using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Notifications;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Utilities.Notifications;

public class NotificationGateway(RemoteProxy remoteProxy)
{
    public async Task<List<NotificationEntryDto>?> GetPageAsync(
        int limit = 25,
        DateTimeOffset? before = null,
        CancellationToken ct = default)
    {
        var url = $"{NotificationRoutes.GetAll}?limit={limit}";

        if (before.HasValue)
            url += $"&before={Uri.EscapeDataString(before.Value.UtcDateTime.ToString("O"))}";

        var response = await remoteProxy.SendAsync<List<NotificationEntryDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, url),
            retry: new RetryOptions { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(500) },
            cancellationToken: ct);

        return response.Value;
    }

    public async Task<bool> DismissAsync(Guid id, CancellationToken ct = default)
    {
        var url = NotificationRoutes.Dismiss.Replace("{id}", id.ToString());

        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, url),
            retry: new RetryOptions { MaxAttempts = 1, Delay = TimeSpan.FromMilliseconds(0) },
            cancellationToken: ct);

        return response.IsSuccess;
    }
}
