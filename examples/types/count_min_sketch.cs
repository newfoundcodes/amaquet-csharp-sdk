using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed count_min_sketch example. OP calls require --operations.</summary>
public static class TypeCountMinSketch
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.CountMinSketch("example:count_min_sketch");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"width\":256,\"depth\":4}").RootElement
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
                        "{\"value\":\"alice\",\"count\":5}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.EstimateAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"alice\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
