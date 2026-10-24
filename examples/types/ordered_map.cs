using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed ordered_map example. OP calls require --operations.</summary>
public static class TypeOrderedMap
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.OrderedMap("example:ordered_map");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.SetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"field\":\"name\",\"value\":{\"type\":\"utf8_string\",\"value\":\"alpha\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.GetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"field\":\"name\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.DeleteAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"field\":\"name\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.EntriesAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
