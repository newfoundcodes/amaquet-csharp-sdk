using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed timeseries_labels example. OP calls require --operations.</summary>
public static class TypeTimeseriesLabels
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.TimeseriesLabels("example:timeseries_labels");
        await handle.CreateAsync(
            JsonDocument
                .Parse("{\"labels\":{\"env\":\"prod\",\"region\":\"local\"}}")
                .RootElement
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
                        "{\"label\":\"region\",\"value\":\"ph\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.GetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
