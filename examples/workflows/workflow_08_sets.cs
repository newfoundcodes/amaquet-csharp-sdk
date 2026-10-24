using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 8: workflow_08_sets. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow08Sets
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Set("example:workflow:set");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AddAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"value\":{\"type\":\"utf8_string\",\"value\":\"alpha\"}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
