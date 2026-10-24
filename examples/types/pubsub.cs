using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed pubsub example. OP calls require --operations.</summary>
public static class TypePubsub
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Pubsub("example:pubsub");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.PublishAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"channel\":\"events\",\"value\":{\"type\":\"utf8_string\",\"value\":\"ping\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.StatsAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
