using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schola;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed class ZettelkastenGateway(RemoteProxy remoteProxy)
{
    public async Task<Result> CreateZettelAsync(CreateZettelRequest requestBody)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ZettelkastenRoutes.CreateZettel)
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

    public async Task<Result> CreateTopicAsync(CreateTopicRequest requestBody)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ZettelkastenRoutes.CreateTopic)
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

    public async Task<List<KnowledgeIndexEntryDto>?> GetKnowledgeIndexAsync()
    {
        var response = await remoteProxy.SendAsync<List<KnowledgeIndexEntryDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, ZettelkastenRoutes.GetTopics),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response.Value;
    }

    public async Task<List<ZettelDto>?> GetZettelsAsync()
    {
        var response = await remoteProxy.SendAsync<List<ZettelDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, ZettelkastenRoutes.GetZettels),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response.Value;
    }
    
    // Schola
    public async Task<Result> CreateArcAsync(CreateArcRequest requestBody)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ScholaRoutes.CreateArc)
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
    public async Task<Result> CreateUnitAsync(CreateUnitRequest requestBody)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ScholaRoutes.CreateUnit)
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
