using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed geo_point example. OP calls require --operations.</summary>
public static class TypeGeoPoint
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.GeoPoint("example:geo_point");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("{\"Lat\":14.5995,\"Lon\":120.9842}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.DistanceAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"point\":{\"Lat\":14.5995,\"Lon\":120.9842}}"
                    )!
                )
            ).GetRawText()
        );
    }
}
