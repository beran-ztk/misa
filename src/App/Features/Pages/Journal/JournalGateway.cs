using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

public class JournalGateway(RemoteProxy remoteProxy)
{
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
    
    public async Task<IReadOnlyList<ChronicleEntryDto>> GetJournalsForMonthAsync(int year, int month)
    {
        // Use local-time month boundaries so the query matches what the user expects.
        var offset     = DateTimeOffset.Now.Offset;
        var localStart = new DateTimeOffset(year, month, 1, 0, 0, 0, offset);
        var localEnd   = localStart.AddMonths(1).AddTicks(-1);

        var fromStr = Uri.EscapeDataString(localStart.UtcDateTime.ToString("O"));
        var toStr   = Uri.EscapeDataString(localEnd.UtcDateTime.ToString("O"));
        var url     = $"{ChronicleRoutes.GetChronicle}?from={fromStr}&to={toStr}";

        var response = await remoteProxy.SendAsync<List<ChronicleEntryDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, url),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response.Value?
                   .Where(e => e.Type == ChronicleEntryType.Journal)
                   .OrderBy(e => e.At)
                   .ToList()
               ?? [];
    }
}
