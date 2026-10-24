using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed decimal example. OP calls require --operations.</summary>
public static class TypeDecimal
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Decimal("example:decimal");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("\"12345.6789\"").RootElement
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
                        "{\"value\":\"0.3211\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.CmpAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"12346.0000\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SubAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":\"0.3211\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
