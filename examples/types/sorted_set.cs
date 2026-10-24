using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed sorted_set example. OP calls require --operations.</summary>
public static class TypeSortedSet
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.SortedSet("example:sorted_set");
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
                        "{\"member\":{\"type\":\"utf8_string\",\"value\":\"alpha\"},\"score\":10.5}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ScoreAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"member\":{\"type\":\"utf8_string\",\"value\":\"alpha\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"member\":{\"type\":\"utf8_string\",\"value\":\"alpha\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RangeAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"start\":0,\"stop\":-1,\"reverse\":false}"
                    )!
                )
            ).GetRawText()
        );
    }
}
