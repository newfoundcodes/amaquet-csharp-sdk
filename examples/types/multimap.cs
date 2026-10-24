using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed multimap example. OP calls require --operations.</summary>
public static class TypeMultimap
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Multimap("example:multimap");
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
                        "{\"field\":\"tag\",\"value\":{\"type\":\"utf8_string\",\"value\":\"alpha\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.GetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"field\":\"tag\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"field\":\"tag\",\"value\":{\"type\":\"utf8_string\",\"value\":\"alpha\"}}"
                    )!
                )
            ).GetRawText()
        );
    }
}
