using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed ring_buffer example. OP calls require --operations.</summary>
public static class TypeRingBuffer
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.RingBuffer("example:ring_buffer");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"capacity\":4}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.PushAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":{\"type\":\"utf8_string\",\"value\":\"a\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.LenAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ValuesAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
