using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed roaring_bitmap example. OP calls require --operations.</summary>
public static class TypeRoaringBitmap
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.RoaringBitmap("example:roaring_bitmap");
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
                        "{\"value\":42}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ContainsAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":42}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.CardinalityAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":42}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ValuesAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
