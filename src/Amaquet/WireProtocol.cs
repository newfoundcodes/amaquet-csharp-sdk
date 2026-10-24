using System.Buffers.Binary;

namespace Amaquet;

/// <summary>FrameKind: Provides the Frame Kind operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Defines strict Amaquet protocol v1 frame layout, opcodes, validation, and payload limits.</remarks>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public enum FrameKind : byte
{
    /// <summary>A request sent by a client.</summary>
    Request = 1,

    /// <summary>A response sent by the server.</summary>
    Response = 2,

    /// <summary>An unsolicited event sent by the server.</summary>
    Event = 3,
}

/// <summary>Opcode: Provides the Opcode operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Defines strict Amaquet protocol v1 frame layout, opcodes, validation, and payload limits.</remarks>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public enum Opcode : ushort
{
    /// <summary>General command operation.</summary>
    Command = 1,

    /// <summary>Initial protocol handshake.</summary>
    Hello = 2,

    /// <summary>Authentication operation.</summary>
    Auth = 3,

    /// <summary>Subscribe operation.</summary>
    Subscribe = 4,

    /// <summary>Unsubscribe operation.</summary>
    Unsubscribe = 5,

    /// <summary>Request cancellation operation.</summary>
    Cancel = 6,
}

/// <summary>WireFrame: Provides the Wire Frame operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed record WireFrame(
    FrameKind Kind,
    Opcode Opcode,
    ulong RequestId,
    byte[] Payload,
    ushort Flags = 0
);

/// <summary>Decoded metadata from an Amaquet protocol frame header.</summary>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public readonly record struct WireHeader(
    FrameKind Kind,
    Opcode Opcode,
    ulong RequestId,
    int Length,
    ushort Flags
);

/// <summary>WireProtocol: Provides the Wire Protocol operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Defines strict Amaquet protocol v1 frame layout, opcodes, validation, and payload limits.</remarks>
/// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public static class WireProtocol
{
    /// <summary>Length in bytes of a protocol frame header.</summary>
    public const int HeaderLength = 24;

    /// <summary>Maximum protocol payload size in bytes.</summary>
    public const int MaxPayload = 64 * 1024 * 1024;

    /// <summary>Encode: Encode using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public static byte[] Encode(WireFrame frame, int maximum = MaxPayload)
    {
        if (
            frame.Payload is null
            || frame.Payload.Length > maximum
            || maximum < 1
            || maximum > MaxPayload
            || !Enum.IsDefined(frame.Kind)
            || !Enum.IsDefined(frame.Opcode)
        )
        {
            throw new ArgumentException("Invalid protocol frame");
        }

        var bytes = new byte[checked(HeaderLength + frame.Payload.Length)];
        bytes[0] = (byte)'A';
        bytes[1] = (byte)'M';
        bytes[2] = (byte)'Q';
        bytes[3] = (byte)'T';
        bytes[4] = 1;
        bytes[5] = (byte)frame.Kind;

        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(6, 2), frame.Flags);
        BinaryPrimitives.WriteUInt16BigEndian(
            bytes.AsSpan(8, 2),
            (ushort)frame.Opcode
        );

        // bytes[10..12] are reserved and must be zero.
        BinaryPrimitives.WriteUInt64BigEndian(
            bytes.AsSpan(12, 8),
            frame.RequestId
        );

        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(20, 4),
            (uint)frame.Payload.Length
        );

        frame.Payload.CopyTo(bytes.AsSpan(HeaderLength));
        return bytes;
    }

    /// <summary>DecodeHeader: Decode Header using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public static WireHeader DecodeHeader(
        ReadOnlySpan<byte> h,
        int maximum = MaxPayload
    )
    {
        if (
            h.Length != HeaderLength
            || maximum < 1
            || maximum > MaxPayload
            || h[0] != 'A'
            || h[1] != 'M'
            || h[2] != 'Q'
            || h[3] != 'T'
            || h[4] != 1
            || BinaryPrimitives.ReadUInt16BigEndian(h.Slice(10, 2)) != 0
        )
        {
            throw new AmaquetProtocolException("Invalid Amaquet frame header");
        }

        var kind = (FrameKind)h[5];
        var opcode = (Opcode)
            BinaryPrimitives.ReadUInt16BigEndian(h.Slice(8, 2));
        var size = BinaryPrimitives.ReadUInt32BigEndian(h.Slice(20, 4));

        if (!Enum.IsDefined(kind) || !Enum.IsDefined(opcode) || size > maximum)
        {
            throw new AmaquetProtocolException(
                "Invalid frame kind, opcode, or payload length"
            );
        }

        return new WireHeader(
            kind,
            opcode,
            BinaryPrimitives.ReadUInt64BigEndian(h.Slice(12, 8)),
            checked((int)size),
            BinaryPrimitives.ReadUInt16BigEndian(h.Slice(6, 2))
        );
    }

    /// <summary>ReadAsync: Read Async using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Protocol v1 frames use a fixed 24-byte header and a maximum 64 MiB payload; reject malformed frames.</remarks>
    /// <remarks>Parameter `ct`: Cancellation token for this operation.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <param name="stream">Input stream containing the frame.</param>
    /// <param name="maximum">Maximum permitted payload size.</param>
    /// <param name="ct">Argument passed to the Amaquet server or used for local request settings.</param>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public static async Task<WireFrame> ReadAsync(
        Stream stream,
        int maximum = MaxPayload,
        CancellationToken ct = default
    )
    {
        byte[] header = new byte[HeaderLength];
        await stream.ReadExactlyAsync(header, ct).ConfigureAwait(false);

        var h = DecodeHeader(header, maximum); // validate before allocating the payload
        byte[] payload = new byte[h.Length];

        await stream.ReadExactlyAsync(payload, ct).ConfigureAwait(false);
        return new WireFrame(h.Kind, h.Opcode, h.RequestId, payload, h.Flags);
    }
}
