using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 28: workflow_28_nested_typed_values. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow28NestedTypedValues
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Record("example:workflow:record");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"schema\":[{\"name\":\"name\",\"type\":\"utf8_string\",\"nullable\":false},{\"name\":\"age\",\"type\":\"integer\",\"nullable\":true}]}"
                )
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.SetAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"field\":\"name\",\"value\":{\"type\":\"utf8_string\",\"value\":\"alice\"}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
