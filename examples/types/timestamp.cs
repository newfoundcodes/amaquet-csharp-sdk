using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed timestamp example. OP calls require --operations.</summary>
public static class TypeTimestamp
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Timestamp("example:timestamp");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("\"2026-10-01T00:00:00Z\"").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }
    }
}
