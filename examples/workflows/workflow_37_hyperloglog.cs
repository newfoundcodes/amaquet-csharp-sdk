using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 37: workflow_37_hyperloglog. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow37Hyperloglog
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Hyperloglog("example:workflow:hyperloglog");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"precision\":12}").RootElement
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
