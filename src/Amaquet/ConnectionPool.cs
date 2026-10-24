using System.Text.Json;

namespace Amaquet;

/// <summary>Round-robin pool of independent multiplexed native connections. No automatic replay of writes.</summary>
/// <summary>ConnectionPool: Provides the Connection Pool operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Reuses Amaquet sessions while handling broken connections and protecting callers from unbounded concurrency.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class ConnectionPool : IAsyncDisposable
{
    private readonly AmaquetClient[] _clients;
    private long _next = -1;
    private int _disposed;

    private ConnectionPool(AmaquetClient[] clients) => _clients = clients;

    /// <summary>ConnectAsync: Provides the Connect Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Parameter `endpoint`: Host and port of the native service or HTTP administration origin.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public static async Task<ConnectionPool> ConnectAsync(
        string endpoint,
        int size = 4,
        ClientOptions? options = null,
        CancellationToken ct = default
    )
    {
        if (size < 1 || size > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        var connected = new List<AmaquetClient>(size);
        try
        {
            for (int i = 0; i < size; i++)
            {
                connected.Add(
                    await AmaquetClient
                        .ConnectAsync(endpoint, options, ct)
                        .ConfigureAwait(false)
                );
            }

            return new ConnectionPool(connected.ToArray());
        }
        catch
        {
            foreach (var client in connected)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>Next: Provides the Next operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public AmaquetClient Next()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(ConnectionPool));
        }

        var index = (int)(
            (ulong)Interlocked.Increment(ref _next) % (ulong)_clients.Length
        );
        return _clients[index];
    }

    /// <summary>CommandAsync: Provides the Command Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Parameter `args`: Exact JSON argument properties supported by the server; names are not translated.</remarks>
    /// <remarks>Parameter `name`: Native command or type operation name sent to the server.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="name">Command name.</param>
    /// <param name="args">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CommandAsync(
        string name,
        IDictionary<string, object?>? args = null,
        CancellationToken ct = default
    ) => Next().CommandAsync(name, args, ct);

    /// <summary>DisposeAsync: Provides the Dispose Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (var client in _clients)
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
