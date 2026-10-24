using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed quantized_vector example. OP calls require --operations.</summary>
public static class TypeQuantizedVector
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.QuantizedVector("example:quantized_vector");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument
                .Parse("{\"Data\":[1,2,-3],\"Scale\":0.1,\"ZeroPoint\":0}")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.DequantizeAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
