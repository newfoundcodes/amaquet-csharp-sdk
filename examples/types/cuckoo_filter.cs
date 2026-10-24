using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed cuckoo_filter example. OP calls require --operations.</summary>
public static class TypeCuckooFilter
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.CuckooFilter("example:cuckoo_filter");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"capacity\":1024}").RootElement
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
                        "{\"value\":\"alice\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ContainsAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"alice\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"alice\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
