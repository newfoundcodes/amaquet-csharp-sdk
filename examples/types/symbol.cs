using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed symbol example. OP calls require --operations.</summary>
public static class TypeSymbol
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Symbol("example:symbol");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("\"ACTIVE\"").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }
    }
}
