using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Resona.Models;

namespace Resona.Services;

public sealed record GitHubIssue(int Number, string Title, string Url, IReadOnlyList<string> Labels);

public sealed record GitHubIssuesFetchResult(
    IReadOnlyList<GitHubIssue> Issues,
    DateTimeOffset? FetchedAt,
    string? Error = null,
    bool IsCached = false);

public sealed class GitHubIssuesService
{
    private const string IssuesEndpoint = "https://api.github.com/repos/bezztk/resona/issues?state=open&per_page=100";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;
    private readonly string _cachePath;

    public static readonly GitHubIssuesService Current = new(CreateHttpClient(), Values.KnownIssuesCachePath);

    public GitHubIssuesService(HttpClient httpClient, string cachePath)
    {
        _httpClient = httpClient;
        _cachePath = cachePath;
    }

    public GitHubIssuesFetchResult LoadCached()
    {
        try
        {
            if (!File.Exists(_cachePath))
                return new GitHubIssuesFetchResult([], null, IsCached: true);

            var cache = JsonSerializer.Deserialize<KnownIssuesCache>(File.ReadAllText(_cachePath), JsonOptions);
            return cache is null
                ? new GitHubIssuesFetchResult([], null, IsCached: true)
                : new GitHubIssuesFetchResult(cache.Issues ?? [], cache.FetchedAt, IsCached: true);
        }
        catch
        {
            return new GitHubIssuesFetchResult([], null, IsCached: true);
        }
    }

    public async Task<GitHubIssuesFetchResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(IssuesEndpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var cached = LoadCached();
                return new GitHubIssuesFetchResult(
                    cached.Issues,
                    cached.FetchedAt,
                    response.StatusCode == System.Net.HttpStatusCode.NotFound
                        ? "GitHub issues are unavailable for this repository."
                        : $"GitHub returned {(int)response.StatusCode}.",
                    IsCached: true);
            }

            var payload = await response.Content.ReadFromJsonAsync<List<GitHubIssueResponse>>(JsonOptions, cancellationToken)
                          ?? [];
            var issues = payload
                .Where(issue => issue.PullRequest is null)
                .Select(issue => new GitHubIssue(
                    issue.Number,
                    issue.Title.Trim(),
                    issue.HtmlUrl,
                    issue.Labels?.Select(label => label.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList() ?? []))
                .OrderBy(issue => issue.Number)
                .ToList();
            var fetchedAt = DateTimeOffset.UtcNow;
            WriteCache(new KnownIssuesCache(fetchedAt, issues));
            return new GitHubIssuesFetchResult(issues, fetchedAt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            var cached = LoadCached();
            return cached with
            {
                Error = cached.Issues.Count > 0
                    ? "Could not refresh GitHub issues. Showing the last saved list."
                    : "Could not reach GitHub."
            };
        }
    }

    private void WriteCache(KnownIssuesCache cache)
    {
        try
        {
            var directory = Path.GetDirectoryName(_cachePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            var temporaryPath = _cachePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(cache, JsonOptions));
            File.Move(temporaryPath, _cachePath, overwrite: true);
        }
        catch
        {
            // A read-only local cache must never prevent issue retrieval.
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Resona-KnownIssues/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private sealed record KnownIssuesCache(DateTimeOffset? FetchedAt, IReadOnlyList<GitHubIssue>? Issues);

    private sealed record GitHubIssueResponse(
        int Number,
        string Title,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("pull_request")] JsonElement? PullRequest,
        IReadOnlyList<GitHubIssueLabel>? Labels);

    private sealed record GitHubIssueLabel(string Name);
}
