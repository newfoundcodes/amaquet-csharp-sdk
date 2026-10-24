using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Source-backed lock example. OP calls require --operations.</summary>
public static class TypeLock
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Lock("example:lock");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (!operations)
        {
            return;
        }

        Console.WriteLine(
            (
                await handle.TryLockAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"owner\":\"worker-a\",\"token\":\"t1\",\"ttl_ms\":5000}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.UnlockAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"token\":\"t1\"}"
                    )!
                )
            ).GetRawText()
        );

        Console.WriteLine(
            (
                await handle.RenewAsync(
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        "{\"token\":\"t1\",\"ttl_ms\":5000}"
                    )!
                )
            ).GetRawText()
        );
    }
}
