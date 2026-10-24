using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed bit_field example. OP calls require --operations.</summary>
public static class TypeBitField
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.BitField("example:bit_field");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"size_bytes\":8}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.SetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"offset\":4,\"width\":8,\"value\":171}"
                    )!
                )
            ).GetRawText()
        );
        Console.WriteLine(
            (
                await handle.GetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"offset\":4,\"width\":8}"
                    )!
                )
            ).GetRawText()
        );
    }
}
