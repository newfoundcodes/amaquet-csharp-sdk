using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed lifo_stack example. OP calls require --operations.</summary>
public static class TypeLifoStack
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.LifoStack("example:lifo_stack");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.PushAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"value\":{\"type\":\"utf8_string\",\"value\":\"first\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.PeekAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.PopAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
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
