using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed dense_vector example. OP calls require --operations.</summary>
public static class TypeDenseVector
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.DenseVector("example:dense_vector");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("[1.0,0.0,0.5]").RootElement
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
                        "{\"vector\":[1.0,0.0,0.5],\"metric\":\"cosine\"}"
                    )!
                )
            ).GetRawText()
        );
    }
}
