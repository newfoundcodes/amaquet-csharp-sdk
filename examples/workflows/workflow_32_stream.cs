using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 32: workflow_32_stream. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow32Stream
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Stream("example:workflow:stream");
        await handle.CreateAsync(JsonDocument.Parse("{}").RootElement);

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.AddAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"fields\":{\"msg\":{\"type\":\"utf8_string\",\"value\":\"hello\"}}}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
