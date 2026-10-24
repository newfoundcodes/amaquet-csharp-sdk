using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed vector_set example. OP calls require --operations.</summary>
public static class TypeVectorSet
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.VectorSet("example:vector_set");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"dim\":3,\"metric\":\"cosine\"}").RootElement
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
                        "{\"id\":\"v1\",\"vector\":[1,0,0],\"metadata\":{\"class\":\"a\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.SearchAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"vector\":[1,0,0],\"k\":1}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RemoveAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"v1\"}"
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
