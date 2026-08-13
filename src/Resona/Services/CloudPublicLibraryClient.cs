using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Resona.Models;

namespace Resona.Services;

public sealed class CloudPublicLibraryClient
{
    private readonly HttpClient _httpClient;
    private readonly Func<string?> _serverUrlProvider;

    public static readonly CloudPublicLibraryClient Current = new(new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    });

    public CloudPublicLibraryClient(HttpClient httpClient, Func<string?>? serverUrlProvider = null)
    {
        _httpClient = httpClient;
        _serverUrlProvider = serverUrlProvider
                             ?? (() => AppSettingsStore.Load().CloudServerUrl);
    }

    public async Task<IReadOnlyList<CloudPublicProfileSummary>> GetProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        var profiles = new List<CloudPublicProfileSummary>();
        for (var offset = 0;; offset += PublicPageSize)
        {
            var page = await GetAsync<CloudPage<CloudPublicProfileSummary>>(
                $"api/v1/public/profiles?offset={offset}&limit={PublicPageSize}", cancellationToken);
            profiles.AddRange(page.Items);
            if (profiles.Count >= page.Total || page.Items.Count == 0)
                return profiles;
        }
    }

    public Task<CloudPublicProfileSummary> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        GetAsync<CloudPublicProfileSummary>(
            $"api/v1/public/profiles/{Uri.EscapeDataString(userId)}", cancellationToken);

    public async Task<IReadOnlyList<CloudPublicLibraryTrack>> GetTracksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var tracks = new List<CloudPublicLibraryTrack>();
        var escapedUserId = Uri.EscapeDataString(userId);
        for (var offset = 0;; offset += PublicPageSize)
        {
            var page = await GetAsync<CloudPage<CloudPublicLibraryTrack>>(
                $"api/v1/public/profiles/{escapedUserId}/tracks?offset={offset}&limit={PublicPageSize}",
                cancellationToken);
            tracks.AddRange(page.Items);
            if (tracks.Count >= page.Total || page.Items.Count == 0)
                return tracks;
        }
    }

    public async Task<byte[]?> GetProfileImageAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = BuildUri($"api/v1/public/profiles/{Uri.EscapeDataString(userId)}/image");
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<T> GetAsync<T>(string relativePath, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(BuildUri(relativePath), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Cloud server returned an empty response.");
    }

    private Uri BuildUri(string relativePath)
    {
        var serverUrl = _serverUrlProvider()?.Trim();
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new InvalidOperationException("Cloud server is not configured in Settings > Profile.");
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Cloud server address is invalid.");

        var normalizedBase = baseUri.AbsoluteUri.EndsWith('/')
            ? baseUri
            : new Uri(baseUri.AbsoluteUri + "/");
        return new Uri(normalizedBase, relativePath);
    }

    private const int PublicPageSize = 100;
}
