using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 39: workflow_39_matrix. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow39Matrix
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        var handle = client.Typed.Matrix("example:workflow:matrix");
        await handle.CreateAsync(
            JsonDocument
                .Parse(
                    "{\"rows\":2,\"cols\":2,\"dtype\":\"float64\",\"data\":[1,2,3,4]}"
                )
                .RootElement
        );

        Console.WriteLine((await handle.ReadKeyAsync()).GetRawText());
        if (operations)
        {
            Console.WriteLine(
                (
                    await handle.GetAsync(
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            "{\"indices\":[1,1]}"
                        )!
                    )
                ).GetRawText()
            );
        }
    }
}
