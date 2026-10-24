using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed top_k example. OP calls require --operations.</summary>
public static class TypeTopK
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.TopK("example:top_k");
        await handle.CreateAsync(JsonDocument.Parse("{\"k\":5}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AddAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"alice\",\"count\":10}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ItemsAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
