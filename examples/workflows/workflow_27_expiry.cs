using System.Text.Json;
using Amaquet;

namespace Amaquet.Examples;

/// <summary>Workflow 27: workflow_27_expiry. Use only on a disposable instance when needed.</summary>
public static class WorkflowWorkflow27Expiry
{
    public static async Task RunAsync(AmaquetClient client, bool operations)
    {
        await client.SetAsync(
            "example:workflow:expire",
            "utf8_string",
            "later"
        );

        await client.ExpireAtAsync(
            "example:workflow:expire",
            DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeMilliseconds()
                * 1000000
        );
    }
}
