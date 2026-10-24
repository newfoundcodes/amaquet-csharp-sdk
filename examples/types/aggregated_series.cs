using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed aggregated_series example. OP calls require --operations.</summary>
public static class TypeAggregatedSeries
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.AggregatedSeries("example:aggregated_series");
        await handle.CreateAsync(
            JsonDocument
                .Parse("{\"bucket_ms\":1000,\"aggregation\":\"avg\"}")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.AggregateAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"source_key\":\"example:timeseries:source\",\"from\":0,\"to\":3000,\"bucket_ms\":1000,\"aggregation\":\"avg\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RangeAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"source_key\":\"example:timeseries:source\",\"from\":0,\"to\":3000,\"bucket_ms\":1000,\"aggregation\":\"avg\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
