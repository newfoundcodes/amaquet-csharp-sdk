using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed persistent_topic example. OP calls require --operations.</summary>
public static class TypePersistentTopic
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.PersistentTopic("example:persistent_topic");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"max_messages\":100}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.PublishAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"message_key\":\"m1\",\"value\":{\"type\":\"utf8_string\",\"value\":\"hello\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ReadAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"from\":0,\"count\":10}"
                    )!
                )
            ).GetRawText()
        );
    }
}
