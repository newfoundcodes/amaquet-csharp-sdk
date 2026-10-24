using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed radix_tree example. OP calls require --operations.</summary>
public static class TypeRadixTree
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.RadixTree("example:radix_tree");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.InsertAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"amaquet\",\"id\":\"1\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.PrefixAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"prefix\":\"ama\",\"limit\":10}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ExactAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"amaquet\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
