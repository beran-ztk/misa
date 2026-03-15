using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle;

public class ChronicleGateway(RemoteProxy remoteProxy)
{
    public async Task<List<ChronicleEntryDto>?> GetChronicleAsync(DateTimeOffset from, DateTimeOffset to)
    {
        var fromStr = Uri.EscapeDataString(from.UtcDateTime.ToString("O"));
        var toStr   = Uri.EscapeDataString(to.UtcDateTime.ToString("O"));
        var url     = $"{ChronicleRoutes.GetChronicle}?from={fromStr}&to={toStr}";

        var response = await remoteProxy.SendAsync<List<ChronicleEntryDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, url),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response.Value;
    }

    public async Task<Result> CreateAsync(CreateJournalRequest requestBody)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ChronicleRoutes.CreateJournal)
            {
                Content = JsonContent.Create(requestBody)
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }
}
