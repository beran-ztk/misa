using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed class ZettelkastenGateway(RemoteProxy remoteProxy)
{
    public async Task<Result> DeleteSubtreeAsync(Guid[] ids)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.DeleteKnowledgeSubtree)
            {
                Content = JsonContent.Create(new BatchDeleteKnowledgeSubtreeRequest(ids))
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }

    public async Task<Result> DeleteItemAsync(Guid itemId)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.DeleteItemRequest(itemId)),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }

    public async Task<Result> RenameItemAsync(Guid itemId, RenameItemRequest requestBody)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ItemRoutes.RenameItemUrl(itemId))
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
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, ZettelkastenRoutes.GetKnowledgeIndex),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response.Value;
    }

    public async Task<ZettelDto?> GetZettelAsync(Guid id)
    {
        var response = await remoteProxy.SendAsync<ZettelDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, ZettelkastenRoutes.GetZettelUrl(id)),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response.Value;
    }

    public async Task<Result> UpdateZettelContentAsync(Guid id, string? content)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ZettelkastenRoutes.UpdateZettelContentUrl(id))
            {
                Content = JsonContent.Create(new UpdateZettelContentRequest(content))
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }

    public async Task<Result> ReparentItemAsync(Guid itemId, Guid newParentId)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ZettelkastenRoutes.ReparentKnowledgeItemUrl(itemId))
            {
                Content = JsonContent.Create(new ReparentKnowledgeItemRequest(newParentId))
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }

    public async Task<Result> SetKnowledgeIndexExpandedStateAsync(Guid id, bool isExpanded)
    {
        var response = await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ZettelkastenRoutes.SetKnowledgeIndexExpandedUrl(id))
            {
                Content = JsonContent.Create(new SetKnowledgeIndexExpandedStateRequest(isExpanded))
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
