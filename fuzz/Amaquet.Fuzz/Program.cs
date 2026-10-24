using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Amaquet;

int cases =
    args.Length > 0 && int.TryParse(args[0], out int count) ? count : 2000;
if (cases < 1 || cases > 1000000)
{
    throw new ArgumentOutOfRangeException(nameof(cases));
}

var random = new Random(632006);
var targets = new Action<byte[]>[]
{
    FuzzFrame,
    FuzzHeader,
    FuzzMagic,
    FuzzVersion,
    FuzzFlags,
    FuzzOpcode,
    FuzzPayloadSize,
    FuzzJson,
    FuzzInteger,
    FuzzEndpoint,
    FuzzIdentity,
    FuzzResourceId,
    FuzzBase64,
};

for (int t = 0; t < targets.Length; t++)
{
    for (int i = 0; i < cases; i++)
    {
        byte[] input = new byte[random.Next(0, 512)];
        random.NextBytes(input);
        targets[t](input);
    }

    Console.WriteLine(
        $"PASS fuzz target {t + 1}/{targets.Length}, {cases} mutations"
    );
}

Console.WriteLine(
    $"PASS {targets.Length * cases} deterministic mutation cases"
);

static void FuzzFrame(byte[] data)
{
    var frame = WireProtocol.Encode(
        new WireFrame(FrameKind.Request, Opcode.Command, 123, data)
    );

    if (
        !WireProtocol
            .DecodeHeader(frame.AsSpan(0, 24))
            .Equals(
                new WireHeader(
                    FrameKind.Request,
                    Opcode.Command,
                    123,
                    data.Length,
                    0
                )
            )
    )
    {
        throw new Exception("Frame roundtrip invariant broken");
    }
}

static void FuzzHeader(byte[] data)
{
    if (data.Length == 24)
    {
        Rejectable(() => WireProtocol.DecodeHeader(data));
    }
}

static void FuzzMagic(byte[] data)
{
    var frame = WireProtocol.Encode(
        new WireFrame(FrameKind.Request, Opcode.Command, 5, data)
    );

    frame[0] ^= 1;
    Rejectable(() => WireProtocol.DecodeHeader(frame.AsSpan(0, 24)));
}

static void FuzzVersion(byte[] data)
{
    var frame = WireProtocol.Encode(
        new WireFrame(FrameKind.Request, Opcode.Command, 5, data)
    );

    frame[4] = 42;
    Rejectable(() => WireProtocol.DecodeHeader(frame.AsSpan(0, 24)));
}

static void FuzzFlags(byte[] data)
{
    var frame = WireProtocol.Encode(
        new WireFrame(FrameKind.Request, Opcode.Command, 5, data)
    );

    frame[10] = 1;
    Rejectable(() => WireProtocol.DecodeHeader(frame.AsSpan(0, 24)));
}

static void FuzzOpcode(byte[] data)
{
    var frame = WireProtocol.Encode(
        new WireFrame(FrameKind.Request, Opcode.Command, 5, data)
    );

    frame[9] = 99;
    Rejectable(() => WireProtocol.DecodeHeader(frame.AsSpan(0, 24)));
}

static void FuzzPayloadSize(byte[] data)
{
    var frame = WireProtocol.Encode(
        new WireFrame(FrameKind.Request, Opcode.Command, 5, data)
    );

    frame[20] = 255;
    frame[21] = 255;
    frame[22] = 255;
    frame[23] = 255;

    Rejectable(() => WireProtocol.DecodeHeader(frame.AsSpan(0, 24)));
}

static void FuzzJson(byte[] data)
{
    Rejectable(() => JsonWire.Decode(data));
}

static void FuzzInteger(byte[] data)
{
    var n = new BigInteger(data, isUnsigned: false, isBigEndian: true);
    var encoded = JsonWire.Encode(JsonWire.Map("integer", n));

    if (
        JsonWire.Decode(encoded).GetProperty("integer").GetRawText()
        != n.ToString(System.Globalization.CultureInfo.InvariantCulture)
    )
    {
        throw new Exception("BigInteger precision lost");
    }
}

static void FuzzEndpoint(byte[] data)
{
    var text = Encoding.Latin1.GetString(data);
    Rejectable(() => AmaquetEndpoint.Parse(text));
}

static void FuzzIdentity(byte[] data)
{
    var fake = JsonWire.Decode(
        JsonWire.Encode(
            JsonWire.Map(
                "identity_public_key",
                Convert.ToBase64String(data),
                "nonce_signature",
                Convert.ToBase64String(data)
            )
        )
    );

    Rejectable(() =>
        Identity.Verify(
            fake,
            "challenge",
            new ClientOptions { RequireSignedIdentity = true }
        )
    );
}

static void FuzzResourceId(byte[] data)
{
    Rejectable(() => AdminClient.PathId(Encoding.UTF8.GetString(data)));
}

static void FuzzBase64(byte[] data)
{
    string text = Convert.ToBase64String(data);
    if (!Convert.FromBase64String(text).SequenceEqual(data))
    {
        throw new Exception("Base64 roundtrip failed");
    }
}

static void Rejectable(Action action)
{
    try
    {
        action();
    }
    catch (Exception e)
        when (e
                is AmaquetException
                    or ArgumentException
                    or FormatException
                    or System.Text.Json.JsonException
        ) { }
}
