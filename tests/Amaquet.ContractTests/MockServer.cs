using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amaquet;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Amaquet.ContractTests;

internal sealed class MockServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Ed25519PrivateKeyParameters _private = new(
        RandomNumberGenerator.GetBytes(32),
        0
    );
    private readonly Dictionary<string, byte[]> _blob = new();
    private readonly Dictionary<string, byte[]> _committed = new();
    private Task? _server;
    private int _commandCount;

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
    public int CommandCount => Volatile.Read(ref _commandCount);
    public string PublicKeyBase64 =>
        Convert.ToBase64String(_private.GeneratePublicKey().GetEncoded());
    public string Fingerprint =>
        "ed25519:"
        + Convert
            .ToHexString(
                SHA256.HashData(_private.GeneratePublicKey().GetEncoded())
            )
            .ToLowerInvariant();

    public void Start()
    {
        _listener.Start();
        _server = Task.Run(AcceptAsync);
    }

    private async Task AcceptAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var socket = await _listener.AcceptTcpClientAsync(_stop.Token);
                _ = Task.Run(() => ServeAsync(socket));
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using (client)
        {
            var stream = client.GetStream();
            var gate = new SemaphoreSlim(1, 1);

            async Task WriteAsync(WireFrame reply)
            {
                await gate.WaitAsync(_stop.Token);
                try
                {
                    await stream.WriteAsync(
                        WireProtocol.Encode(reply),
                        _stop.Token
                    );
                }
                finally
                {
                    gate.Release();
                }
            }

            async Task RespondAsync(
                WireFrame incoming,
                object? value,
                string? error = null
            )
            {
                var body = error is null
                    ? JsonWire.Map("ok", true, "result", value)
                    : JsonWire.Map(
                        "ok",
                        false,
                        "error",
                        JsonWire.Map("code", "MOCK", "message", error)
                    );

                await WriteAsync(
                    new WireFrame(
                        FrameKind.Response,
                        Opcode.Command,
                        incoming.RequestId,
                        JsonWire.Encode(body)
                    )
                );
            }

            try
            {
                while (!_stop.IsCancellationRequested)
                {
                    var frame = await WireProtocol.ReadAsync(
                        stream,
                        ct: _stop.Token
                    );
                    var data = JsonWire.Decode(frame.Payload);

                    if (frame.Opcode == Opcode.Hello)
                    {
                        string nonce = JsonWire.RequireString(data, "nonce");
                        var signer = new Ed25519Signer();
                        signer.Init(true, _private);

                        var raw = Encoding.UTF8.GetBytes(nonce);
                        signer.BlockUpdate(raw, 0, raw.Length);

                        await RespondAsync(
                            frame,
                            JsonWire.Map(
                                "protocol",
                                1,
                                "selected_protocol",
                                1,
                                "auth_required",
                                true,
                                "identity_public_key",
                                PublicKeyBase64,
                                "nonce_signature",
                                Convert.ToBase64String(
                                    signer.GenerateSignature()
                                ),
                                "identity_fingerprint",
                                Fingerprint
                            )
                        );
                    }
                    else if (frame.Opcode == Opcode.Auth)
                        await RespondAsync(
                            frame,
                            JsonWire.Map("authenticated", true)
                        );
                    else if (frame.Opcode == Opcode.Subscribe)
                    {
                        await WriteAsync(
                            new WireFrame(
                                FrameKind.Event,
                                Opcode.Subscribe,
                                frame.RequestId,
                                JsonWire.Encode(
                                    JsonWire.Map(
                                        "channel",
                                        "example",
                                        "data",
                                        "early-event"
                                    )
                                )
                            )
                        );

                        await RespondAsync(
                            frame,
                            JsonWire.Map("subscribed", true)
                        );
                    }
                    else if (
                        frame.Opcode == Opcode.Unsubscribe
                        || frame.Opcode == Opcode.Cancel
                    )
                        await RespondAsync(
                            frame,
                            JsonWire.Map(
                                "cancelled",
                                true,
                                "unsubscribed",
                                true
                            )
                        );
                    else if (frame.Opcode == Opcode.Command)
                    {
                        Interlocked.Increment(ref _commandCount);
                        _ = Task.Run(
                            async () =>
                            {
                                try
                                {
                                    var cmd = JsonWire.RequireString(
                                        data,
                                        "command"
                                    );
                                    var args = data.GetProperty("args");

                                    if (
                                        args.TryGetProperty(
                                            "delay_ms",
                                            out var ms
                                        )
                                    ) {
                                        await Task.Delay(
                                            ms.GetInt32(),
                                            _stop.Token
                                        );
                                    }

                                    object? result;
                                    if (cmd == "BLOB_BEGIN")
                                    {
                                        string id = JsonWire.RequireString(
                                            args,
                                            "upload_id"
                                        );

                                        lock (_blob)
                                            _blob[id] = new byte[
                                                checked(
                                                    (int)
                                                        JsonWire.RequireInt64(
                                                            args,
                                                            "total_size"
                                                        )
                                                )
                                            ];
                                        result = JsonWire.Map("upload_id", id);
                                    }
                                    else if (cmd == "BLOB_CHUNK")
                                    {
                                        string id = JsonWire.RequireString(
                                            args,
                                            "upload_id"
                                        );
                                        var bytes = Convert.FromBase64String(
                                            JsonWire.RequireString(args, "data")
                                        );

                                        lock (_blob)
                                            bytes
                                                .AsSpan()
                                                .CopyTo(
                                                    _blob[id]
                                                        .AsSpan(
                                                            checked(
                                                                (int)
                                                                    JsonWire.RequireInt64(
                                                                        args,
                                                                        "offset"
                                                                    )
                                                            )
                                                        )
                                                );

                                        result = JsonWire.Map(
                                            "written",
                                            bytes.Length
                                        );
                                    }
                                    else if (cmd == "BLOB_COMMIT")
                                    {
                                        string id = JsonWire.RequireString(
                                            args,
                                            "upload_id"
                                        );

                                        lock (_blob)
                                            _committed[id] = _blob[id];

                                        result = JsonWire.Map(
                                            "committed",
                                            true
                                        );
                                    }
                                    else if (cmd == "BLOB_ABORT")
                                    {
                                        result = JsonWire.Map("aborted", true);
                                    }
                                    else if (cmd == "BLOB_READ")
                                    {
                                        byte[] content;
                                        lock (_blob)
                                            content = _committed.Values.Last();

                                        long offset = JsonWire.RequireInt64(
                                            args,
                                            "offset"
                                        );

                                        long length = JsonWire.RequireInt64(
                                            args,
                                            "length"
                                        );

                                        var part = content
                                            .Skip(checked((int)offset))
                                            .Take(checked((int)length))
                                            .ToArray();

                                        result = JsonWire.Map(
                                            "key",
                                            JsonWire.RequireString(args, "key"),
                                            "offset",
                                            offset,
                                            "size",
                                            content.Length,
                                            "version",
                                            1,
                                            "data",
                                            Convert.ToBase64String(part)
                                        );
                                    }
                                    else
                                        result = JsonWire.Map(
                                            "command",
                                            cmd,
                                            "args",
                                            args.Clone()
                                        );
                                    await RespondAsync(frame, result);
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        await RespondAsync(
                                            frame,
                                            null,
                                            ex.Message
                                        );
                                    }
                                    catch { }
                                }
                            },
                            _stop.Token
                        );
                    }
                }
            }
            catch (Exception ex)
                when (ex
                        is IOException
                            or EndOfStreamException
                            or ObjectDisposedException
                            or OperationCanceledException
                ) { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();

        if (_server is not null)
            try
            {
                await _server;
            }
            catch { }

        _stop.Dispose();
    }
}
