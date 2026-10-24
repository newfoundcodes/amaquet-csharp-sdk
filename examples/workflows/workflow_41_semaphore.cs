using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 41: workflow_41_semaphore. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow41Semaphore
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Semaphore("example:workflow:semaphore");
        await handle.CreateAsync(
            JsonDocument.Parse("{\"limit\":2}").RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.TryAcquireAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
