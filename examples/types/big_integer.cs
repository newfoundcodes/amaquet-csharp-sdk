using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed big_integer example. OP calls require --operations.</summary>
public static class TypeBigInteger
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.BigInteger("example:big_integer");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument
                .Parse("\"1234567890123456789012345678901234567890\"")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AddAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"10\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
