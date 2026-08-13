using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Resona.Models;

namespace Resona.Services;

public enum CloudSyncState { NotConfigured, Idle, Synchronizing, Succeeded, Failed }

public sealed record CloudSyncStatus(
    CloudSyncState State,
    string Message,
    int? TrackCount = null,
    string? CompletedAt = null);

public sealed class CloudLibrarySyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private int _initialized;

    public static readonly CloudLibrarySyncService Current = new(new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(2)
    });

    public CloudLibrarySyncService(HttpClient httpClient) => _httpClient = httpClient;

    public CloudSyncStatus Status { get; private set; } = new(
        CloudSyncState.NotConfigured, "Cloud server is not configured.");

    public event Action<CloudSyncStatus>? StatusChanged;

    public void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
            return;
        _ = SynchronizeAsync();
    }

    public async Task<CloudSyncStatus> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var serverUrl = AppSettingsStore.Load().CloudServerUrl?.Trim();
        if (string.IsNullOrWhiteSpace(serverUrl))
            return SetStatus(new CloudSyncStatus(
                CloudSyncState.NotConfigured, "Cloud server is not configured."));
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("http" or "https"))
            return SetStatus(new CloudSyncStatus(
                CloudSyncState.Failed, "Cloud server address must be an absolute HTTP or HTTPS URL."));

        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            SetStatus(new CloudSyncStatus(CloudSyncState.Synchronizing, "Preparing full public library snapshot…"));
            var identity = CloudIdentityStore.Current.GetOrCreate();
            var snapshot = await Task.Run(
                () => CloudLibrarySnapshotBuilder.Build(MusicLibraryService.Current, identity),
                cancellationToken);
            await UploadSnapshotAsync(baseUri, identity, snapshot, cancellationToken);

            return SetStatus(new CloudSyncStatus(
                CloudSyncState.Succeeded,
                $"Full public library synchronized · {snapshot.TrackCount} tracks",
                snapshot.TrackCount,
                DateTime.UtcNow.ToString("O")));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            WorkflowLog.Error("cloud", "Full library snapshot synchronization failed.", exception);
            return SetStatus(new CloudSyncStatus(
                CloudSyncState.Failed, $"Cloud synchronization failed: {exception.Message}"));
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public async Task UploadSnapshotAsync(
        Uri baseUri,
        CloudIdentity identity,
        CloudLibrarySnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        CloudLibrarySnapshotBuilder.Validate(snapshot, snapshot.TrackCount);
        var endpoint = new Uri(EnsureTrailingSlash(baseUri), "api/v1/library-snapshot");
        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
        {
            Content = JsonContent.Create(snapshot, options: JsonOptions)
        };
        request.Headers.Add("X-Resona-User-Id", identity.UserId);
        request.Headers.Add("X-Resona-Device-Id", identity.DeviceId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Device", identity.DeviceKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private CloudSyncStatus SetStatus(CloudSyncStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
        return status;
    }

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
}
