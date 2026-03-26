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
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.DeleteKnowledgeSubtree)
            {
                Content = JsonContent.Create(new BatchDeleteKnowledgeSubtreeRequest(ids))
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> DeleteItemAsync(Guid itemId)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.DeleteItemRequest(itemId)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> RenameItemAsync(Guid itemId, RenameItemRequest requestBody)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ItemRoutes.RenameItemUrl(itemId))
            {
                Content = JsonContent.Create(requestBody)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> CreateZettelAsync(CreateZettelRequest requestBody)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ZettelkastenRoutes.CreateZettel)
            {
                Content = JsonContent.Create(requestBody)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> CreateTopicAsync(CreateTopicRequest requestBody)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ZettelkastenRoutes.CreateTopic)
            {
                Content = JsonContent.Create(requestBody)
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<List<KnowledgeIndexEntryDto>?> GetKnowledgeIndexAsync()
    {
        var response = await remoteProxy.SendAsync<List<KnowledgeIndexEntryDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, ZettelkastenRoutes.GetKnowledgeIndex),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);

        return response.Value;
    }

    public async Task<ZettelDto?> GetZettelAsync(Guid id)
    {
        var response = await remoteProxy.SendAsync<ZettelDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, ZettelkastenRoutes.GetZettelUrl(id)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);

        return response.Value;
    }

    public async Task<Result> UpdateZettelContentAsync(Guid id, string? content)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ZettelkastenRoutes.UpdateZettelContentUrl(id))
            {
                Content = JsonContent.Create(new UpdateZettelContentRequest(content))
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> ReparentItemAsync(Guid itemId, Guid newParentId)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ZettelkastenRoutes.ReparentKnowledgeItemUrl(itemId))
            {
                Content = JsonContent.Create(new ReparentKnowledgeItemRequest(newParentId))
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<List<DeletedKnowledgeEntryDto>?> GetDeletedKnowledgeAsync()
    {
        var response = await remoteProxy.SendAsync<List<DeletedKnowledgeEntryDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, ZettelkastenRoutes.GetDeletedKnowledgeIndex),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);

        return response.Value;
    }

    public async Task<Result> RestoreSubtreeAsync(Guid[] ids)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ItemRoutes.RestoreKnowledgeSubtree)
            {
                Content = JsonContent.Create(new RestoreKnowledgeSubtreeRequest(ids))
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> HardDeleteAsync(Guid itemId)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Delete, ItemRoutes.HardDeleteItemRequest(itemId)),
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }

    public async Task<Result> SetKnowledgeIndexExpandedStateAsync(Guid id, bool isExpanded)
    {
        return await remoteProxy.SendAsync(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Patch, ZettelkastenRoutes.SetKnowledgeIndexExpandedUrl(id))
            {
                Content = JsonContent.Create(new SetKnowledgeIndexExpandedStateRequest(isExpanded))
            },
            retry: RetryOptions.Default,
            cancellationToken: CancellationToken.None);
    }
}
