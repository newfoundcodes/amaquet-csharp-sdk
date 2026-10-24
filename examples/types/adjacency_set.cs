using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed adjacency_set example. OP calls require --operations.</summary>
public static class TypeAdjacencySet
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.AdjacencySet("example:adjacency_set");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AddAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":\"a\",\"to\":\"b\",\"weight\":1.5}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.NeighborsAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":\"a\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":\"a\",\"to\":\"b\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
