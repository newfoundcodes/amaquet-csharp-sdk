/*
 * Amaquet C# SDK - A Newfoundcodes project.
 *
 * Copyright (C) 2026 Jonathan Eldy Baldivicio
 *
 * Author: Jonathan Eldy Baldivicio
 * Contact: jonathaneldy.baldivicio@newfoundcodes.com
 *
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;

namespace Amaquet;

/// <summary>BlobRange: Provides the Blob Range operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed record BlobRange(
    string Key,
    long Offset,
    long Size,
    long Version,
    byte[] Data
);

/// <summary>AmaquetClient: Provides the Amaquet Client operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Implements staged binary transfer commands with upload IDs, chunk offsets, range validation, and commit or abort lifecycle.</remarks>
/// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed partial class AmaquetClient
{
    /// <summary>BeginBlobAsync: Provides the Begin Blob Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> BeginBlobAsync(
        string key,
        long size,
        string? uploadId = null,
        bool connectionScoped = false,
        IDictionary<string, object?>? writeOptions = null,
        CancellationToken ct = default
    )
    {
        if (size < 0 || size > MaxBlob)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        return CommandAsync(
            "BLOB_BEGIN",
            WithOptions(
                JsonWire.Map(
                    "key",
                    key,
                    "total_size",
                    size,
                    "upload_id",
                    uploadId ?? Guid.NewGuid().ToString("N"),
                    "connection_scoped",
                    connectionScoped
                ),
                writeOptions
            ),
            ct
        );
    }

    /// <summary>BlobChunkAsync: Provides the Blob Chunk Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="uploadId">Staged upload identifier.</param>
    /// <param name="offset">Byte offset of this chunk.</param>
    /// <param name="bytes">Chunk data.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> BlobChunkAsync(
        string uploadId,
        long offset,
        ReadOnlyMemory<byte> bytes,
        CancellationToken ct = default
    )
    {
        if (
            offset < 0
            || offset > MaxBlob
            || bytes.Length < 1
            || bytes.Length > MaxBlobChunk
            || bytes.Length > MaxBlob - offset
        )
        {
            throw new ArgumentOutOfRangeException(nameof(bytes));
        }

        return CommandAsync(
            "BLOB_CHUNK",
            JsonWire.Map(
                "upload_id",
                uploadId,
                "offset",
                offset,
                "data",
                Convert.ToBase64String(bytes.Span)
            ),
            ct
        );
    }

    /// <summary>CommitBlobAsync: Provides the Commit Blob Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="uploadId">Staged upload identifier.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> CommitBlobAsync(
        string uploadId,
        CancellationToken ct = default
    ) => CommandAsync("BLOB_COMMIT", JsonWire.Map("upload_id", uploadId), ct);

    /// <summary>AbortBlobAsync: Provides the Abort Blob Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="uploadId">Staged upload identifier.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> AbortBlobAsync(
        string uploadId,
        CancellationToken ct = default
    ) => CommandAsync("BLOB_ABORT", JsonWire.Map("upload_id", uploadId), ct);

    /// <summary>ReadBlobRangeAsync: Read Blob Range Async using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="offset">Starting byte offset.</param>
    /// <param name="length">Maximum number of bytes to read.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<BlobRange> ReadBlobRangeAsync(
        string key,
        long offset,
        int length,
        CancellationToken ct = default
    )
    {
        if (
            offset < 0
            || offset > MaxBlob
            || length < 0
            || length > MaxBlobChunk
        )
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var response = await CommandAsync(
                "BLOB_READ",
                JsonWire.Map("key", key, "offset", offset, "length", length),
                ct
            )
            .ConfigureAwait(false);

        if (
            response.ValueKind != JsonValueKind.Object
            || JsonWire.RequireString(response, "key") != key
            || JsonWire.RequireInt64(response, "offset") != offset
        )
        {
            throw new AmaquetProtocolException(
                "Blob range metadata does not match request"
            );
        }

        long size = JsonWire.RequireInt64(response, "size"),
            version = JsonWire.RequireInt64(response, "version");
        if (size < 0 || size > MaxBlob || version < 1 || offset > size)
        {
            throw new AmaquetProtocolException("Invalid blob size/version");
        }

        string base64 = JsonWire.RequireString(response, "data");
        if (base64.Length > (((long)length + 2) / 3) * 4 + 4)
        {
            throw new AmaquetProtocolException(
                "Blob response exceeds requested size"
            );
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
            if (Convert.ToBase64String(bytes) != base64)
            {
                throw new FormatException("Noncanonical blob Base64");
            }
        }
        catch (FormatException e)
        {
            throw new AmaquetProtocolException("Invalid blob encoding", e);
        }

        if (bytes.Length > length || bytes.Length > size - offset)
        {
            throw new AmaquetProtocolException("Inconsistent blob range");
        }

        return new BlobRange(key, offset, size, version, bytes);
    }

    /// <summary>UploadBlobAsync: Provides the Upload Blob Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<JsonElement> UploadBlobAsync(
        string key,
        Stream input,
        long size,
        int chunkSize = 1 << 20,
        bool connectionScoped = false,
        IDictionary<string, object?>? options = null,
        CancellationToken ct = default
    )
    {
        if (
            size < 0
            || size > MaxBlob
            || chunkSize < 1
            || chunkSize > MaxBlobChunk
            || (size + chunkSize - 1) / chunkSize > 65536
        )
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        var begin = await BeginBlobAsync(
                key,
                size,
                connectionScoped: connectionScoped,
                writeOptions: options,
                ct: ct
            )
            .ConfigureAwait(false);
        var id = JsonWire.RequireString(begin, "upload_id");
        bool committed = false;

        try
        {
            var buffer = new byte[chunkSize];
            long sent = 0;

            while (sent < size)
            {
                int want = (int)Math.Min(chunkSize, size - sent);

                await input
                    .ReadExactlyAsync(buffer.AsMemory(0, want), ct)
                    .ConfigureAwait(false);
                await BlobChunkAsync(id, sent, buffer.AsMemory(0, want), ct)
                    .ConfigureAwait(false);

                sent += want;
            }

            var result = await CommitBlobAsync(id, ct).ConfigureAwait(false);
            committed = true;

            return result;
        }
        finally
        {
            if (!committed)
            {
                try
                {
                    await AbortBlobAsync(id).ConfigureAwait(false);
                }
                catch (Exception) { }
            }
        }
    }

    /// <summary>UploadBlobAsync: Provides the Upload Blob Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="bytes">Blob contents to upload.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public Task<JsonElement> UploadBlobAsync(
        string key,
        byte[] bytes,
        CancellationToken ct = default
    ) =>
        UploadBlobAsync(
            key,
            new MemoryStream(bytes, writable: false),
            bytes.LongLength,
            ct: ct
        );

    /// <summary>DownloadBlobAsync: Provides the Download Blob Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<long> DownloadBlobAsync(
        string key,
        Stream target,
        int chunkSize = 1 << 20,
        long maxBytes = MaxBlob,
        CancellationToken ct = default
    )
    {
        if (
            chunkSize < 1
            || chunkSize > MaxBlobChunk
            || maxBytes < 0
            || maxBytes > MaxBlob
        )
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        long offset = 0,
            size = -1,
            version = -1;
        do
        {
            var range = await ReadBlobRangeAsync(key, offset, chunkSize, ct)
                .ConfigureAwait(false);

            if (size < 0)
            {
                size = range.Size;
                version = range.Version;
            }
            else if (range.Size != size || range.Version != version)
            {
                throw new AmaquetProtocolException(
                    "Blob changed during download"
                );
            }

            if (range.Data.Length == 0 && offset < size)
            {
                throw new AmaquetProtocolException("Blob read ended early");
            }

            if (range.Data.Length > maxBytes - offset)
            {
                throw new AmaquetProtocolException(
                    "Blob exceeds download byte limit"
                );
            }

            await target.WriteAsync(range.Data, ct).ConfigureAwait(false);
            offset += range.Data.Length;
        } while (offset < size);
        return offset;
    }

    /// <summary>DownloadBlobBytesAsync: Provides the Download Blob Bytes Async operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Binary transfers use staged IDs and bounded chunks; abort incomplete uploads and verify range limits.</remarks>
    /// <remarks>Parameter `key`: Target Amaquet key; use unique keys for examples and tests.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="key">Target Amaquet key.</param>
    /// <param name="maximum">Maximum number of bytes to download.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public async Task<byte[]> DownloadBlobBytesAsync(
        string key,
        int maximum = 64 << 20,
        CancellationToken ct = default
    )
    {
        using var buffer = new MemoryStream();
        await DownloadBlobAsync(key, buffer, maxBytes: maximum, ct: ct)
            .ConfigureAwait(false);

        return buffer.ToArray();
    }
}
