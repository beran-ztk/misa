using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resona.Services;

/// <summary>
/// Owns the process-wide application mutex and forwards later launch attempts to
/// the primary process. It deliberately starts before Avalonia and all workers.
/// </summary>
public sealed class SingleInstanceCoordinator : IDisposable
{
    private const int ActivationRetryCount = 20;
    private static readonly TimeSpan ActivationRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _listenerCancellation = new();
    private readonly object _activationGate = new();
    private Task? _listenerTask;
    private Action? _activationHandler;
    private bool _activationPending;
    private bool _ownsMutex;
    private bool _disposed;

    private SingleInstanceCoordinator(string applicationId, string? identity = null)
    {
        var instanceKey = BuildInstanceKey(applicationId, identity ?? CurrentUserIdentity());
        var mutexName = OperatingSystem.IsWindows()
            ? $"Local\\{instanceKey}"
            : instanceKey;
        _pipeName = $"{instanceKey}.activation";
        // A named mutex kernel object disappears when the last process handle is
        // closed, including after a crash. createdNew is therefore the atomic,
        // process-safe ownership decision without a stale lock file to clean up.
        _mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        _ownsMutex = createdNew;

        if (_ownsMutex)
            _listenerTask = Task.Run(ListenForActivationAsync);
    }

    public bool IsPrimary => _ownsMutex;

    public static SingleInstanceCoordinator Start(
        string applicationId = "Beran.Resona",
        string? identity = null) =>
        new(applicationId, identity);

    public void SetActivationHandler(Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var invokePending = false;
        lock (_activationGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _activationHandler = handler;
            invokePending = _activationPending;
            _activationPending = false;
        }

        if (invokePending)
            InvokeActivationHandler(handler);
    }

    public async Task<bool> NotifyPrimaryAsync(CancellationToken cancellationToken = default)
    {
        if (IsPrimary)
            return false;

        // The newly launched process is usually allowed to transfer foreground
        // activation. Grant that permission before asking the primary to focus.
        if (OperatingSystem.IsWindows())
            AllowSetForegroundWindow(uint.MaxValue); // ASFW_ANY

        for (var attempt = 0; attempt < ActivationRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var client = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ActivationRetryDelay);
                await client.ConnectAsync(timeout.Token);
                await client.WriteAsync(new byte[] { 1 }, cancellationToken);
                await client.FlushAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The primary may still be creating its listener. Retry briefly.
            }
            catch (IOException)
            {
                // A listener can rotate between connections; retry on the next slot.
            }

            await Task.Delay(ActivationRetryDelay, cancellationToken);
        }

        return false;
    }

    private async Task ListenForActivationAsync()
    {
        var cancellationToken = _listenerCancellation.Token;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(cancellationToken);
                var signal = new byte[1];
                if (await server.ReadAsync(signal, cancellationToken) > 0)
                    OnActivationRequested();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                WorkflowLog.Error("single-instance", "Activation listener failed; restarting it.", exception);
                try
                {
                    await Task.Delay(ActivationRetryDelay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    private void OnActivationRequested()
    {
        Action? handler;
        lock (_activationGate)
        {
            handler = _activationHandler;
            if (handler is null)
            {
                _activationPending = true;
                return;
            }
        }

        InvokeActivationHandler(handler);
    }

    private static void InvokeActivationHandler(Action handler)
    {
        try
        {
            handler();
        }
        catch (Exception exception)
        {
            WorkflowLog.Error("single-instance", "Could not activate the primary window.", exception);
        }
    }

    internal static string BuildInstanceKey(string applicationId, string identity)
    {
        var identityHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16];
        var safeApplicationId = new StringBuilder(applicationId.Length);
        foreach (var character in applicationId)
            safeApplicationId.Append(char.IsLetterOrDigit(character) ? character : '.');
        return $"{safeApplicationId}.{identityHash}";
    }

    private static string CurrentUserIdentity() =>
        $"{Environment.UserDomainName}\\{Environment.UserName}";

    public void Dispose()
    {
        lock (_activationGate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _activationHandler = null;
        }

        _listenerCancellation.Cancel();
        if (_listenerTask is not null)
        {
            try { _listenerTask.Wait(TimeSpan.FromSeconds(1)); }
            catch (AggregateException) { }
        }
        if (_ownsMutex)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Ownership can only be lost during abnormal teardown.
            }
            _ownsMutex = false;
        }

        _mutex.Dispose();
        _listenerCancellation.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint processId);
}
