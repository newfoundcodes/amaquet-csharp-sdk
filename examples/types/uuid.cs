using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed uuid example. OP calls require --operations.</summary>
public static class TypeUuid
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Uuid("example:uuid");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument
                .Parse("\"550e8400-e29b-41d4-a716-446655440000\"")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }
    }
}
