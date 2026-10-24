using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 9: workflow_09_hashmap. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow09Hashmap
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Hashmap("example:workflow:hashmap");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.SetAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"field\":\"name\",\"value\":{\"type\":\"utf8_string\",\"value\":\"alpha\"}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
