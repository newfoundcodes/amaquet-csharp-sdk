using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 29: workflow_29_ordered_map. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow29OrderedMap
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.OrderedMap("example:workflow:ordered_map");
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
