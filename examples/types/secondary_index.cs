using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed secondary_index example. OP calls require --operations.</summary>
public static class TypeSecondaryIndex
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.SecondaryIndex("example:secondary_index");
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
                        "{\"field\":\"status\",\"value\":\"active\",\"id\":\"u1\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.LookupAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"field\":\"status\",\"value\":\"active\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
