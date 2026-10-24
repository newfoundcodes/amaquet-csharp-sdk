using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed url example. OP calls require --operations.</summary>
public static class TypeUrl
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Url("example:url");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument
                .Parse("\"https://example.com:8443/path?q=amaquet\"")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.HostAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SchemeAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
