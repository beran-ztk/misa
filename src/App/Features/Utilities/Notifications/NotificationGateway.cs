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
        bool onlyUnread = false,
        CancellationToken ct = default)
    {
        var url = $"{NotificationRoutes.GetAll}?limit={limit}";

        if (before.HasValue)
            url += $"&before={Uri.EscapeDataString(before.Value.UtcDateTime.ToString("O"))}";

        if (onlyUnread)
            url += "&unreadOnly=true";

        var response = await remoteProxy.SendAsync<List<NotificationEntryDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, url),
            retry: RetryOptions.Default,
            cancellationToken: ct);

        return response.Value;
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var response = await remoteProxy.SendAsync<NotificationUnreadCountDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, NotificationRoutes.UnreadCount),
            retry: RetryOptions.Default,
            cancellationToken: ct);

        return response.Value?.Count ?? 0;
    }

    public async Task<bool> DismissAsync(Guid id, CancellationToken ct = default)
    {
        var url = NotificationRoutes.Dismiss.Replace("{id}", id.ToString());

        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, url),
            retry: RetryOptions.None,
            cancellationToken: ct);

        return response.IsSuccess;
    }

    public async Task<bool> MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var url = NotificationRoutes.MarkRead.Replace("{id}", id.ToString());

        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, url),
            retry: RetryOptions.None,
            cancellationToken: ct);

        return response.IsSuccess;
    }

    public async Task<bool> MarkAllAsReadAsync(CancellationToken ct = default)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, NotificationRoutes.MarkAllRead),
            retry: RetryOptions.None,
            cancellationToken: ct);

        return response.IsSuccess;
    }
}
