using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using Resona.Models;
using Resona.Services;

namespace Resona.Tests;

public sealed class CloudPublicLibraryClientTests
{
    [Fact]
    public async Task Profiles_are_loaded_across_all_server_pages()
    {
        var requestedUris = new List<Uri>();
        using var httpClient = new HttpClient(new StubHandler(request =>
        {
            requestedUris.Add(request.RequestUri!);
            var secondPage = request.RequestUri!.Query.Contains("offset=100", StringComparison.Ordinal);
            var count = secondPage ? 1 : 100;
            var start = secondPage ? 100 : 0;
            var profiles = Enumerable.Range(start, count)
                .Select(index => Profile(index))
                .ToList();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CloudPage<CloudPublicProfileSummary>(
                    profiles, start, 100, 101))
            };
        }));
        var client = new CloudPublicLibraryClient(httpClient, () => "https://cloud.example.test");

        var profiles = await client.GetProfilesAsync();

        Assert.Equal(101, profiles.Count);
        Assert.Equal(2, requestedUris.Count);
        Assert.Equal("/api/v1/public/profiles", requestedUris[0].AbsolutePath);
    }

    [Fact]
    public async Task Missing_profile_image_returns_null()
    {
        using var httpClient = new HttpClient(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)));
        var client = new CloudPublicLibraryClient(httpClient, () => "https://cloud.example.test/");

        var image = await client.GetProfileImageAsync(Guid.NewGuid().ToString("D"));

        Assert.Null(image);
    }

    private static CloudPublicProfileSummary Profile(int index) => new(
        Guid.NewGuid().ToString("D"),
        $"Listener {index}",
        string.Empty,
        false,
        index,
        "2026-08-13T00:00:00Z",
        "2026-08-13T00:00:00Z");

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
