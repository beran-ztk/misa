using System.Net;
using System.Net.Http;
using System.Linq;
using Resona.Services;

namespace Resona.Tests;

public sealed class TrackAnalysisServiceTests
{
    [Fact]
    public async Task CheckHealthAsync_SendsConfiguredApiKey()
    {
        var handler = new CapturingHandler("{\"status\":\"ok\"}");
        using var service = CreateService(handler, "test-key");

        var healthy = await service.CheckHealthAsync();

        Assert.True(healthy);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("test-key", handler.ApiKey);
    }

    [Fact]
    public async Task AnalyzeTrackAsync_SendsConfiguredApiKey()
    {
        var trackPath = Path.Combine(Path.GetTempPath(), $"resona-{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(trackPath, [0]);
        try
        {
            var handler = new CapturingHandler("{\"success\":true}");
            using var service = CreateService(handler, "test-key");

            var result = await service.AnalyzeTrackAsync(trackPath);

            Assert.True(result.Success);
            Assert.Equal(HttpMethod.Post, handler.Method);
            Assert.Equal("test-key", handler.ApiKey);
        }
        finally
        {
            File.Delete(trackPath);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_OmitsHeaderWhenApiKeyIsBlank()
    {
        var handler = new CapturingHandler("{\"status\":\"ok\"}");
        using var service = CreateService(handler, "  ");

        await service.CheckHealthAsync();

        Assert.Null(handler.ApiKey);
    }

    private static TrackAnalysisService CreateService(CapturingHandler handler, string? apiKey) =>
        new(
            new HttpClient(handler),
            serverUrlProvider: () => "https://analyzer.test",
            apiKeyProvider: () => apiKey);

    private sealed class CapturingHandler(string responseBody) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? ApiKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            ApiKey = request.Headers.TryGetValues("X-Api-Key", out var values)
                ? values.Single()
                : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
