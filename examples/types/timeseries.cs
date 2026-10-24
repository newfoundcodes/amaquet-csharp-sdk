using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed timeseries example. OP calls require --operations.</summary>
public static class TypeTimeseries
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Timeseries("example:timeseries");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"labels\":{\"service\":\"api\",\"env\":\"prod\"},\"retention_ms\":3600000}"
                )
                .RootElement
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
                        "{\"timestamp\":1730000000000,\"value\":10}"
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

        Console.WriteLine(
            (
                await handle.RangeAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":0,\"to\":3000}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.LastAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.AggregateAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":0,\"to\":3000,\"bucket_ms\":1000,\"aggregation\":\"avg\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
