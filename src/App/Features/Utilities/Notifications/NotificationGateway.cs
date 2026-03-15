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
    public async Task<List<NotificationEntryDto>?> GetAllAsync(CancellationToken ct = default)
    {
        var response = await remoteProxy.SendAsync<List<NotificationEntryDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, NotificationRoutes.GetAll),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: ct);

        return response.Value;
    }
}
