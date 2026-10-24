using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed stream example. OP calls require --operations.</summary>
public static class TypeStream
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Stream("example:stream");
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
                        "{\"fields\":{\"msg\":{\"type\":\"utf8_string\",\"value\":\"hello\"}}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RangeAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"count\":10}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.GroupCreateAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"group\":\"g1\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.GroupReadAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"group\":\"workers\",\"consumer\":\"c1\",\"count\":10}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.AckAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"group\":\"workers\",\"ids\":[\"0-1\"]}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.PendingAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"group\":\"workers\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.LenAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
