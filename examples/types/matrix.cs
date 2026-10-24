using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed matrix example. OP calls require --operations.</summary>
public static class TypeMatrix
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Matrix("example:matrix");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"rows\":2,\"cols\":2,\"dtype\":\"float64\",\"data\":[1,2,3,4]}"
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
                await handle.GetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"indices\":[1,1]}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SetAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"indices\":[0,1],\"value\":9}"
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
