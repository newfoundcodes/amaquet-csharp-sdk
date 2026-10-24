using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 40: workflow_40_event_log. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow40EventLog
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.EventLog("example:workflow:event_log");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AppendAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"event_type\":\"created\",\"aggregate_id\":\"agg1\",\"payload\":{\"type\":\"utf8_string\",\"value\":\"born\"}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
