using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Amaquet;

/// <summary>RequestTicket: Provides the Request Ticket operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed record RequestTicket(
    ulong Id,
    Opcode Opcode,
    Task<JsonElement> Response
);

/// <summary>A single multiplexed Amaquet TCP/TLS session. Safe for concurrent callers.</summary>
/// <summary>AmaquetClient: Provides the Amaquet Client operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed partial class AmaquetClient : IAsyncDisposable
{
    private const long MaxBlob = 1L << 30;
    private const int MaxBlobChunk = 32 << 20;
    private readonly TcpClient _tcp;
    private readonly Stream _stream;
    private readonly ClientOptions _options;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentDictionary<ulong, Pending> _pending = new();
    private readonly ConcurrentDictionary<
        ulong,
        AmaquetSubscription
    > _subscriptions = new();
    private readonly ConcurrentDictionary<string, ulong> _targets = new();
    private Task _reader = Task.CompletedTask;
    private long _ids;
    private int _closed;
    private Exception? _failure;
    private JsonElement _serverInfo;

    /// <summary>Pending: Provides the Pending operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    private sealed record Pending(
        Opcode Opcode,
        TaskCompletionSource<JsonElement> Source
    );

    private AmaquetClient(TcpClient tcp, Stream stream, ClientOptions options)
    {
        _tcp = tcp;
        _stream = stream;
        _options = options;
        _reader = Task.Run(ReadLoopAsync);
    }

    public JsonElement ServerInfo => _serverInfo;

    /// <summary>Read: Read using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public bool IsClosed => Volatile.Read(ref _closed) != 0;

    /// <summary>new: Provides the new operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public Types Typed => new(this);

    /// <summary>ConnectAsync: Provides the Connect Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `endpoint`: Host and port of the native service or HTTP administration origin.</remarks>
    /// <remarks>Parameter `options`: Call settings or documented construction flags, as accepted by this method.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="options">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="endpoint">Amaquet endpoint to connect to.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public static async Task<AmaquetClient> ConnectAsync(
        string endpoint,
        ClientOptions? options = null,
        CancellationToken ct = default
    )
    {
        options ??= new ClientOptions();
        options.Validate();

        var addr = AmaquetEndpoint.Parse(endpoint);
        var tcp = new TcpClient();

        Stream? stream = null;
        AmaquetClient? client = null;

        try
        {
            using (
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
            )
            {
                cts.CancelAfter(options.ConnectTimeout);
                await tcp.ConnectAsync(addr.Host, addr.Port, cts.Token)
                    .ConfigureAwait(false);

                stream = tcp.GetStream();
                if (addr.Tls)
                {
                    var secure = new SslStream(
                        stream,
                        leaveInnerStreamOpen: false
                    );

                    await secure
                        .AuthenticateAsClientAsync(
                            new SslClientAuthenticationOptions
                            {
                                TargetHost = addr.Host,
                                EnabledSslProtocols =
                                    SslProtocols.Tls12 | SslProtocols.Tls13,
                                CertificateRevocationCheckMode = System
                                    .Security
                                    .Cryptography
                                    .X509Certificates
                                    .X509RevocationMode
                                    .Online,
                            },
                            cts.Token
                        )
                        .ConfigureAwait(false);

                    stream = secure;
                }
            }

            client = new AmaquetClient(tcp, stream!, options);

            var nonce = Convert
                .ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=');
            var hello = await client
                .OpcodeAsync(
                    Opcode.Hello,
                    JsonWire.Map(
                        "nonce",
                        nonce,
                        "min_protocol",
                        1,
                        "max_protocol",
                        1
                    ),
                    ct
                )
                .ConfigureAwait(false);

            if (
                hello.ValueKind != JsonValueKind.Object
                || !hello.TryGetProperty("protocol", out var protocol)
                || protocol.ValueKind != JsonValueKind.Number
                || protocol.GetInt32() != 1
                || (
                    hello.TryGetProperty("selected_protocol", out var selected)
                    && selected.GetInt32() != 1
                )
            )
            {
                throw new AmaquetProtocolException(
                    "Server does not speak supported Amaquet protocol v1"
                );
            }

            Identity.Verify(hello, nonce, options);
            client._serverInfo = hello;

            bool authRequired =
                hello.TryGetProperty("auth_required", out var auth)
                && auth.ValueKind == JsonValueKind.True;
            if (authRequired && string.IsNullOrEmpty(options.ApiKey))
            {
                throw new AmaquetRequestException(
                    "UNAUTHORIZED",
                    "Server requires an API key"
                );
            }

            if (options.ApiKey is not null)
            {
                await client
                    .OpcodeAsync(
                        Opcode.Auth,
                        JsonWire.Map("api_key", options.ApiKey),
                        ct
                    )
                    .ConfigureAwait(false);
            }

            return client;
        }
        catch
        {
            if (client is not null)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                stream?.Dispose();
                tcp.Dispose();
            }
            throw;
        }
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var frame = await WireProtocol
                    .ReadAsync(_stream, _options.MaxPayloadBytes, _stop.Token)
                    .ConfigureAwait(false);
                var envelope = JsonWire.Decode(frame.Payload);

                if (frame.Kind == FrameKind.Event)
                {
                    if (
                        frame.Opcode != Opcode.Subscribe
                        || envelope.ValueKind != JsonValueKind.Object
                    )
                    {
                        throw new AmaquetProtocolException(
                            "Unexpected event frame"
                        );
                    }

                    if (
                        _subscriptions.TryGetValue(
                            frame.RequestId,
                            out var subscription
                        )
                    )
                    {
                        subscription.Offer(envelope);
                    }

                    continue;
                }

                if (frame.Kind != FrameKind.Response)
                {
                    throw new AmaquetProtocolException(
                        "Unexpected request frame from server"
                    );
                }

                if (!_pending.TryRemove(frame.RequestId, out var pending))
                {
                    continue; // Cancelled or timed out.
                }

                try
                {
                    if (
                        frame.Opcode != Opcode.Command
                        || envelope.ValueKind != JsonValueKind.Object
                        || !envelope.TryGetProperty("ok", out var ok)
                        || (
                            ok.ValueKind != JsonValueKind.True
                            && ok.ValueKind != JsonValueKind.False
                        )
                    )
                    {
                        pending.Source.TrySetException(
                            new AmaquetProtocolException(
                                "Response frame or envelope mismatch"
                            )
                        );

                        throw new AmaquetProtocolException(
                            "Response frame or envelope mismatch"
                        );
                    }

                    if (ok.GetBoolean())
                    {
                        if (envelope.TryGetProperty("result", out var result))
                        {
                            pending.Source.TrySetResult(result.Clone());
                        }
                        else
                        {
                            pending.Source.TrySetResult(
                                JsonWire.Decode("null"u8)
                            );
                        }
                    }
                    else
                    {
                        if (
                            !envelope.TryGetProperty("error", out var error)
                            || error.ValueKind != JsonValueKind.Object
                        )
                        {
                            pending.Source.TrySetException(
                                new AmaquetProtocolException(
                                    "Invalid error envelope"
                                )
                            );

                            throw new AmaquetProtocolException(
                                "Invalid error envelope"
                            );
                        }

                        var code = JsonWire.RequireString(error, "code");
                        var message = JsonWire.RequireString(error, "message");

                        pending.Source.TrySetException(
                            new AmaquetRequestException(code, message)
                        );
                    }
                }
                catch (Exception ex)
                {
                    pending.Source.TrySetException(ex);
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        { }
        catch (Exception ex)
        {
            ShutDown(
                new AmaquetTransportException(
                    "Amaquet read loop terminated",
                    ex
                )
            );
        }
    }

    private ulong NextId()
    {
        long n = Interlocked.Increment(ref _ids);
        if (n <= 0)
        {
            throw new AmaquetException("Request ID exhausted");
        }

        return (ulong)n;
    }

    private async Task<RequestTicket> SendAsync(
        Opcode opcode,
        object? request,
        Action<ulong>? beforeWrite = null,
        CancellationToken ct = default
    )
    {
        var bytes = JsonWire.Encode(request);
        if (bytes.Length > _options.MaxPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Request exceeds configured payload size"
            );
        }

        using var writeTimeout =
            CancellationTokenSource.CreateLinkedTokenSource(ct, _stop.Token);
        writeTimeout.CancelAfter(_options.RequestTimeout);
        await _writeGate.WaitAsync(writeTimeout.Token).ConfigureAwait(false);

        try
        {
            if (IsClosed)
            {
                throw new AmaquetTransportException(
                    "Connection is closed",
                    _failure
                );
            }

            if (_pending.Count >= _options.MaxPendingRequests)
            {
                throw new AmaquetException("Too many pending requests");
            }

            ulong id = NextId();
            var source = new TaskCompletionSource<JsonElement>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var pending = new Pending(opcode, source);

            if (!_pending.TryAdd(id, pending))
            {
                throw new AmaquetProtocolException("Duplicate request ID");
            }

            try
            {
                beforeWrite?.Invoke(id);
                var frame = WireProtocol.Encode(
                    new WireFrame(FrameKind.Request, opcode, id, bytes),
                    _options.MaxPayloadBytes
                );

                await _stream
                    .WriteAsync(frame, writeTimeout.Token)
                    .ConfigureAwait(false);
                await _stream
                    .FlushAsync(writeTimeout.Token)
                    .ConfigureAwait(false);

                return new RequestTicket(id, opcode, source.Task);
            }
            catch
            {
                _pending.TryRemove(id, out _);
                source.TrySetCanceled();

                // A partially written frame cannot be safely reused.
                ShutDown(
                    new AmaquetTransportException(
                        "Amaquet request write failed"
                    )
                );
                throw;
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task<JsonElement> AwaitAsync(
        RequestTicket ticket,
        CancellationToken ct
    )
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            ct,
            _stop.Token
        );
        timeout.CancelAfter(_options.RequestTimeout);

        try
        {
            return await ticket
                .Response.WaitAsync(timeout.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!_stop.IsCancellationRequested)
        {
            _pending.TryRemove(ticket.Id, out _);
            _ = BestEffortCancelAsync(ticket.Id);

            throw;
        }
    }

    private async Task BestEffortCancelAsync(ulong id)
    {
        try
        {
            await OpcodeAsync(Opcode.Cancel, JsonWire.Map("request_id", id))
                .ConfigureAwait(false);
        }
        catch (Exception)
        { /* Cancellation is best effort. */
        }
    }

    /// <summary>BeginCommandAsync: Provides the Begin Command Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `args`: Exact JSON argument properties supported by the server; names are not translated.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="args">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="command">Command to send to the server.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<RequestTicket> BeginCommandAsync(
        string command,
        IDictionary<string, object?>? args = null,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command is required", nameof(command));
        }

        return SendAsync(
            Opcode.Command,
            JsonWire.Map(
                "command",
                command.ToUpperInvariant(),
                "args",
                args ?? new Dictionary<string, object?>()
            ),
            ct: ct
        );
    }

    /// <summary>CommandAsync: Provides the Command Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `args`: Exact JSON argument properties supported by the server; names are not translated.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="args">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="command">Command to send to the server.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<JsonElement> CommandAsync(
        string command,
        IDictionary<string, object?>? args = null,
        CancellationToken ct = default
    ) =>
        await AwaitAsync(
                await BeginCommandAsync(command, args, ct)
                    .ConfigureAwait(false),
                ct
            )
            .ConfigureAwait(false);

    /// <summary>OpcodeAsync: Provides the Opcode Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `args`: Exact JSON argument properties supported by the server; names are not translated.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="opcode">Protocol opcode to send.</param>
    /// <param name="args">Optional operation arguments.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<JsonElement> OpcodeAsync(
        Opcode opcode,
        object? args,
        CancellationToken ct = default
    ) =>
        await AwaitAsync(
                await SendAsync(opcode, args, ct: ct).ConfigureAwait(false),
                ct
            )
            .ConfigureAwait(false);

    /// <summary>CancelRequestAsync: Provides the Cancel Request Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="requestId">Request identifier to cancel.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CancelRequestAsync(
        ulong requestId,
        CancellationToken ct = default
    ) => OpcodeAsync(Opcode.Cancel, JsonWire.Map("request_id", requestId), ct);

    /// <summary>PingAsync: Provides the Ping Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> PingAsync(CancellationToken ct = default) =>
        CommandAsync("PING", ct: ct);

    /// <summary>GetAsync: Provides the Get Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> GetAsync(
        string key,
        CancellationToken ct = default
    ) => CommandAsync("GET", JsonWire.Map("key", key), ct);

    /// <summary>SetAsync: Provides the Set Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `value`: Data to write or transmit, using the wire-type JSON format.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="type">Amaquet wire type.</param>
    /// <param name="value">Value to store.</param>
    /// <param name="writeOptions">Optional write settings.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> SetAsync(
        string key,
        string type,
        object? value,
        IDictionary<string, object?>? writeOptions = null,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "SET",
            WithOptions(
                JsonWire.Map("key", key, "type", type, "value", value),
                writeOptions
            ),
            ct
        );

    /// <summary>CreateAsync: Provides the Create Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="type">Amaquet wire type.</param>
    /// <param name="config">Type creation configuration.</param>
    /// <param name="writeOptions">Optional write settings.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CreateAsync(
        string key,
        string type,
        object? config = null,
        IDictionary<string, object?>? writeOptions = null,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "CREATE",
            WithOptions(
                JsonWire.Map(
                    "key",
                    key,
                    "type",
                    type,
                    "options",
                    config ?? new { }
                ),
                writeOptions
            ),
            ct
        );

    /// <summary>CasAsync: Provides the Cas Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `value`: Data to write or transmit, using the wire-type JSON format.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="expectedVersion">Expected current version.</param>
    /// <param name="type">Amaquet wire type.</param>
    /// <param name="value">Replacement value.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CasAsync(
        string key,
        ulong expectedVersion,
        string type,
        object? value,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "CAS",
            JsonWire.Map(
                "key",
                key,
                "expected_version",
                expectedVersion,
                "type",
                type,
                "value",
                value
            ),
            ct
        );

    /// <summary>BatchAsync: Provides the Batch Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="requests">Requests to execute.</param>
    /// <param name="continueOnError">Whether to continue after a request fails.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> BatchAsync(
        IEnumerable<object> requests,
        bool continueOnError = false,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "BATCH",
            JsonWire.Map(
                "requests",
                requests,
                "continue_on_error",
                continueOnError
            ),
            ct
        );

    /// <summary>DeleteAsync: Provides the Delete Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="keys">Keys to delete.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> DeleteAsync(
        IEnumerable<string> keys,
        CancellationToken ct = default
    ) => CommandAsync("DEL", JsonWire.Map("keys", keys), ct);

    /// <summary>ExistsAsync: Provides the Exists Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> ExistsAsync(
        string key,
        CancellationToken ct = default
    ) => CommandAsync("EXISTS", JsonWire.Map("key", key), ct);

    /// <summary>TypeAsync: Provides the Type Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> TypeAsync(
        string key,
        CancellationToken ct = default
    ) => CommandAsync("TYPE", JsonWire.Map("key", key), ct);

    /// <summary>TtlAsync: Provides the Ttl Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> TtlAsync(
        string key,
        CancellationToken ct = default
    ) => CommandAsync("TTL", JsonWire.Map("key", key), ct);

    /// <summary>ExpireAsync: Provides the Expire Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="ttlMilliseconds">Time to live in milliseconds.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> ExpireAsync(
        string key,
        long ttlMilliseconds,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "EXPIRE",
            JsonWire.Map("key", key, "ttl_ms", ttlMilliseconds),
            ct
        );

    /// <summary>ExpireAtAsync: Provides the Expire At Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="expiresAtUnixNanoseconds">Expiration time as Unix nanoseconds.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> ExpireAtAsync(
        string key,
        long expiresAtUnixNanoseconds,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "EXPIRE",
            JsonWire.Map(
                "key",
                key,
                "expires_at_unix_nano",
                expiresAtUnixNanoseconds
            ),
            ct
        );

    /// <summary>PersistAsync: Provides the Persist Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> PersistAsync(
        string key,
        CancellationToken ct = default
    ) => CommandAsync("PERSIST", JsonWire.Map("key", key), ct);

    /// <summary>KeysAsync: Provides the Keys Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="prefix">Key prefix filter.</param>
    /// <param name="limit">Maximum number of keys to return.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> KeysAsync(
        string prefix = "",
        int limit = 100,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "KEYS",
            JsonWire.Map("prefix", prefix, "limit", limit),
            ct
        );

    /// <summary>ScanAsync: Provides the Scan Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="cursor">Cursor returned by a previous scan.</param>
    /// <param name="prefix">Optional key prefix filter.</param>
    /// <param name="count">Maximum number of keys to return.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> ScanAsync(
        string cursor = "",
        string prefix = "",
        int count = 100,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "SCAN",
            JsonWire.Map("cursor", cursor, "prefix", prefix, "count", count),
            ct
        );

    /// <summary>TypesAsync: Provides the Types Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> TypesAsync(CancellationToken ct = default) =>
        CommandAsync("TYPES", ct: ct);

    /// <summary>InfoAsync: Provides the Info Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> InfoAsync(CancellationToken ct = default) =>
        CommandAsync("INFO", ct: ct);

    /// <summary>MemoryAsync: Provides the Memory Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> MemoryAsync(
        string key,
        CancellationToken ct = default
    ) => CommandAsync("MEMORY", JsonWire.Map("key", key), ct);

    /// <summary>OperationAsync: Provides the Operation Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `args`: Exact JSON argument properties supported by the server; names are not translated.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="operation">Operation name to execute.</param>
    /// <param name="args">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> OperationAsync(
        string key,
        string operation,
        IDictionary<string, object?>? args = null,
        CancellationToken ct = default
    ) =>
        CommandAsync(
            "OP",
            JsonWire.Map(
                "key",
                key,
                "operation",
                operation.ToUpperInvariant(),
                "args",
                args ?? new Dictionary<string, object?>()
            ),
            ct
        );

    /// <summary>WithOptions: Provides the With Options operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public static Dictionary<string, object?> WithOptions(
        Dictionary<string, object?> request,
        IDictionary<string, object?>? writeOptions
    )
    {
        if (writeOptions is null)
        {
            return request;
        }

        if (
            writeOptions.TryGetValue("nx", out var nx)
            && nx is true
            && writeOptions.TryGetValue("xx", out var xx)
            && xx is true
        )
        {
            throw new ArgumentException("nx and xx cannot both be true");
        }

        foreach (var item in writeOptions)
        {
            request[item.Key] = item.Value;
        }

        return request;
    }

    /// <summary>SubscribeAsync: Provides the Subscribe Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="channel">Subscription channel.</param>
    /// <param name="serverBuffer">Server-side subscription buffer size.</param>
    /// <param name="localCapacity">Local subscription queue capacity.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<AmaquetSubscription> SubscribeAsync(
        string key,
        string channel,
        int serverBuffer = 64,
        int localCapacity = 256,
        CancellationToken ct = default
    )
    {
        if (serverBuffer < 1 || localCapacity < 1 || localCapacity > 100000)
        {
            throw new ArgumentOutOfRangeException(nameof(localCapacity));
        }

        var target = key + "\0" + channel;
        if (!_targets.TryAdd(target, 0))
        {
            throw new AmaquetException("Duplicate local subscription");
        }

        AmaquetSubscription? sub = null;
        try
        {
            var ticket = await SendAsync(
                    Opcode.Subscribe,
                    JsonWire.Map(
                        "key",
                        key,
                        "channel",
                        channel,
                        "buffer",
                        serverBuffer
                    ),
                    id =>
                    {
                        sub = new AmaquetSubscription(
                            this,
                            key,
                            channel,
                            id,
                            localCapacity
                        );
                        if (
                            !_subscriptions.TryAdd(id, sub)
                            || !_targets.TryUpdate(target, id, 0)
                        )
                        {
                            throw new AmaquetProtocolException(
                                "Failed to register subscription"
                            );
                        }
                    },
                    ct
                )
                .ConfigureAwait(false);

            await AwaitAsync(ticket, ct).ConfigureAwait(false);
            return sub!;
        }
        catch
        {
            _targets.TryRemove(target, out _);
            if (sub is not null)
            {
                _subscriptions.TryRemove(sub.RequestId, out _);
                sub.Finish();
            }

            try
            {
                await OpcodeAsync(
                        Opcode.Unsubscribe,
                        JsonWire.Map("key", key, "channel", channel)
                    )
                    .ConfigureAwait(false);
            }
            catch { }

            throw;
        }
    }

    /// <summary>UnsubscribeAsync: Provides the Unsubscribe Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="channel">Subscription channel.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<JsonElement> UnsubscribeAsync(
        string key,
        string channel,
        CancellationToken ct = default
    )
    {
        try
        {
            return await OpcodeAsync(
                    Opcode.Unsubscribe,
                    JsonWire.Map("key", key, "channel", channel),
                    ct
                )
                .ConfigureAwait(false);
        }
        finally
        {
            if (
                _targets.TryRemove(key + "\0" + channel, out var id)
                && _subscriptions.TryRemove(id, out var sub)
            )
            {
                sub.Finish();
            }
        }
    }

    private void ShutDown(Exception exception)
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        _failure = exception;
        _stop.Cancel();

        try
        {
            _stream.Dispose();
        }
        catch { }

        _tcp.Dispose();
        foreach (var entry in _pending)
        {
            if (_pending.TryRemove(entry.Key, out var request))
            {
                request.Source.TrySetException(exception);
            }
        }

        foreach (var entry in _subscriptions)
        {
            if (_subscriptions.TryRemove(entry.Key, out var sub))
            {
                sub.Finish();
            }
        }

        _targets.Clear();
    }

    /// <summary>DisposeAsync: Provides the Dispose Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public async ValueTask DisposeAsync()
    {
        ShutDown(new AmaquetTransportException("Connection closed"));

        try
        {
            await _reader.ConfigureAwait(false);
        }
        catch { }

        _stop.Dispose();
        _writeGate.Dispose();
    }
}

