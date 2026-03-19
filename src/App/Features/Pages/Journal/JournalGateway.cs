using System;
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
}
