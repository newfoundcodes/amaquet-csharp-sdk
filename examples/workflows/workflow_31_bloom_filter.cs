using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 31: workflow_31_bloom_filter. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow31BloomFilter
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.BloomFilter("example:workflow:bloom_filter");
        await handle.CreateAsync(
            JsonDocument
                .Parse("{\"expected\":1000,\"false_positive_rate\":0.01}")
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AddAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"value\":\"alice\"}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