/// <summary>AmaquetSubscription: Provides the Amaquet Subscription operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class AmaquetSubscription : IAsyncDisposable
{
    private readonly AmaquetClient _client;
    private readonly string _key,
        _channel;
    private readonly Channel<JsonElement> _queue;
    private long _dropped;
    private int _closed;

    /// <summary>AmaquetSubscription: Provides the Amaquet Subscription operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `client`: Existing client connected to the Amaquet endpoint.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    internal AmaquetSubscription(
        AmaquetClient client,
        string key,
        string channel,
        ulong id,
        int capacity
    )
    {
        _client = client;
        _key = key;
        _channel = channel;

        RequestId = id;
        _queue = Channel.CreateBounded<JsonElement>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
            }
        );
    }

    public ulong RequestId { get; }

    /// <summary>Read: Read using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public long DroppedEvents => Interlocked.Read(ref _dropped);

    /// <summary>Read: Read using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public bool IsClosed => Volatile.Read(ref _closed) != 0;

    /// <summary>Offer: Provides the Offer operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    internal void Offer(JsonElement evt)
    {
        if (!IsClosed && !_queue.Writer.TryWrite(evt))
        {
            Interlocked.Increment(ref _dropped);
        }
    }

    /// <summary>Finish: Provides the Finish operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    internal void Finish()
    {
        if (Interlocked.Exchange(ref _closed, 1) == 0)
        {
            _queue.Writer.TryComplete();
        }
    }

    /// <summary>ReadAllAsync: Read All Async using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    public IAsyncEnumerable<JsonElement> ReadAllAsync(
        CancellationToken ct = default
    ) => _queue.Reader.ReadAllAsync(ct);

    /// <summary>DisposeAsync: Provides the Dispose Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public async ValueTask DisposeAsync()
    {
        if (IsClosed)
        {
            return;
        }

        try
        {
            await _client
                .UnsubscribeAsync(_key, _channel)
                .ConfigureAwait(false);
        }
        finally
        {
            Finish();
        }
    }
}
