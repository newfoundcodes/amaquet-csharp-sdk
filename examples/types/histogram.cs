using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed histogram example. OP calls require --operations.</summary>
public static class TypeHistogram
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Histogram("example:histogram");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"bounds\":[1,5,10,50,100]}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.ObserveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":3}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SnapshotAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
