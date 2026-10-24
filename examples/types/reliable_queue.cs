using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed reliable_queue example. OP calls require --operations.</summary>
public static class TypeReliableQueue
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.ReliableQueue("example:reliable_queue");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"max_attempts\":3}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.EnqueueAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"job1\",\"value\":{\"type\":\"utf8_string\",\"value\":\"work\"}}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.ClaimAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"lease_ms\":5000}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.AckAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"job1\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.NackAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"id\":\"job1\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.StatsAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{}"
                    )!
                )
            ).GetRawText()
        );
    }
}
