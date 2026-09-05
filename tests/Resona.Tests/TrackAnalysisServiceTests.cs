using System.Net;
using System.Net.Http;
using System.Linq;
using Resona.Services;

namespace Resona.Tests;

public sealed class TrackAnalysisServiceTests
{
    [Theory]
    [InlineData("http://192.168.178.102:5081/health", "http://192.168.178.102:5081")]
    [InlineData("https://analyzer.example.test/HEALTH/", "https://analyzer.example.test")]
    [InlineData("https://analyzer.example.test/prefix/", "https://analyzer.example.test/prefix")]
    public void Server_address_is_normalized(string value, string expected)
    {
        Assert.True(TrackAnalysisService.TryNormalizeServerUrl(value, out var normalized));
        Assert.Equal(expected, normalized);
    }

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

    [Fact]
    public async Task Server_error_preserves_http_status_code()
    {
        var handler = new CapturingHandler("{}", HttpStatusCode.InternalServerError);
        using var service = CreateService(handler, null);

        var exception = await Assert.ThrowsAsync<MusicAnalysisException>(() => service.CheckHealthAsync());

        Assert.Equal(MusicAnalysisErrorKind.ServerError, exception.Kind);
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
    }

    private static TrackAnalysisService CreateService(CapturingHandler handler, string? apiKey) =>
        new(
            new HttpClient(handler),
            serverUrlProvider: () => "https://analyzer.test",
            apiKeyProvider: () => apiKey);

    private sealed class CapturingHandler(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
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
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
