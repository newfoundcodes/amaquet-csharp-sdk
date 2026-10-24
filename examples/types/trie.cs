using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed trie example. OP calls require --operations.</summary>
public static class TypeTrie
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Trie("example:trie");
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
