using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amaquet;

/// <summary>JsonWire: Provides the Json Wire operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Converts native values to JSON and preserves exact integer representations where required.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public static class JsonWire
{
    /// <summary>new: Provides the new operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new BigIntegerJsonConverter() },
    };

    /// <summary>Encode: Encode using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Parameter `value`: Data to write or transmit, using the wire-type JSON format.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public static byte[] Encode(object? value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, Options);

    /// <summary>Decode: Decode using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public static JsonElement Decode(ReadOnlySpan<byte> json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json.ToArray());
            return doc.RootElement.Clone();
        }
        catch (JsonException e)
        {
            throw new AmaquetProtocolException("Malformed JSON payload", e);
        }
    }

    /// <summary>Map: Provides the Map operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public static Dictionary<string, object?> Map(params object?[] pairs)
    {
        if (pairs.Length % 2 != 0)
        {
            throw new ArgumentException("Keys and values must form pairs");
        }

        var map = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (int i = 0; i < pairs.Length; i += 2)
        {
            map.Add(
                (string)(
                    pairs[i] ?? throw new ArgumentNullException(nameof(pairs))
                ),
                pairs[i + 1]
            );
        }

        return map;
    }

    /// <summary>RequireString: Provides the Require String operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public static string RequireString(JsonElement obj, string field)
    {
        if (
            !obj.TryGetProperty(field, out var value)
            || value.ValueKind != JsonValueKind.String
        )
        {
            throw new AmaquetProtocolException(
                $"Missing string field '{field}'"
            );
        }

        return value.GetString()!;
    }

    /// <summary>RequireInt64: Provides the Require Int64 operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    /// <returns>The decoded result returned by this asynchronous or synchronous operation.</returns>
    public static long RequireInt64(JsonElement obj, string field)
    {
        if (
            !obj.TryGetProperty(field, out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt64(out long n)
        )
        {
            throw new AmaquetProtocolException(
                $"Missing exact int64 field '{field}'"
            );
        }

        return n;
    }
}

/// <summary>BigIntegerJsonConverter: Provides the Big Integer Json Converter operation or value as implemented by this Amaquet SDK component; follow the server protocol for accepted arguments.</summary>
/// <remarks>Converts native values to JSON and preserves exact integer representations where required.</remarks>
/// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
/// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
public sealed class BigIntegerJsonConverter : JsonConverter<BigInteger>
{
    /// <summary>Read: Read using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Parameter `options`: Call settings or documented construction flags, as accepted by this method.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public override BigInteger Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected integral JSON number");
        }

        byte[] rawUtf8 = reader.HasValueSequence
            ? reader.ValueSequence.ToArray()
            : reader.ValueSpan.ToArray();
        var raw = System.Text.Encoding.UTF8.GetString(rawUtf8);
        if (
            !BigInteger.TryParse(
                raw,
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var parsed
            )
        )
        {
            throw new JsonException("Invalid integer");
        }

        return parsed;
    }

    /// <summary>Write: Write using the implementation&#x27;s validation, error, and resource-lifetime rules.</summary>
    /// <remarks>Use TLS and identity pins for remote endpoints; close resources and bound pending requests in long-running applications.</remarks>
    /// <remarks>Parameter `options`: Call settings or documented construction flags, as accepted by this method.</remarks>
    /// <remarks>Parameter `value`: Data to write or transmit, using the wire-type JSON format.</remarks>
    /// <remarks>Failures: Invalid arguments, protocol errors, remote errors, timeout, or connection loss may be reported as appropriate to this API.</remarks>
    public override void Write(
        Utf8JsonWriter writer,
        BigInteger value,
        JsonSerializerOptions options
    ) => writer.WriteRawValue(value.ToString(CultureInfo.InvariantCulture));
}
