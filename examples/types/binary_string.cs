using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed binary_string example. OP calls require --operations.</summary>
public static class TypeBinaryString
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.BinaryString("example:binary_string");
        await client.SetAsync(
            handle.Key,
            handle.WireType,
            JsonDocument.Parse("\"AAECaGVsbG8=\"").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }
    }
}
