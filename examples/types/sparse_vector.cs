using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed sparse_vector example. OP calls require --operations.</summary>
public static class TypeSparseVector
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.SparseVector("example:sparse_vector");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument
                .Parse("{\"dim\":8,\"values\":{\"1\":0.5,\"5\":1.25}}")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }
    }
}
