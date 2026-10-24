using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed null example. OP calls require --operations.</summary>
public static class TypeNull
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Null("example:null");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("null").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }
    }
}
