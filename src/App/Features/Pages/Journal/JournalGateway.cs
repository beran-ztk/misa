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
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ChronicleRoutes.CreateJournal)
            {
                Content = JsonContent.Create(requestBody)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
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
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);

        return response.Value?
                   .Where(e => e.Type == ChronicleEntryType.Journal)
                   .OrderBy(e => e.At)
                   .ToList()
               ?? [];
    }

    public async Task<Result> UpdateAsync(Guid itemId, UpdateJournalRequest requestBody)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Put, ChronicleRoutes.UpdateJournalRequest(itemId))
            {
                Content = JsonContent.Create(requestBody)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> DeleteAsync(Guid itemId)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.DeleteItemRequest(itemId)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }
}
